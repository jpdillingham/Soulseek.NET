// <copyright file="UserEventArgs.cs" company="JP Dillingham">
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
    ///     Generic event arguments for user events.
    /// </summary>
    public class UserEventArgs : EventArgs
    {
    }

    /// <summary>
    ///     Event arguments for events raised by user state changed events.
    /// </summary>
    public class UserStatusChangedEventArgs : UserEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusChangedEventArgs"/> class.
        /// </summary>
        /// <param name="userStatusResponse">The status response which generated the event.</param>
        public UserStatusChangedEventArgs(UserStatusResponse userStatusResponse)
        {
            Username = userStatusResponse.Username;
            Status = userStatusResponse.Status;
            Privileged = userStatusResponse.Privileged;
        }

        /// <summary>
        ///     Gets a value indicating whether the peer is privileged.
        /// </summary>
        public bool Privileged { get; }

        /// <summary>
        ///     Gets the status of the peer.
        /// </summary>
        public UserStatus Status { get; }

        /// <summary>
        ///     Gets the username of the peer.
        /// </summary>
        public string Username { get; }
    }
}