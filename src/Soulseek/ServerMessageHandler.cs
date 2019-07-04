namespace Soulseek
{
    using System;
    using System.Linq;
    using System.Threading;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;

    internal sealed class ServerMessageHandler
    {
        public ServerMessageHandler(
            ISoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = (SoulseekClient)soulseekClient;
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        public event EventHandler<UserStatusChangedEventArgs> UserStatusChanged;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessage> PrivateMessageReceived;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        public async void HandleMessage(object sender, Message message)
        {
            Diagnostic.Debug($"Server message received: {message.Code}");

            try
            {
                switch (message.Code)
                {
                    case MessageCode.ServerParentMinSpeed:
                    case MessageCode.ServerParentSpeedRatio:
                    case MessageCode.ServerWishlistInterval:
                        SoulseekClient.Waiter.Complete(new WaitKey(message.Code), IntegerResponse.Parse(message));
                        break;

                    case MessageCode.ServerLogin:
                        SoulseekClient.Waiter.Complete(new WaitKey(message.Code), LoginResponse.Parse(message));
                        break;

                    case MessageCode.ServerRoomList:
                        SoulseekClient.Waiter.Complete(new WaitKey(message.Code), RoomList.Parse(message));
                        break;

                    case MessageCode.ServerPrivilegedUsers:
                        SoulseekClient.Waiter.Complete(new WaitKey(message.Code), PrivilegedUserList.Parse(message));
                        break;

                    case MessageCode.ServerConnectToPeer:
                        var connectToPeerResponse = ConnectToPeerResponse.Parse(message);

                        if (connectToPeerResponse.Type == Constants.ConnectionType.Tranfer)
                        {
                            // ensure that we are expecting at least one file from this user before we connect. the response
                            // doesn't contain any other identifying information about the file.
                            if (!SoulseekClient.Downloads.IsEmpty && SoulseekClient.Downloads.Values.Any(d => d.Username == connectToPeerResponse.Username))
                            {
                                var (connection, remoteToken) = await SoulseekClient.PeerConnectionManager.AddTransferConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                                var download = SoulseekClient.Downloads.Values.FirstOrDefault(v => v.RemoteToken == remoteToken && v.Username == connectToPeerResponse.Username);

                                if (download != default(Transfer))
                                {
                                    SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken), connection);
                                }
                            }
                            else
                            {
                                throw new SoulseekClientException($"Unexpected transfer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}); Ignored.");
                            }
                        }
                        else
                        {
                            await SoulseekClient.PeerConnectionManager.GetOrAddMessageConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.ServerAddUser:
                        var addUserResponse = AddUserResponse.Parse(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(message.Code, addUserResponse.Username), addUserResponse);
                        break;

                    case MessageCode.ServerGetStatus:
                        var statsResponse = GetStatusResponse.Parse(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(message.Code, statsResponse.Username), statsResponse);
                        UserStatusChanged?.Invoke(this, new UserStatusChangedEventArgs(statsResponse));
                        break;

                    case MessageCode.ServerPrivateMessage:
                        var pm = PrivateMessage.Parse(message);
                        PrivateMessageReceived?.Invoke(this, pm);

                        if (SoulseekClient.Options.AutoAcknowledgePrivateMessages)
                        {
                            await SoulseekClient.AcknowledgePrivateMessageAsync(pm.Id, CancellationToken.None).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.ServerGetPeerAddress:
                        var peerAddressResponse = GetPeerAddressResponse.Parse(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(message.Code, peerAddressResponse.Username), peerAddressResponse);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled server message: {message.Code}; {message.Payload.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling server message: {message.Code}; {ex.Message}", ex);
            }
        }
    }
}
