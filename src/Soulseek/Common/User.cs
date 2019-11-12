// <copyright file="User.cs" company="JP Dillingham">
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

namespace Soulseek
{
    /// <summary>
    ///     A file attribute.
    /// </summary>
    public sealed class User
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="status">The status of the user.</param>
        /// <param name="averageSpeed">The average upload speed of the user.</param>
        /// <param name="downloadCount">The number of active user downloads.</param>
        /// <param name="fileCount">The number of files shared by the user.</param>
        /// <param name="directoryCount">The number of directories shared by the user.</param>
        /// <param name="slotsFree">The number of the user's free download slots, if provided.</param>
        /// <param name="countryCode">The user's country code, if provided.</param>
        public User(UserStatus status, int averageSpeed, long downloadCount, int fileCount, int directoryCount, bool? slotsFree = null, string countryCode = null)
        {
            Status = status;
            AverageSpeed = averageSpeed;
            DownloadCount = downloadCount;
            FileCount = fileCount;
            DirectoryCount = directoryCount;
            SlotsFree = slotsFree;
            CountryCode = countryCode;
        }

        /// <summary>
        ///     Gets the status of the user (0 = offline, 1 = away, 2 = online).
        /// </summary>
        public UserStatus Status { get; }

        /// <summary>
        ///     Gets the average upload speed of the user.
        /// </summary>
        public int AverageSpeed { get; }

        /// <summary>
        ///     Gets the number of active user downloads.
        /// </summary>
        public long DownloadCount { get; }

        /// <summary>
        ///     Gets the number of files shared by the user.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the number of directories shared by the user.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets a value indicating whether the user has download slots available.
        /// </summary>
        public bool? SlotsFree { get; }

        /// <summary>
        ///     Gets the user's country code, if provided.
        /// </summary>
        public string CountryCode { get; }
    }
}