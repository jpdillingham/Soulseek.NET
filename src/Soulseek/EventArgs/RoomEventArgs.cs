// <copyright file="RoomEventArgs.cs" company="JP Dillingham">
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
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Generic event arguments for chat room events.
    /// </summary>
    public abstract class RoomEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the event took place.</param>
        /// <param name="username">The username of the user associated with the event.</param>
        protected RoomEventArgs(string roomName, string username)
        {
            RoomName = roomName;
            Username = username;
        }

        /// <summary>
        ///     Gets the name of the room in which the event took place.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the username of the user associated with the event.
        /// </summary>
        public string Username { get; }
    }

    /// <summary>
    ///     Event arguments for events raised upon the join of a user to a chat room.
    /// </summary>
    public class RoomJoinedEventArgs : RoomEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomJoinedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the event took place.</param>
        /// <param name="username">The username of the user associated with the event.</param>
        /// <param name="userData">The user's data.</param>
        public RoomJoinedEventArgs(string roomName, string username, UserData userData)
            : base(roomName, username)
        {
            UserData = userData;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomJoinedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomJoinedEventArgs(RoomJoinedNotification notification)
            : this(notification.RoomName, notification.Username, notification.UserData)
        {
        }

        /// <summary>
        ///     Gets the user's data.
        /// </summary>
        public UserData UserData { get; }
    }

    /// <summary>
    ///     Event arguments for events raised upon the departure of a user from a chat room.
    /// </summary>
    public class RoomLeftEventArgs : RoomEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomLeftEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the event took place.</param>
        /// <param name="username">The username of the user associated with the event.</param>
        public RoomLeftEventArgs(string roomName, string username)
            : base(roomName, username)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomLeftEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomLeftEventArgs(RoomLeftNotification notification)
            : this(notification.RoomName, notification.Username)
        {
        }
    }

    /// <summary>
    ///     Event arguments for events raised upon receipt of a chat room message.
    /// </summary>
    public class RoomMessageReceivedEventArgs : RoomEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        public RoomMessageReceivedEventArgs(string roomName, string username, string message)
            : base(roomName, username)
        {
            Message = message;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomMessageReceivedEventArgs(RoomMessageNotification notification)
            : this(notification.RoomName, notification.Username, notification.Message)
        {
        }

        /// <summary>
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }
    }
}
