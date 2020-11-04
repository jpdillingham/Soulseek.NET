// <copyright file="ServerMessageHandler.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

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
                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when a global message is received.
        /// </summary>
        public event EventHandler<string> GlobalMessageReceived;

        /// <summary>
        ///     Occurs when the client is forcefully disconnected from the server, probably because another client logged in with
        ///     the same credentials.
        /// </summary>
        public event EventHandler KickedFromServer;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when the server sends a list of privileged users.
        /// </summary>
        public event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;

        /// <summary>
        ///     Occurs when the server sends a notification of new user privileges.
        /// </summary>
        public event EventHandler<PrivilegeNotificationReceivedEventArgs> PrivilegeNotificationReceived;

        /// <summary>
        ///     Occurs when a user joins a chat room.
        /// </summary>
        public event EventHandler<RoomJoinedEventArgs> RoomJoined;

        /// <summary>
        ///     Occurs when a user leaves a chat room.
        /// </summary>
        public event EventHandler<RoomLeftEventArgs> RoomLeft;

        /// <summary>
        ///     Occurs when the server sends a list of chat rooms.
        /// </summary>
        public event EventHandler<RoomListReceivedEventArgs> RoomListReceived;

        /// <summary>
        ///     Occurs when a chat room message is received.
        /// </summary>
        public event EventHandler<RoomMessageReceivedEventArgs> RoomMessageReceived;

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
        /// <param name="args">The message event args.</param>
        public void HandleMessageRead(object sender, MessageEventArgs args)
        {
            HandleMessageRead(sender, args.Message);
        }

        /// <summary>
        ///     Handles incoming messages.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="message">The message.</param>
        public async void HandleMessageRead(object sender, byte[] message)
        {
            var code = new MessageReader<MessageCode.Server>(message).ReadCode();

            if (code != MessageCode.Server.SearchRequest)
            {
                Diagnostic.Debug($"Server message received: {code}");
            }

            try
            {
                switch (code)
                {
                    case MessageCode.Server.ParentMinSpeed:
                    case MessageCode.Server.ParentSpeedRatio:
                    case MessageCode.Server.WishlistInterval:
                    case MessageCode.Server.CheckPrivileges:
                        SoulseekClient.Waiter.Complete(new WaitKey(code), IntegerResponse.FromByteArray<MessageCode.Server>(message));
                        break;

                    case MessageCode.Server.NewPassword:
                        var confirmedPassword = NewPassword.FromByteArray(message).Password;
                        SoulseekClient.Waiter.Complete(new WaitKey(code), confirmedPassword);
                        break;

                    case MessageCode.Server.GlobalAdminMessage:
                        var msg = GlobalMessageNotification.FromByteArray(message);
                        GlobalMessageReceived?.Invoke(this, msg);
                        break;

                    case MessageCode.Server.Ping:
                        SoulseekClient.Waiter.Complete(new WaitKey(code));
                        break;

                    case MessageCode.Server.Login:
                        SoulseekClient.Waiter.Complete(new WaitKey(code), LoginResponse.FromByteArray(message));
                        break;

                    case MessageCode.Server.RoomList:
                        var roomList = RoomListResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code), roomList);
                        RoomListReceived?.Invoke(this, new RoomListReceivedEventArgs(roomList));
                        break;

                    case MessageCode.Server.PrivilegedUsers:
                        var privilegedUserList = PrivilegedUserListNotification.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code), privilegedUserList);
                        PrivilegedUserListReceived?.Invoke(this, privilegedUserList);
                        break;

                    case MessageCode.Server.AddPrivilegedUser:
                        PrivilegeNotificationReceived?.Invoke(this, new PrivilegeNotificationReceivedEventArgs(PrivilegedUserNotification.FromByteArray(message)));
                        break;

                    case MessageCode.Server.NotifyPrivileges:
                        var pn = PrivilegeNotification.FromByteArray(message);
                        PrivilegeNotificationReceived?.Invoke(this, new PrivilegeNotificationReceivedEventArgs(pn.Username, pn.Id));

                        if (SoulseekClient.Options.AutoAcknowledgePrivilegeNotifications)
                        {
                            await SoulseekClient.AcknowledgePrivilegeNotificationAsync(pn.Id, CancellationToken.None).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Server.UserPrivileges:
                        var privilegeResponse = UserPrivilegeResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code, privilegeResponse.Username), privilegeResponse.IsPrivileged);
                        break;

                    case MessageCode.Server.NetInfo:
                        var netInfo = NetInfoNotification.FromByteArray(message);

                        try
                        {
                            var parents = netInfo.Parents.Select(parent => (parent.Username, new IPEndPoint(parent.IPAddress, parent.Port)));
                            await SoulseekClient.DistributedConnectionManager.AddParentConnectionAsync(parents).ConfigureAwait(false);
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
                                Diagnostic.Debug($"Received transfer ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for remote token {connectToPeerResponse.Token}");

                                // ensure that we are expecting at least one file from this user before we connect. the response
                                // doesn't contain any other identifying information about the file.
                                if (!SoulseekClient.Downloads.IsEmpty && SoulseekClient.Downloads.Values.Any(d => d.Username == connectToPeerResponse.Username))
                                {
                                    var (connection, remoteToken) = await SoulseekClient.PeerConnectionManager.GetTransferConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                                    var download = SoulseekClient.Downloads.Values.FirstOrDefault(v => v.RemoteToken == remoteToken && v.Username == connectToPeerResponse.Username);

                                    if (download != default(TransferInternal))
                                    {
                                        Diagnostic.Debug($"Solicited inbound transfer connection to {download.Username} ({connection.IPEndPoint}) for token {download.Token} (remote: {download.RemoteToken}) established. (id: {connection.Id})");
                                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken), connection);
                                    }
                                    else
                                    {
                                        Diagnostic.Debug($"Transfer ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for remote token {connectToPeerResponse.Token} does not match any waiting downloads, discarding.");
                                        connection.Disconnect("Unknown transfer");
                                    }
                                }
                                else
                                {
                                    throw new SoulseekClientException($"Unexpected transfer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}); Ignored");
                                }
                            }
                            else if (connectToPeerResponse.Type == Constants.ConnectionType.Peer)
                            {
                                Diagnostic.Debug($"Received message ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint})");
                                await SoulseekClient.PeerConnectionManager.GetOrAddMessageConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                            }
                            else if (connectToPeerResponse.Type == Constants.ConnectionType.Distributed)
                            {
                                Diagnostic.Debug($"Received distributed ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint})");
                                await SoulseekClient.DistributedConnectionManager.AddChildConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                            }
                            else
                            {
                                throw new MessageException($"Unknown Connect To Peer connection type '{connectToPeerResponse.Type}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Debug($"Error handling ConnectToPeer response from {connectToPeerResponse?.Username} ({connectToPeerResponse?.IPEndPoint}): {ex.Message}");
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
                        var pm = PrivateMessageNotification.FromByteArray(message);
                        PrivateMessageReceived?.Invoke(this, new PrivateMessageReceivedEventArgs(pm));

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
                        var roomData = RoomJoinResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code, roomData.Name), roomData);
                        break;

                    case MessageCode.Server.LeaveRoom:
                        var leaveRoomResponse = RoomLeaveResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(code, leaveRoomResponse.RoomName));
                        break;

                    case MessageCode.Server.SayInChatRoom:
                        var roomMessage = RoomMessageNotification.FromByteArray(message);
                        RoomMessageReceived?.Invoke(this, new RoomMessageReceivedEventArgs(roomMessage));
                        break;

                    case MessageCode.Server.UserJoinedRoom:
                        var joinNotification = RoomJoinedNotification.FromByteArray(message);
                        RoomJoined?.Invoke(this, new RoomJoinedEventArgs(joinNotification));
                        break;

                    case MessageCode.Server.UserLeftRoom:
                        var leftNotification = RoomLeftNotification.FromByteArray(message);
                        RoomLeft?.Invoke(this, new RoomLeftEventArgs(leftNotification));
                        break;

                    case MessageCode.Server.KickedFromServer:
                        KickedFromServer?.Invoke(this, EventArgs.Empty);
                        break;

                    case MessageCode.Server.FileSearch:
                        var searchRequest = ServerSearchRequest.FromByteArray(message);

                        // sometimes (most of the time?) a room search will result in a request to ourselves (assuming we are
                        // joined to it)
                        if (searchRequest.Username == SoulseekClient.Username)
                        {
                            break;
                        }

                        if (SoulseekClient.Options.SearchResponseResolver == default)
                        {
                            break;
                        }

                        try
                        {
                            var searchResponse = await SoulseekClient.Options.SearchResponseResolver(searchRequest.Username, searchRequest.Token, SearchQuery.FromText(searchRequest.Query)).ConfigureAwait(false);

                            if (searchResponse != null && searchResponse.FileCount + searchResponse.LockedFileCount > 0)
                            {
                                var endpoint = await SoulseekClient.GetUserEndPointAsync(searchRequest.Username).ConfigureAwait(false);

                                var peerConnection = await SoulseekClient.PeerConnectionManager.GetOrAddMessageConnectionAsync(searchRequest.Username, endpoint, CancellationToken.None).ConfigureAwait(false);
                                await peerConnection.WriteAsync(searchResponse.ToByteArray()).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Error resolving search response for query '{searchRequest.Query}' requested by {searchRequest.Username} with token {searchRequest.Token}: {ex.Message}", ex);
                        }

                        break;

                    // if we fail to connect to a distributed parent in a timely manner, the server will begin to send us
                    // distributed search requests directly. forward these to the distributed message handler.
                    case MessageCode.Server.SearchRequest:
                        SoulseekClient.DistributedMessageHandler.HandleMessageRead(SoulseekClient.ServerConnection, message);
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

        /// <summary>
        ///     Handles outgoing messages, post send.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance to which the message was sent.</param>
        /// <param name="args">The message event args.</param>
        public void HandleMessageWritten(object sender, MessageEventArgs args)
        {
            var code = new MessageReader<MessageCode.Server>(args.Message).ReadCode();
            Diagnostic.Debug($"Server message sent: {code}");
        }
    }
}