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

namespace Soulseek.NET
{
    using System.Collections.Generic;

    /// <summary>
    ///     A file directory within a peer's shared files.
    /// </summary>
    public sealed class Directory
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Directory"/> class.
        /// </summary>
        internal Directory()
        {
        }

        /// <summary>
        ///     Gets the directory name.
        /// </summary>
        public string Directoryname { get; internal set; }

        /// <summary>
        ///     Gets the number of files within the directory.
        /// </summary>
        public int FileCount { get; internal set; }

        /// <summary>
        ///     Gets the collection of files contained within the directory.
        /// </summary>
        public IEnumerable<File> Files => FileList.AsReadOnly();

        /// <summary>
        ///     Gets or sets the list of files contained within the directory.
        /// </summary>
        internal List<File> FileList { get; set; } = new List<File>();
    }
}