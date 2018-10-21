
namespace Soulseek.NET
{
    using System.Collections.Generic;

    public class File
    {
        public int Code { get; set; }
        public string Filename { get; set; }
        public int Size { get; set; }
        public string Extension { get; set; }
        public int AttributeCount { get; set; }
        public IEnumerable<FileAttribute> Attributes { get; set; } = new List<FileAttribute>();
    }

    public class FileAttribute
    {
        public int Type { get; set; }
        public int Value { get; set; }
    }
}
