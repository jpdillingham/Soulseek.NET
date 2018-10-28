namespace Soulseek.NET
{
    using System.Collections.Generic;

    public class SearchOptions
    {
        public int Timeout { get; set; } = 15;
        public int ResultLimit { get; set; } = 5000;

        public int MinimumFileCount { get; set; } = 1;
        public int MinimumPeerFreeUploadSlots { get; set; }
        public int MaximumPeerInQueue { get; set; } = 9999;
        public int MinimumPeerUploadSpeed { get; set; }

        public IEnumerable<string> IgnoredFileExtensions { get; set; }

        public int MinimumBitRate { get; set; } = 128;
        public int MinimumSize { get; set; }
        public int MinimumLength { get; set; }
        public int MinimumSampleRate { get; set; }
        public int MinimumBitDepth { get; set; }
    }
}
