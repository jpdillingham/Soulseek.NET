namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;

    public class SearchResultReceivedEventArgs : NetworkEventArgs
    {
        public PeerSearchReplyResponse Result { get; set; }

        public SearchResultReceivedEventArgs(NetworkEventArgs e)
        {
            Address = e.Address;
            IPAddress = e.IPAddress;
            Port = e.Port;
        }
    }

    public class MessageReceivedEventArgs : NetworkEventArgs
    {
        public Message Message { get; set; }

        public MessageReceivedEventArgs(NetworkEventArgs e)
        {
            Address = e.Address;
            IPAddress = e.IPAddress;
            Port = e.Port;
        }
    }
}
