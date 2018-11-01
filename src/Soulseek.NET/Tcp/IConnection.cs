
namespace Soulseek.NET.Tcp
{
    using Soulseek.NET.Common;
    using System;
    using System.Threading.Tasks;

    internal interface IConnection
    {
        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        Task ConnectAsync();
        void Disconnect(string message = null);
        Task SendAsync(byte[] bytes, bool suppressCodeNormalization = false);
    }
}
