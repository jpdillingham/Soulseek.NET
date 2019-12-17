// <copyright file="BrowseEventArgs.cs" company="JP Dillingham">
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

    public class BrowseEventArgs : EventArgs
    {
        public BrowseEventArgs(string username)
        {
            Username = username;
        }

        public string Username { get; }
    }

    public class BrowseProgressUpdatedEventArgs : BrowseEventArgs
    {
        public BrowseProgressUpdatedEventArgs(string username, long bytesTransferred, long size)
            : base(username)
        {
            BytesTransferred = bytesTransferred;
            Size = size;
        }

        /// <summary>
        ///     Gets the total number of bytes transferred.
        /// </summary>
        public long BytesTransferred { get; }

        /// <summary>
        ///     Gets the number of remaining bytes to be transferred.
        /// </summary>
        public long BytesRemaining => Size - BytesTransferred;

        /// <summary>
        ///     Gets the progress of the data transfer as a percentage of current and total data length.
        /// </summary>
        public double PercentComplete => (BytesTransferred / (double)Size) * 100d;

        /// <summary>
        ///     Gets the total expected length of the data transfer.
        /// </summary>
        public long Size { get; }
    }
}
