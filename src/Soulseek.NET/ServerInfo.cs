namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Responses;
    using System.Collections.Generic;

    public class ServerInfo
    {
        internal ServerInfo()
        {
        }

        public int ParentMinSpeed { get; internal set; }
        public int ParentSpeedRatio { get; internal set; }
        public IEnumerable<string> PrivilegedUsers { get; internal set; }
        public IEnumerable<Room> Rooms { get; internal set; }
        public int WishlistInterval { get; internal set; }
    }
}
