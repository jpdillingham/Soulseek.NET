// <copyright file="IServerMessageHandler.cs" company="JP Dillingham">
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

    /// <summary>
    ///     Handles incoming messages from the server connection.
    /// </summary>
    internal interface IServerMessageHandler : IMessageHandler
    {
        /// <summary>
        ///     Occurs when the client is forcefully disconnected from the server, probably because another client logged in with
        ///     the same credentials.
        /// </summary>
        event EventHandler KickedFromServer;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        event EventHandler<PrivateMessageEventArgs> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when the server sends a list of privileged users.
        /// </summary>
        event EventHandler<PrivilegedUserListReceivedEventArgs> PrivilegedUserListReceived;

        /// <summary>
        ///     Occurs when a user joins a chat room.
        /// </summary>
        event EventHandler<RoomJoinedEventArgs> RoomJoined;

        /// <summary>
        ///     Occurs when a user leaves a chat room.
        /// </summary>
        event EventHandler<RoomLeftEventArgs> RoomLeft;

        /// <summary>
        ///     Occurs when a chat room message is received.
        /// </summary>
        event EventHandler<RoomMessageEventArgs> RoomMessageReceived;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        event EventHandler<UserStatusChangedEventArgs> UserStatusChanged;
    }
}