using Soulseek.NET.Messaging.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    public class ServerInfo
    {
        public int ParentMinSpeed { get; internal set; }
        public int ParentSpeedRatio { get; internal set; }
        public IEnumerable<string> PrivilegedUsers { get; internal set; }
        public IEnumerable<Room> Rooms { get; internal set; }
        public int WishlistInterval { get; internal set; }
    }
}
