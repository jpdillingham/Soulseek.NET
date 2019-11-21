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
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Generic event arguments for client events.
    /// </summary>
    public abstract class SoulseekClientEventArgs : EventArgs
    {
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
        public SoulseekClientStateChangedEventArgs(SoulseekClientStates previousState, SoulseekClientStates state, string message = null)
        {
            PreviousState = previousState;
            State = state;
            Message = message;
        }

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

    /// <summary>
    ///     Event arguments for events raised upon receipt of a private message.
    /// </summary>
    public class PrivateMessageEventArgs : SoulseekClientEventArgs
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
        {
            Id = id;
            Timestamp = timestamp;
            Username = username;
            Message = message;
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
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the username of the user which sent the message.
        /// </summary>
        public string Username { get; }

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
}