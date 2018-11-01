namespace Soulseek.NET.Messaging.Responses
{
    public sealed class FileAttribute
    {
        public FileAttributeType Type { get; internal set; }
        public int Value { get; internal set; }

        internal FileAttribute()
        {
        }
    }
}
