namespace Soulseek.NET.Tcp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    internal sealed class TcpClientAdapter : ITcpClient, IDisposable
    {
        internal TcpClientAdapter(TcpClient tcpClient = null)
        {
            TcpClient = tcpClient ?? new TcpClient();
        }

        public bool Connected => TcpClient.Connected;

        private bool Disposed { get; set; }
        private TcpClient TcpClient { get; set; }

        public void Close()
        {
            TcpClient.Close();
        }

        public async Task ConnectAsync(IPAddress ipAddress, int port)
        {
            await TcpClient.ConnectAsync(ipAddress, port);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public NetworkStream GetStream()
        {
            return TcpClient.GetStream();
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    TcpClient.Dispose();
                }

                Disposed = true;
            }
        }
    }
}