// <copyright file="SearchOptions.cs" company="JP Dillingham">
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

    public class SearchOptions
    {
        public int Timeout { get; set; } = 15;
        public int FileLimit { get; set; } = 10000;

        public bool FilterResponses { get; set; } = true;
        public int MinimumResponseFileCount { get; set; } = 1;
        public int MinimumPeerFreeUploadSlots { get; set; }
        public int MaximumPeerQueueLength { get; set; } = 1000000;
        public int MinimumPeerUploadSpeed { get; set; }

        public IEnumerable<string> IgnoredFileExtensions { get; set; }

        public bool FilterFiles { get; set; } = false;
        public int MinimumFileBitRate { get; set; }
        public int MinimumFileSize { get; set; }
        public int MinimumFileLength { get; set; }
        public int MinimumFileSampleRate { get; set; }
        public int MinimumFileBitDepth { get; set; }
        public bool IncludeConstantBitRate { get; set; } = true;
        public bool IncludeVariableBitRate { get; set; } = true;
    }
}
