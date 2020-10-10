// <copyright file="SoulseekClientEventArgs.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Generic event arguments for client events.
    /// </summary>
    public abstract class SoulseekClientEventArgs : EventArgs
    {
    }

    /// <summary>
    ///     Event arguments for events raised upon receipt of a global message.
    /// </summary>
    public class GlobalMessageReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message content.</param>
        public GlobalMessageReceivedEventArgs(string message)
        {
            Message = message;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal GlobalMessageReceivedEventArgs(GlobalMessageNotification notification)
            : this(notification.Message)
        {
        }

        /// <summary>
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }
    }

    /// <summary>
    ///     Event arguments for events raised upon receipt of a private message.
    /// </summary>
    public class PrivateMessageReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="id">The unique id of the message.</param>
        /// <param name="timestamp">The UTC timestamp at which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        /// <param name="replayed">A value indicating whether the message was replayed from a previous time.</param>
        public PrivateMessageReceivedEventArgs(int id, DateTime timestamp, string username, string message, bool replayed)
        {
            Id = id;
            Timestamp = timestamp;
            Username = username;
            Message = message;
            Replayed = replayed;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal PrivateMessageReceivedEventArgs(PrivateMessageNotification notification)
            : this(notification.Id, notification.Timestamp, notification.Username, notification.Message, notification.Replayed)
        {
        }

        /// <summary>
        ///     Gets the unique id of the message.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets a value indicating whether the message was replayed from a previous time.
        /// </summary>
        public bool Replayed { get; }

        /// <summary>
        ///     Gets the UTC timestamp at which the message was sent.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        ///     Gets the username of the user which sent the message.
        /// </summary>
        public string Username { get; }
    }

    /// <summary>
    ///     Event arguments for events raised upon notification of new privileges.
    /// </summary>
    public class PrivilegeNotificationReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivilegeNotificationReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username of the new privileged user.</param>
        /// <param name="id">The unique id of the notification, if applicable.</param>
        public PrivilegeNotificationReceivedEventArgs(string username, int? id = null)
        {
            Username = username;
            Id = id;
        }

        /// <summary>
        ///     Gets the username of the new privileged user.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets the unique id of the notification, if applicable.
        /// </summary>
        public int? Id { get; }

        /// <summary>
        ///     Gets a value indicating whether the notification must be acknowleged.
        /// </summary>
        public bool RequiresAcknowlegement => Id.HasValue;
    }

    /// <summary>
    ///     Event arguments for events raised upon receipt of the list of privileged users.
    /// </summary>
    public class PrivilegedUserListReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivilegedUserListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="usernames">The list usernames of privilegd users.</param>
        public PrivilegedUserListReceivedEventArgs(IReadOnlyCollection<string> usernames)
        {
            Usernames = usernames;
        }

        /// <summary>
        ///     Gets the list of usernames of privileged users.
        /// </summary>
        public IReadOnlyCollection<string> Usernames { get; }
    }

    /// <summary>
    ///     Event arguments for events raised upon receipt of the list of rooms.
    /// </summary>
    public class RoomListReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="rooms">The list of rooms.</param>
        public RoomListReceivedEventArgs(IReadOnlyCollection<Room> rooms)
        {
            Rooms = rooms;
        }

        /// <summary>
        ///     Gets the list of rooms.
        /// </summary>
        public IReadOnlyCollection<Room> Rooms { get; }
    }

    /// <summary>
    ///     Event arguments for events raised by client disconnect.
    /// </summary>
    public class SoulseekClientDisconnectedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientDisconnectedEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message describing the reason for the disconnect.</param>
        /// <param name="exception">The Exception associated with the disconnect, if applicable.</param>
        public SoulseekClientDisconnectedEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the Exception associated with change in state, if applicable.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the message describing the reason for the disconnect.
        /// </summary>
        public string Message { get; }
    }

    /// <summary>
    ///     Event arguments for events raised by a change in client state.
    /// </summary>
    public class SoulseekClientStateChangedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the client.</param>
        /// <param name="state">The current state of the client.</param>
        /// <param name="message">The message associated with the change in state, if applicable.</param>
        /// <param name="exception">The Exception associated with the change in state, if applicable.</param>
        public SoulseekClientStateChangedEventArgs(SoulseekClientStates previousState, SoulseekClientStates state, string message = null, Exception exception = null)
        {
            PreviousState = previousState;
            State = state;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the Exception associated with change in state, if applicable.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the message associated with the change in state, if applicable.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the previous client state.
        /// </summary>
        public SoulseekClientStates PreviousState { get; }

        /// <summary>
        ///     Gets the current client state.
        /// </summary>
        public SoulseekClientStates State { get; }
    }
}