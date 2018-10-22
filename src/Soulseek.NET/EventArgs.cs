
namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;

    public class MessageReceivedEventArgs
    {
        public string Address;
        public string IPAddress;
        public int Port;
        public Message Message { get; set; }
    }
}
