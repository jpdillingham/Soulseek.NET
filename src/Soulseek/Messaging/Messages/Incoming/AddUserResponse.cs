// <copyright file="AddUserResponse.cs" company="JP Dillingham">
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
    ///     The response to an add user request.
    /// </summary>
    public sealed class AddUserResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the added peer.</param>
        /// <param name="exists">A value indicating whether the username exists on the network.</param>
        /// <param name="user">If <see cref="Exists"/>, the user.</param>
        internal AddUserResponse(string username, bool exists, User user)
        {
            Username = username;
            Exists = exists;
            User = user;
        }

        /// <summary>
        ///     Gets the username of the added peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets a value indicating whether the username exists on the network.
        /// </summary>
        public bool Exists { get; }

        /// <summary>
        ///     Gets the user, if <see cref="Exists"/>.
        /// </summary>
        public User User { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="AddUserResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        internal static AddUserResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.AddUser)
            {
                throw new MessageException($"Message Code mismatch creating Add User Response (expected: {(int)MessageCode.Server.AddUser}, received: {(int)code}.");
            }

            var username = reader.ReadString();
            var b = reader.ReadByte();
            var exists = b > 0;

            User user = null;

            if (exists)
            {
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

                user = new User(status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode: countryCode);
            }

            return new AddUserResponse(username, exists, user);
        }
    }
}