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
        /// <param name="status">The status of the peer.</param>
        /// <param name="averageSpeed">The average upload speed of the peer.</param>
        /// <param name="downloadCount">The number of active peer downloads.</param>
        /// <param name="fileCount">The number of files shared by the peer.</param>
        /// <param name="directoryCount">The number of directories shared by the peer.</param>
        /// <param name="countryCode">The peer's country code.</param>
        internal AddUserResponse(string username, bool exists, UserStatus? status, int? averageSpeed, long? downloadCount, int? fileCount, int? directoryCount, string countryCode)
        {
            Username = username;
            Exists = exists;
            Status = status;
            AverageSpeed = averageSpeed;
            DownloadCount = downloadCount;
            FileCount = fileCount;
            DirectoryCount = directoryCount;
            CountryCode = countryCode;
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
        ///     Gets the status of the peer (0 = offline, 1 = away, 2 = online).
        /// </summary>
        public UserStatus? Status { get; }

        /// <summary>
        ///     Gets the average upload speed of the peer.
        /// </summary>
        public int? AverageSpeed { get; }

        /// <summary>
        ///     Gets the number of active peer downloads.
        /// </summary>
        public long? DownloadCount { get; }

        /// <summary>
        ///     Gets the number of files shared by the peer.
        /// </summary>
        public int? FileCount { get; }

        /// <summary>
        ///     Gets the number of directories shared by the peer.
        /// </summary>
        public int? DirectoryCount { get; }

        /// <summary>
        ///     Gets the peer's country code.
        /// </summary>
        public string CountryCode { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="AddUserResponse"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static AddUserResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerAddUser)
            {
                throw new MessageException($"Message Code mismatch creating Add User Response (expected: {(int)MessageCode.ServerAddUser}, received: {(int)reader.Code}.");
            }

            var username = reader.ReadString();
            var exists = reader.ReadByte() > 0;

            UserStatus? status = null;
            int? averageSpeed = null;
            long? downloadCount = null;
            int? fileCount = null;
            int? directoryCount = null;
            string countryCode = null;

            if (exists)
            {
                status = (UserStatus)reader.ReadInteger();
                averageSpeed = reader.ReadInteger();
                downloadCount = reader.ReadLong();
                fileCount = reader.ReadInteger();
                directoryCount = reader.ReadInteger();
                countryCode = reader.ReadString();
            }

            return new AddUserResponse(username, exists, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode);
        }
    }
}