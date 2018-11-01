namespace Soulseek.NET.Messaging.Responses
{

    public sealed class Room
    {
        public string Name { get; internal set; }
        public int UserCount { get; internal set; }

        internal Room()
        {
        }
    }
}
