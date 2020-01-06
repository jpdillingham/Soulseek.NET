// <copyright file="SearchResponse.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A response to a file search.
    /// </summary>
    public class SearchResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="freeUploadSlots">The number of free upload slots for the peer.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="fileList">The optional file list.</param>
        public SearchResponse(string username, int token, int freeUploadSlots, int uploadSpeed, long queueLength, IEnumerable<File> fileList = null)
        {
            Username = username;
            Token = token;
            FreeUploadSlots = freeUploadSlots;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;
            FileList = fileList ?? Enumerable.Empty<File>();
            FileCount = FileList.Count();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="fileCount">The number of files contained within the result, as counted by the original response.</param>
        /// <param name="freeUploadSlots">The number of free upload slots for the peer.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="fileList">The optional file list.</param>
        public SearchResponse(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength, IEnumerable<File> fileList = null)
        {
            Username = username;
            Token = token;
            FileCount = fileCount;
            FreeUploadSlots = freeUploadSlots;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;
            FileList = fileList ?? Enumerable.Empty<File>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="searchResponse">An existing instance from which to copy properties.</param>
        /// <param name="fileList">The file list with which to replace the existing file list.</param>
        internal SearchResponse(SearchResponse searchResponse, IEnumerable<File> fileList)
            : this(searchResponse.Username, searchResponse.Token, fileList.Count(), searchResponse.FreeUploadSlots, searchResponse.UploadSpeed, searchResponse.QueueLength, fileList)
        {
        }

        /// <summary>
        ///     Gets the number of files contained within the result, as counted by the original response from the peer and prior
        ///     to filtering. For the filtered count, check the length of <see cref="Files"/>.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the list of files.
        /// </summary>
        public IReadOnlyCollection<File> Files => FileList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the number of free upload slots for the peer.
        /// </summary>
        public int FreeUploadSlots { get; }

        /// <summary>
        ///     Gets the length of the peer's upload queue.
        /// </summary>
        public long QueueLength { get; }

        /// <summary>
        ///     Gets the unique search token.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the upload speed of the peer.
        /// </summary>
        public int UploadSpeed { get; }

        /// <summary>
        ///     Gets the username of the responding peer.
        /// </summary>
        public string Username { get; }

        private IEnumerable<File> FileList { get; }
    }
}