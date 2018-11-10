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

    /// <summary>
    ///     Options for the search operation.
    /// </summary>
    public class SearchOptions : SoulseekClientOptions
    {
        /// <summary>
        ///     Gets or sets the number of allowed concurrent peer connections.
        /// </summary>
        public int ConcurrentPeerConnections { get; set; } = 500;

        public int SearchTimeout { get; set; } = 15;
        public int FileLimit { get; set; } = 10000;

        public bool FilterResponses { get; set; } = true;
        public int MinimumResponseFileCount { get; set; } = 1;
        public int MinimumPeerFreeUploadSlots { get; set; } = 0;
        public int MaximumPeerQueueLength { get; set; } = 1000000;
        public int MinimumPeerUploadSpeed { get; set; } = 0;

        public IEnumerable<string> IgnoredFileExtensions { get; set; }

        public bool FilterFiles { get; set; } = false;
        public int MinimumFileBitRate { get; set; } = 128;
        public int MinimumFileSize { get; set; } = 0;
        public int MinimumFileLength { get; set; } = 0;
        public int MinimumFileSampleRate { get; set; } = 0;
        public int MinimumFileBitDepth { get; set; } = 0;
        public bool IncludeConstantBitRate { get; set; } = true;
        public bool IncludeVariableBitRate { get; set; } = true;
    }
}
