namespace Soulseek.NET
{
    using System.Collections.Generic;
    using System.Linq;

    public sealed class File
    {
        public int Code { get; internal set; }
        public string Filename { get; internal set; }
        public long Size { get; internal set; }
        public string Extension { get; internal set; }
        public int AttributeCount { get; internal set; }
        public IEnumerable<FileAttribute> Attributes => AttributeList.AsReadOnly();

        internal List<FileAttribute> AttributeList { get; set; } = new List<FileAttribute>();

        internal File()
        {
        }

        public int? GetAttributeValue(FileAttributeType type)
        {
            return AttributeList.Where(a => a.Type == type).SingleOrDefault()?.Value;
        }
    }
}