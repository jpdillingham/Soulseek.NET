// <copyright file="RoomJoinNotification.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using Soulseek.Exceptions;

    /// <summary>
    ///     An incoming notification that a user has joined a chat room.
    /// </summary>
    public sealed class RoomJoinedNotification
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomJoinedNotification"/> class.
        /// </summary>
        internal RoomJoinedNotification(string roomName, string username, UserData userData)
        {
            RoomName = roomName;
            Username = username;
            UserData = userData;
        }

        public string RoomName { get; }
        public string Username { get; }
        public UserData UserData { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="RoomJoinedNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static RoomJoinedNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.UserJoinedRoom)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(RoomJoinedNotification)} (expected: {(int)MessageCode.Server.UserJoinedRoom}, received: {(int)code}");
            }

            var roomName = reader.ReadString();
            var username = reader.ReadString();

            var status = (UserStatus)reader.ReadInteger();
            var averageSpeed = reader.ReadInteger();
            var downloadCount = reader.ReadLong();
            var fileCount = reader.ReadInteger();
            var directoryCount = reader.ReadInteger();
            string countryCode = null;

            if (reader.HasMoreData)
            {
                countryCode = reader.ReadString();
            }

            var userData = new UserData(status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode: countryCode);

            return new RoomJoinedNotification(roomName, username, userData);
        }
    }
}