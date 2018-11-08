// <copyright file="SearchFilters.cs" company="JP Dillingham">
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
    using System.Linq;
    using Soulseek.NET.Messaging.Responses;

    internal sealed class SearchFilters
    {
        internal SearchFilters(SearchOptions searchOptions)
        {
            SearchOptions = searchOptions;
        }

        internal SearchOptions SearchOptions { get; private set; }

        internal bool FileMeetsOptionCriteria(File file)
        {
            if (!SearchOptions.FilterFiles)
            {
                return true;
            }

            bool fileHasIgnoredExtension(File f)
            {
                return SearchOptions.IgnoredFileExtensions == null ? false :
                    SearchOptions.IgnoredFileExtensions.Any(e => e == System.IO.Path.GetExtension(f.Filename));
            }

            if (file.Size < SearchOptions.MinimumFileSize || fileHasIgnoredExtension(file))
            {
                return false;
            }

            var bitRate = file.GetAttributeValue(FileAttributeType.BitRate);
            var length = file.GetAttributeValue(FileAttributeType.Length);
            var bitDepth = file.GetAttributeValue(FileAttributeType.BitDepth);
            var sampleRate = file.GetAttributeValue(FileAttributeType.SampleRate);

            if ((bitRate != null && bitRate < SearchOptions.MinimumFileBitRate) ||
                (length != null && length < SearchOptions.MinimumFileLength) ||
                (bitDepth != null && bitDepth < SearchOptions.MinimumFileBitDepth) ||
                (sampleRate != null && sampleRate < SearchOptions.MinimumFileSampleRate))
            {
                return false;
            }

            var constantBitRates = new[] { 32, 64, 128, 192, 256, 320 };
            var isConstant = constantBitRates.Any(b => b == bitRate);

            if (bitRate != null && ((!SearchOptions.IncludeConstantBitRate && isConstant) || (!SearchOptions.IncludeVariableBitRate && !isConstant)))
            {
                return false;
            }

            return true;
        }

        internal bool ResponseMeetsOptionCriteria(SearchResponse response)
        {
            if (SearchOptions.FilterResponses && (
                    response.FileCount < SearchOptions.MinimumResponseFileCount ||
                    response.FreeUploadSlots < SearchOptions.MinimumPeerFreeUploadSlots ||
                    response.UploadSpeed < SearchOptions.MinimumPeerUploadSpeed ||
                    response.QueueLength > SearchOptions.MaximumPeerQueueLength))
            {
                return false;
            }

            return true;
        }
    }
}
