// <copyright file="PrivateMessage.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System;
    using Soulseek.Exceptions;

    /// <summary>
    ///     An incoming private message.
    /// </summary>
    public sealed class PrivateMessageNotification
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateMessageNotification"/> class.
        /// </summary>
        /// <param name="id">The unique id of the message.</param>
        /// <param name="timestamp">The timestamp at which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        /// <param name="isAdmin">A value indicating whether the message was sent by an administrator.</param>
        internal PrivateMessageNotification(int id, DateTime timestamp, string username, string message, bool isAdmin = false)
        {
            Id = id;
            Timestamp = timestamp;
            Username = username;
            Message = message;
            IsAdmin = isAdmin;
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
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the timestamp at which the message was sent.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        ///     Gets the username of the user which sent the message.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PrivateMessageNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        internal static PrivateMessageNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.PrivateMessage)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PrivateMessageNotification)} (expected: {(int)MessageCode.Server.PrivateMessage}, received: {(int)code}.");
            }

            var id = reader.ReadInteger();

            var timestampSeconds = reader.ReadInteger();

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = epoch.AddSeconds(timestampSeconds).ToLocalTime();

            var username = reader.ReadString();
            var msg = reader.ReadString();
            var isAdmin = reader.ReadByte() == 1;

            return new PrivateMessageNotification(id, timestamp, username, msg, isAdmin);
        }
    }
}