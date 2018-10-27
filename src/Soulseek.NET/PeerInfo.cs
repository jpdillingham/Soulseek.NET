namespace Soulseek.NET
{
    public class PeerInfo
    {
        public int Queued { get; internal set; }
        public int Active { get; internal set; }
        public int Connected { get; internal set; }
        public int Connecting { get; internal set; }
        public int Disconnecting { get; internal set; }
        public int Disconnected { get; internal set; }
    }
}
