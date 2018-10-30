namespace Soulseek.NET
{
    using System.Collections.Generic;

    public class SearchOptions
    {
        public int Timeout { get; set; } = 15;
        public int ResultLimit { get; set; } = 5000;

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
