// <copyright file="UserStatsResponse.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Messaging.Messages
{
    /// <summary>
    ///     The response to a peer stats request.
    /// </summary>
    internal sealed class UserStatsResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatsResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="averageSpeed">The average upload speed of the user.</param>
        /// <param name="uploadCount">The number of uploads tracked by the server for this user.</param>
        /// <param name="fileCount">The number of files shared by the user.</param>
        /// <param name="directoryCount">The number of directories shared by the user.</param>
        public UserStatsResponse(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            Username = username;
            AverageSpeed = averageSpeed;
            UploadCount = uploadCount;
            FileCount = fileCount;
            DirectoryCount = directoryCount;
        }

        /// <summary>
        ///     Gets the average upload speed of the user.
        /// </summary>
        public int AverageSpeed { get; }

        /// <summary>
        ///     Gets the number of directories shared by the user.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the number of files shared by the user.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the number of uploads tracked by the server for this user.
        /// </summary>
        public long UploadCount { get; }

        /// <summary>
        ///     Gets the username of the user.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserStatsResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserStatsResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetUserStats)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserStatsResponse)} (expected: {(int)MessageCode.Server.GetUserStats}, received: {(int)code})");
            }

            var username = reader.ReadString();
            var averageSpeed = reader.ReadInteger();
            var uploadCount = reader.ReadLong();
            var fileCount = reader.ReadInteger();
            var directoryCount = reader.ReadInteger();

            return new UserStatsResponse(username, averageSpeed, uploadCount, fileCount, directoryCount);
        }
    }
}