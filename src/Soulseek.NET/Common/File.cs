// <copyright file="File.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A file within search and browse results.
    /// </summary>
    public sealed class File
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="File"/> class.
        /// </summary>
        /// <param name="code">The file code.</param>
        /// <param name="filename">The file name.</param>
        /// <param name="extension">The file extension.</param>
        /// <param name="size">The file size in bytes.</param>
        /// <param name="attributeCount">The number of file <see cref="FileAttribute"/> s.</param>
        /// <param name="attributeList">The optional list of <see cref="FileAttribute"/> s.</param>
        public File(int code, string filename, long size, string extension, int attributeCount, IEnumerable<FileAttribute> attributeList = null)
        {
            Code = code;
            Filename = filename;
            Size = size;
            Extension = extension;
            AttributeCount = attributeCount;

            AttributeList = attributeList ?? Array.Empty<FileAttribute>();
        }

        /// <summary>
        ///     Gets the number of file <see cref="FileAttribute"/> s.
        /// </summary>
        public int AttributeCount { get; internal set; }

        /// <summary>
        ///     Gets the file attributes.
        /// </summary>
        public IReadOnlyCollection<FileAttribute> Attributes => AttributeList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.BitDepth"/> attribute.
        /// </summary>
        public int? BitDepth => GetAttributeValue(FileAttributeType.BitDepth);

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.BitRate"/> attribute.
        /// </summary>
        public int? BitRate => GetAttributeValue(FileAttributeType.BitRate);

        /// <summary>
        ///     Gets the file code.
        /// </summary>
        public int Code { get; }

        /// <summary>
        ///     Gets the file extension.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        ///     Gets the file name.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.Length"/> attribute.
        /// </summary>
        public int? Length => GetAttributeValue(FileAttributeType.Length);

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.SampleRate"/> attribute.
        /// </summary>
        public int? SampleRate => GetAttributeValue(FileAttributeType.SampleRate);

        /// <summary>
        ///     Gets the file size in bytes.
        /// </summary>
        public long Size { get; }

        /// <summary>
        ///     Gets the internal list of file attributes.
        /// </summary>
        private IEnumerable<FileAttribute> AttributeList { get; }

        /// <summary>
        ///     Returns the value of the specified attribute <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The attribute to return.</param>
        /// <returns>The value of the specified attribute.</returns>
        public int? GetAttributeValue(FileAttributeType type)
        {
            return AttributeList.FirstOrDefault(a => a.Type == type)?.Value;
        }
    }
}