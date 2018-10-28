using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    public class SearchOptions
    {
        public int MinimumResultFileCount { get; set; }
        public int MinimumFreeUploadSlots { get; set; }
        public int MaximumInQueue { get; set; }
        public int MinimumUploadSpeed { get; set; }
        public string FileExtension { get; set; }
    }
}
