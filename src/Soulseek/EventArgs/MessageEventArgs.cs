// <copyright file="MessageEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Generic event arguments for message events.
    /// </summary>
    public abstract class MessageEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        protected MessageEventArgs(string username, string message)
        {
            Username = username;
            Message = message;
        }

        /// <summary>
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the username of the user which sent the message.
        /// </summary>
        public string Username { get; }
    }

    /// <summary>
    ///     Event arguments for events raised upon receipt of a private message.
    /// </summary>
    public class PrivateMessageEventArgs : MessageEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateMessageEventArgs"/> class.
        /// </summary>
        /// <param name="id">The unique id of the message.</param>
        /// <param name="timestamp">The timestamp at which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        /// <param name="isAdmin">A value indicating whether the message was sent by an administrator.</param>
        public PrivateMessageEventArgs(int id, DateTime timestamp, string username, string message, bool isAdmin = false)
            : base(username, message)
        {
            Id = id;
            Timestamp = timestamp;
            IsAdmin = isAdmin;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateMessageEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal PrivateMessageEventArgs(PrivateMessageNotification notification)
            : this(notification.Id, notification.Timestamp, notification.Username, notification.Message, notification.IsAdmin)
        {
        }

        /// <summary>
        ///     Gets the unique id of the message.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     Gets a value indicating whether the message was sent by an administrator.
        /// </summary>
        public bool IsAdmin { get; }

        /// <summary>
        ///     Gets the timestamp at which the message was sent.
        /// </summary>
        public DateTime Timestamp { get; }
    }

    /// <summary>
    ///     Event arguments for events raised upon receipt of a chat room message.
    /// </summary>
    public class RoomMessageEventArgs : MessageEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        public RoomMessageEventArgs(string roomName, string username, string message)
            : base(username, message)
        {
            RoomName = roomName;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomMessageEventArgs(RoomMessageNotification notification)
            : this(notification.RoomName, notification.Username, notification.Message)
        {
        }

        /// <summary>
        ///     Gets the name of the room in which the message was sent.
        /// </summary>
        public string RoomName { get; }
    }
}