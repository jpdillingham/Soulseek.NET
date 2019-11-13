// <copyright file="ServerMessageHandler.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Options;

    /// <summary>
    ///     Handles incoming messages from the server connection.
    /// </summary>
    internal sealed class ServerMessageHandler : IServerMessageHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerMessageHandler"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public ServerMessageHandler(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new ClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessage> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        public event EventHandler<UserStatusChangedEventArgs> UserStatusChanged;

        private IDiagnosticFactory Diagnostic { get; }

        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Handles incoming messages.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="message">The message.</param>
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
                        SoulseekClient.Waiter.Complete(new WaitKey(code), IntegerResponse.FromByteArray<MessageCode.Server>(message));
                        break;

                    case MessageCode.Server.Login:
                        SoulseekClient.Waiter.Complete(new WaitKey(code), LoginResponse.FromByteArray(message));
                        break;

                    case MessageCode.Server.RoomList:
                        SoulseekClient.Waiter.Complete(new WaitKey(code), RoomList.FromByteArray(message));
                        break;

                    case MessageCode.Server.PrivilegedUsers:
                        SoulseekClient.Waiter.Complete(new WaitKey(code), PrivilegedUserList.FromByteArray(message));
                        break;

                    case MessageCode.Server.NetInfo:
                        var netInfo = NetInfo.FromByteArray(message);

                        try
                        {
                            await SoulseekClient.DistributedConnectionManager.AddParentConnectionAsync(netInfo.Parents).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Debug($"Error handling NetInfo message: {ex.Message}");
                        }

                        break;

                    case MessageCode.Server.ConnectToPeer:
                        ConnectToPeerResponse connectToPeerResponse = default;

                        try
                        {
                            connectToPeerResponse = ConnectToPeerResponse.FromByteArray(message);

                            if (connectToPeerResponse.Type == Constants.ConnectionType.Transfer)
                            {
                                // ensure that we are expecting at least one file from this user before we connect. the response
                                // doesn't contain any other identifying information about the file.
                                if (!SoulseekClient.Downloads.IsEmpty && SoulseekClient.Downloads.Values.Any(d => d.Username == connectToPeerResponse.Username))
                                {
                                    var (connection, remoteToken) = await SoulseekClient.PeerConnectionManager.GetTransferConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                                    var download = SoulseekClient.Downloads.Values.FirstOrDefault(v => v.RemoteToken == remoteToken && v.Username == connectToPeerResponse.Username);

                                    if (download != default(Transfer))
                                    {
                                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken), connection);
                                    }
                                }
                                else
                                {
                                    throw new SoulseekClientException($"Unexpected transfer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}); Ignored");
                                }
                            }
                            else if (connectToPeerResponse.Type == Constants.ConnectionType.Peer)
                            {
                                await SoulseekClient.PeerConnectionManager.GetOrAddMessageConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                            }
                            else if (connectToPeerResponse.Type == Constants.ConnectionType.Distributed)
                            {
                                await SoulseekClient.DistributedConnectionManager.AddChildConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                            }
                            else
                            {
                                throw new MessageException($"Unknown Connect To Peer connection type '{connectToPeerResponse.Type}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Debug($"Error handling ConnectToPeer response from {connectToPeerResponse?.Username} ({connectToPeerResponse?.IPAddress}:{connectToPeerResponse.Port}): {ex.Message}");
                        }

                        break;

                    case MessageCode.Server.AddUser:
                        var addUserResponse = AddUserResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code, addUserResponse.Username), addUserResponse);
                        break;

                    case MessageCode.Server.GetStatus:
                        var statsResponse = UserStatusResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code, statsResponse.Username), statsResponse);
                        UserStatusChanged?.Invoke(this, new UserStatusChangedEventArgs(statsResponse));
                        break;

                    case MessageCode.Server.PrivateMessage:
                        var pm = PrivateMessage.FromByteArray(message);
                        PrivateMessageReceived?.Invoke(this, pm);

                        if (SoulseekClient.Options.AutoAcknowledgePrivateMessages)
                        {
                            await SoulseekClient.AcknowledgePrivateMessageAsync(pm.Id, CancellationToken.None).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Server.GetPeerAddress:
                        var peerAddressResponse = UserAddressResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code, peerAddressResponse.Username), peerAddressResponse);
                        break;

                    case MessageCode.Server.JoinRoom:
                        var joinRoomResponse = JoinRoomResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code, joinRoomResponse.RoomName), joinRoomResponse);
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