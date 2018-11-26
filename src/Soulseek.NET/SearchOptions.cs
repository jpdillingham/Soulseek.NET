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
    public class SearchOptions
    {
        /// <summary>
        ///     Gets or sets the search timeout value, in seconds, used to determine when the search is complete. (Default = 15).
        /// </summary>
        /// <remarks>
        ///     The timeout duration is from the time of the last response.
        /// </remarks>
        public int SearchTimeout { get; set; } = 15;

        /// <summary>
        ///     Gets or sets the maximum number of search results to accept before the search is considered complete. (Default = 10,000).
        /// </summary>
        public int FileLimit { get; set; } = 10000;

        /// <summary>
        ///     Gets or sets a value indicating whether responses are to be filtered. (Default = true).
        /// </summary>
        public bool FilterResponses { get; set; } = true;

        /// <summary>
        ///     Gets or sets the minimum number of files a response must contain in order to be processed. (Default = 1).
        /// </summary>
        public int MinimumResponseFileCount { get; set; } = 1;

        /// <summary>
        ///     Gets or sets the minimum number of free upload slots a peer must have in order for a response to be processed.  (Default = 0).
        /// </summary>
        public int MinimumPeerFreeUploadSlots { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the maximum queue depth a peer may have in order for a response to be processed.  (Default = 1000000).
        /// </summary>
        public int MaximumPeerQueueLength { get; set; } = 1000000;

        /// <summary>
        ///     Gets or sets the minimum upload speed a peer must have in order for a response to be processed.  (Default = 0).
        /// </summary>
        public int MinimumPeerUploadSpeed { get; set; } = 0;

        /// <summary>
        ///     Gets or sets a list of ignored file extensions.  (Default = empty).
        /// </summary>
        public IEnumerable<string> IgnoredFileExtensions { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether files are to be filtered.  (Default = false).
        /// </summary>
        public bool FilterFiles { get; set; } = false;

        /// <summary>
        ///     Gets or sets the minimum file bitrate.  (Default = 128).
        /// </summary>
        public int MinimumFileBitRate { get; set; } = 128;

        /// <summary>
        ///     Gets or sets the minimum file size.  (Default = 0).
        /// </summary>
        public int MinimumFileSize { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the minimum file length, in seconds.  (Default = 0).
        /// </summary>
        public int MinimumFileLength { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the minimum file sample rate.  (Default = 0).
        /// </summary>
        public int MinimumFileSampleRate { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the minimum file bit depth.  (Default = 0).
        /// </summary>
        public int MinimumFileBitDepth { get; set; } = 0;

        /// <summary>
        ///     Gets or sets a value indicating whether constant bit rate files are to be included.  (Default = true).
        /// </summary>
        public bool IncludeConstantBitRate { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether variable bit rate files are to be included.  (Default = true).
        /// </summary>
        public bool IncludeVariableBitRate { get; set; } = true;
    }
}
