namespace Soulseek.NET
{
    public class SoulseekClientOptions
    {
        public int ConcurrentPeerConnections { get; set; } = 500;
        public int ConnectionTimeout { get; set; } = 5;
        public int ReadTimeout { get; set; } = 5;
        public int BufferSize { get; set; } = 4096;
    }
}
