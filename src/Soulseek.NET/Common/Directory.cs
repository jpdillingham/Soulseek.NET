namespace Soulseek.NET
{
    using System.Collections.Generic;

    public sealed class Directory
    {
        public string Directoryname { get; internal set; }
        public int FileCount { get; internal set; }
        public IEnumerable<File> Files => FileList.AsReadOnly();

        internal List<File> FileList { get; set; } = new List<File>();

        internal Directory()
        {
        }
    }
}
