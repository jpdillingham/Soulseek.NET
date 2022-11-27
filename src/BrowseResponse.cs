// <copyright file="BrowseResponse.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     A response to a peer browse request.
    /// </summary>
    public class BrowseResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseResponse"/> class.
        /// </summary>
        /// <param name="directoryList">The optional directory list.</param>
        /// <param name="lockedDirectoryList">The optional locked directory list.</param>
        public BrowseResponse(IEnumerable<Directory> directoryList = null, IEnumerable<Directory> lockedDirectoryList = null)
        {
            Directories = (directoryList?.ToList() ?? new List<Directory>()).AsReadOnly();
            DirectoryCount = Directories.Count;

            LockedDirectories = (lockedDirectoryList?.ToList() ?? new List<Directory>()).AsReadOnly();
            LockedDirectoryCount = LockedDirectories.Count;
        }

        /// <summary>
        ///     Gets the list of directories.
        /// </summary>
        public IReadOnlyCollection<Directory> Directories { get; }

        /// <summary>
        ///     Gets the number of directories.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the list of locked directories.
        /// </summary>
        public IReadOnlyCollection<Directory> LockedDirectories { get; }

        /// <summary>
        ///     Gets the number of locked directories.
        /// </summary>
        public int LockedDirectoryCount { get; }

        /// <summary>
        ///     Serializes the response to the raw byte array sent over the network.
        /// </summary>
        /// <returns>The serialized response.</returns>
        public byte[] ToByteArray()
        {
            return BrowseResponseFactory.ToByteArray(this);
        }
    }

    /// <summary>
    ///     A raw response to a peer browse request, presented as a stream of binary data.
    /// </summary>
    /// <remarks>
    ///     This is a hack to simulate a discriminated union.
    /// </remarks>
    public class RawBrowseResponse : BrowseResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RawBrowseResponse"/> class.
        /// </summary>
        /// <remarks>
        ///     The input stream will be disposed after the response is written.
        /// </remarks>
        /// <param name="length">The length of the response, in bytes.</param>
        /// <param name="stream">The raw input stream.</param>
        public RawBrowseResponse(long length, Stream stream)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The response length must be greater than zero");
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "The specified input stream is null");
            }

            Length = length;
            Stream = stream;
        }

        /// <summary>
        ///     Gets the length of the response, in bytes.
        /// </summary>
        public long Length { get; }

        /// <summary>
        ///     Gets the raw input stream providing the response.
        /// </summary>
        public Stream Stream { get; }
    }
}