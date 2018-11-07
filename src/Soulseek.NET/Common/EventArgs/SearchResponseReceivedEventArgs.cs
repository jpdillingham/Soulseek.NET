namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Responses;

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
}
