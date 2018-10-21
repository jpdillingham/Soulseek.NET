
namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Responses;
    using System;

    public class SearchResultReceivedEventArgs : EventArgs
    {
        public PeerSearchReplyResponse Response { get; set; }
    }
}
