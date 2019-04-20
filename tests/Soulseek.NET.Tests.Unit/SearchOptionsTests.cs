// <copyright file="SearchOptionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit
{
    using System.Collections.Generic;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Defaults(
            int searchTimeout,
            int responseLimit,
            int fileLimit,
            bool filterResponses,
            int minimumResponseFileCount,
            int minimumPeerFreeUploadSlots,
            int maximumPeerQueueLength,
            int minimumPeerUploadSpeed,
            IEnumerable<string> ignoredFileExtensions,
            bool filterFiles,
            int minimumFileBitRate,
            int minimumFileSize,
            int minimumFileLength,
            int minimumFileSampleRate,
            int minimumFileBitDepth,
            bool includeConstantBitRate,
            bool includeVariableBitRate)
        {
            var o = new SearchOptions(
                searchTimeout,
                responseLimit,
                fileLimit,
                filterResponses,
                minimumResponseFileCount,
                minimumPeerFreeUploadSlots,
                maximumPeerQueueLength,
                minimumPeerUploadSpeed,
                ignoredFileExtensions,
                filterFiles,
                minimumFileBitRate,
                minimumFileSize,
                minimumFileLength,
                minimumFileSampleRate,
                minimumFileBitDepth,
                includeConstantBitRate,
                includeVariableBitRate);

            Assert.Equal(searchTimeout, o.SearchTimeout);
            Assert.Equal(responseLimit, o.ResponseLimit);
            Assert.Equal(fileLimit, o.FileLimit);
            Assert.Equal(filterResponses, o.FilterResponses);
            Assert.Equal(minimumResponseFileCount, o.MinimumResponseFileCount);
            Assert.Equal(minimumPeerFreeUploadSlots, o.MinimumPeerFreeUploadSlots);
            Assert.Equal(maximumPeerQueueLength, o.MaximumPeerQueueLength);
            Assert.Equal(minimumPeerUploadSpeed, o.MinimumPeerUploadSpeed);
            Assert.Equal(ignoredFileExtensions, o.IgnoredFileExtensions);
            Assert.Equal(filterFiles, o.FilterFiles);
            Assert.Equal(minimumFileBitRate, o.MinimumFileBitRate);
            Assert.Equal(minimumFileSize, o.MinimumFileSize);
            Assert.Equal(minimumFileLength, o.MinimumFileLength);
            Assert.Equal(minimumFileSampleRate, o.MinimumFileSampleRate);
            Assert.Equal(minimumFileBitDepth, o.MinimumFileBitDepth);
            Assert.Equal(includeConstantBitRate, o.IncludeConstantBitRate);
            Assert.Equal(includeVariableBitRate, o.IncludeVariableBitRate);
        }
    }
}
