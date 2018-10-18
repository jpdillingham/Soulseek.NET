namespace Soulseek.NET.Tcp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public class TcpClientAdapter : ITcpClient, IDisposable
    {
        public TcpClientAdapter(TcpClient tcpClient = null)
        {
            TcpClient = tcpClient ?? new TcpClient();
        }

        public bool Connected => TcpClient.Connected;

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
            GC.SuppressFinalize(this);
        }

        public NetworkStream GetStream()
        {
            return TcpClient.GetStream();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                TcpClient.Dispose();
            }
        }
    }
}