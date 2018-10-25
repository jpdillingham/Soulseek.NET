namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;

    public class SearchResponseReceivedEventArgs : NetworkEventArgs
    {
        public SearchResponse Response { get; set; }

        public SearchResponseReceivedEventArgs(NetworkEventArgs e)
        {
            Address = e.Address;
            IPAddress = e.IPAddress;
            Port = e.Port;
        }
    }

    public class SearchCompletedEventArgs : EventArgs
    {
        public Search Search { get; set; }
    }

    public class SearchCancelledEventArgs : SearchCompletedEventArgs
    {
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
