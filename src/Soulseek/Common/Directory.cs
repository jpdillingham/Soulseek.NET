// <copyright file="Directory.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A file directory within a peer's shared files.
    /// </summary>
    public sealed class Directory
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Directory"/> class.
        /// </summary>
        /// <param name="directoryname">The directory name.</param>
        /// <param name="fileCount">The number of files.</param>
        /// <param name="fileList">The optional list of <see cref="File"/> s.</param>
        public Directory(string directoryname, int fileCount, IEnumerable<File> fileList = null)
        {
            Directoryname = directoryname;
            FileCount = fileCount;

            FileList = fileList ?? Array.Empty<File>();
        }

        /// <summary>
        ///     Gets the directory name.
        /// </summary>
        public string Directoryname { get; }

        /// <summary>
        ///     Gets the number of files within the directory.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the collection of files contained within the directory.
        /// </summary>
        public IReadOnlyCollection<File> Files => FileList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the list of files contained within the directory.
        /// </summary>
        private IEnumerable<File> FileList { get; }
    }
}