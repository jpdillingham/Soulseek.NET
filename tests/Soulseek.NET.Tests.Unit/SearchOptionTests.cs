namespace Soulseek.NET.Tests.Unit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchOptionTests
    {
        [Theory(DisplayName = "Instantiates with defaults"), AutoData]
        public async Task Instantiates_With_Defaults(
            int searchTimeout,
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
            Assert.Equal(fileLimit, o.FileLimit);
        }
    }
}
