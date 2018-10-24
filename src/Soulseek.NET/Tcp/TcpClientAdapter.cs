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

        private TcpClient TcpClient { get; set; }
        private bool Disposed { get; set; }

        public void Close()
        {
            TcpClient.Close();
        }

        public async Task ConnectAsync(IPAddress ipAddress, int port)
        {
            await TcpClient.ConnectAsync(ipAddress, port);
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

        public void Dispose()
        {
            Dispose(true);
        }
    }
}