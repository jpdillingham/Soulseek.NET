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
    using System.Collections.Generic;
    using System.Linq;

    public sealed class File
    {
        internal File()
        {
        }

        public int AttributeCount { get; internal set; }
        public IEnumerable<FileAttribute> Attributes => AttributeList.AsReadOnly();
        public int Code { get; internal set; }
        public string Extension { get; internal set; }
        public string Filename { get; internal set; }
        public long Size { get; internal set; }
        internal List<FileAttribute> AttributeList { get; set; } = new List<FileAttribute>();

        public int? GetAttributeValue(FileAttributeType type)
        {
            return AttributeList.Where(a => a.Type == type).SingleOrDefault()?.Value;
        }
    }
}