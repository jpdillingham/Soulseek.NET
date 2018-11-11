namespace Soulseek.NET
{
    using Soulseek.NET.Tcp;

    public sealed class Download
    {
        internal Download(string username, string filename, string ipAddress, int port, DownloadOptions options, IConnection peerConnection = null, IConnection transferConnection = null)
        {
            Username = username;
            Filename = filename;
            IPAddress = ipAddress;
            Port = port;
            Options = options ?? new DownloadOptions();
            PeerConnection = peerConnection ?? new Connection(ConnectionType.Peer, ipAddress, port, Options);
            TransferConnection = transferConnection ?? new Connection(ConnectionType.Transfer, ipAddress, port, Options);
        }

        public string Username { get; private set; }
        public string IPAddress { get; private set; }
        public int Port { get; private set; }
        public string Filename { get; private set; }
        public DownloadOptions Options { get; private set; }
        private IConnection PeerConnection { get; set; }
        private IConnection TransferConnection { get; set; }
    }
}
