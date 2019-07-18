namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

    internal sealed class ServerMessageHandler : IServerMessageHandler
    {
        public ServerMessageHandler(
            ISoulseekClient soulseekClient,
            ClientOptions clientOptions,
            IPeerConnectionManager peerConnectionManager,
            IWaiter waiter,
            ConcurrentDictionary<int, Transfer> downloads,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient;
            ClientOptions = clientOptions;
            PeerConnectionManager = peerConnectionManager;
            Waiter = waiter;
            Downloads = downloads;
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, ClientOptions.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessage> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        public event EventHandler<UserStatusChangedEventArgs> UserStatusChanged;

        private ClientOptions ClientOptions { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private ConcurrentDictionary<int, Transfer> Downloads { get; }
        private IPeerConnectionManager PeerConnectionManager { get; }
        private ISoulseekClient SoulseekClient { get; }
        private IWaiter Waiter { get; }

        public async void HandleMessage(object sender, byte[] message)
        {
            var code = new MessageReader<MessageCode.Server>(message).ReadCode();
            Diagnostic.Debug($"Server message received: {code}");

            try
            {
                switch (code)
                {
                    case MessageCode.Server.ParentMinSpeed:
                    case MessageCode.Server.ParentSpeedRatio:
                    case MessageCode.Server.WishlistInterval:
                        Waiter.Complete(new WaitKey(code), IntegerResponse.FromByteArray<MessageCode.Server>(message));
                        break;

                    case MessageCode.Server.Login:
                        Waiter.Complete(new WaitKey(code), LoginResponse.FromByteArray(message));
                        break;

                    case MessageCode.Server.RoomList:
                        Waiter.Complete(new WaitKey(code), RoomList.FromByteArray(message));
                        break;

                    case MessageCode.Server.PrivilegedUsers:
                        Waiter.Complete(new WaitKey(code), PrivilegedUserList.FromByteArray(message));
                        break;

                    case MessageCode.Server.NetInfo:
                        var netInfo = NetInfo.FromByteArray(message);
                        foreach (var peer in netInfo.Parents)
                        {
                            Console.WriteLine($"{peer.Username} {peer.IPAddress} {peer.Port}");
                        }

                        break;

                    case MessageCode.Server.ConnectToPeer:
                        var connectToPeerResponse = ConnectToPeerResponse.FromByteArray(message);

                        if (connectToPeerResponse.Type == Constants.ConnectionType.Tranfer)
                        {
                            // ensure that we are expecting at least one file from this user before we connect. the response
                            // doesn't contain any other identifying information about the file.
                            if (!Downloads.IsEmpty && Downloads.Values.Any(d => d.Username == connectToPeerResponse.Username))
                            {
                                var (connection, remoteToken) = await PeerConnectionManager.GetTransferConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                                var download = Downloads.Values.FirstOrDefault(v => v.RemoteToken == remoteToken && v.Username == connectToPeerResponse.Username);

                                if (download != default(Transfer))
                                {
                                    Waiter.Complete(new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken), connection);
                                }
                            }
                            else
                            {
                                throw new SoulseekClientException($"Unexpected transfer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}); Ignored.");
                            }
                        }
                        else
                        {
                            await PeerConnectionManager.GetOrAddMessageConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Server.AddUser:
                        var addUserResponse = AddUserResponse.FromByteArray(message);
                        Waiter.Complete(new WaitKey(code, addUserResponse.Username), addUserResponse);
                        break;

                    case MessageCode.Server.GetStatus:
                        var statsResponse = GetStatusResponse.FromByteArray(message);
                        Waiter.Complete(new WaitKey(code, statsResponse.Username), statsResponse);
                        UserStatusChanged?.Invoke(this, new UserStatusChangedEventArgs(statsResponse));
                        break;

                    case MessageCode.Server.PrivateMessage:
                        var pm = PrivateMessage.FromByteArray(message);
                        PrivateMessageReceived?.Invoke(this, pm);

                        if (ClientOptions.AutoAcknowledgePrivateMessages)
                        {
                            await SoulseekClient.AcknowledgePrivateMessageAsync(pm.Id, CancellationToken.None).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Server.GetPeerAddress:
                        var peerAddressResponse = GetPeerAddressResponse.FromByteArray(message);
                        Waiter.Complete(new WaitKey(code, peerAddressResponse.Username), peerAddressResponse);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled server message: {code}; {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling server message: {code}; {ex.Message}", ex);
            }
        }
    }
}