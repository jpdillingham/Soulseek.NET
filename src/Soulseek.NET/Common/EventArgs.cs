namespace Soulseek.NET.Common
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

    public class BrowseResponseReceivedEventArgs : NetworkEventArgs
    {
        public SharesResponse Response { get; set; }

        public BrowseResponseReceivedEventArgs(NetworkEventArgs e)
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

    public class DataReceivedEventArgs : NetworkEventArgs
    {
        public byte[] Data;
    }

    public class ConnectionStateChangedEventArgs : NetworkEventArgs
    {
        public ConnectionState State { get; set; }
        public string Message { get; set; }
    }

    public class NetworkEventArgs : EventArgs
    {
        public string Address;
        public string IPAddress;
        public int Port;
    }
}
