namespace Soulseek.NET.Tcp
{
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public interface ITcpClient
    {
        bool Connected { get; }

        void Close();
        Task ConnectAsync(IPAddress ipAddress, int port);
        void Dispose();
        NetworkStream GetStream();
    }
}