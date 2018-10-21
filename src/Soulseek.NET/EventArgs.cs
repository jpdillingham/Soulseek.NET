
namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Responses;
    using System;

    public class SearchResultReceivedEventArgs : EventArgs
    {
        public PeerSearchReplyResponse Response { get; set; }
    }

    public class ResponseReceivedEventArgs : EventArgs
    {
        public Message Message { get; set; }
        public Type ResponseType { get; set; }
        public object Response { get; set; }
    }

    public class MessageReceivedEventArgs
    {
        public Message Message { get; set; }
    }
}
