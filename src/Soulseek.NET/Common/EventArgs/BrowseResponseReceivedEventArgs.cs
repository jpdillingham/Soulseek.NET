namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Responses;

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
}
