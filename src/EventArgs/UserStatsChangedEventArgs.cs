// <copyright file="UserStatsChangedEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Event arguments for events raised by user stats changed events.
    /// </summary>
    public class UserStatsChangedEventArgs : UserEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatsChangedEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="averageSpeed">The average upload speed of the user.</param>
        /// <param name="uploadCount">The number of uploads tracked by the server for this user.</param>
        /// <param name="fileCount">The number of files shared by the user.</param>
        /// <param name="directoryCount">The number of directories shared by the user.</param>
        public UserStatsChangedEventArgs(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
            : base(username)
        {
            AverageSpeed = averageSpeed;
            UploadCount = uploadCount;
            FileCount = fileCount;
            DirectoryCount = directoryCount;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatsChangedEventArgs"/> class.
        /// </summary>
        /// <param name="userStatsResponse">The stats response which generated the event.</param>
        internal UserStatsChangedEventArgs(UserStatsResponse userStatsResponse)
            : this(userStatsResponse.Username, userStatsResponse.AverageSpeed, userStatsResponse.UploadCount, userStatsResponse.FileCount, userStatsResponse.DirectoryCount)
        {
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
    }
}