namespace Soulseek.NET.Messaging.Requests
{ 
    public class PeerSearchRequest
    {
        public PeerSearchRequest(int token, string searchText)
        {
            Token = token;
            SearchText = searchText;
        }

        public int Token { get; set; }
        public string SearchText { get; set; }

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.PeerSearchRequest)
                .WriteInteger(Token)
                .WriteString(SearchText)
                .Build();
        }
    }
}
