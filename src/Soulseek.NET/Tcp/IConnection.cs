
namespace Soulseek.NET.Tcp
{
    using System;
    using System.Threading.Tasks;

    interface IConnection
    {
        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        Task ConnectAsync();
        void Disconnect(string message = null);
        Task SendAsync(byte[] bytes);
    }
}
