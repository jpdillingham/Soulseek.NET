// <copyright file="GlobalMessageReceivedEventArgs.cs" company="JP Dillingham">
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
}