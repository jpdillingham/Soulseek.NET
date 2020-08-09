// <copyright file="BrowseResponse.cs" company="JP Dillingham">
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
    ///     The response to a peer browse request.
    /// </summary>
    public sealed class BrowseResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseResponse"/> class.
        /// </summary>
        /// <param name="directoryList">The optional directory list.</param>
        /// <param name="lockedDirectoryList">The optional locked directory list.</param>
        public BrowseResponse(IEnumerable<Directory> directoryList = null, IEnumerable<Directory> lockedDirectoryList = null)
        {
            DirectoryList = directoryList ?? Enumerable.Empty<Directory>();
            DirectoryCount = DirectoryList.Count();

            LockedDirectoryList = lockedDirectoryList ?? Enumerable.Empty<Directory>();
            LockedDirectoryCount = LockedDirectoryList.Count();
        }

        /// <summary>
        ///     Gets the list of directories.
        /// </summary>
        public IReadOnlyCollection<Directory> Directories => DirectoryList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the number of directories.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the list of locked directories.
        /// </summary>
        public IReadOnlyCollection<Directory> LockedDirectories => LockedDirectoryList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the number of locked directories.
        /// </summary>
        public int LockedDirectoryCount { get; }

        private IEnumerable<Directory> DirectoryList { get; }
        private IEnumerable<Directory> LockedDirectoryList { get; }
    }
}