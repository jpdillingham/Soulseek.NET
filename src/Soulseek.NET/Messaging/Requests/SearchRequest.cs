namespace Soulseek.NET.Messaging.Requests
{ 
    public class SearchRequest
    {
        public SearchRequest(string searchText, int token)
        {
            SearchText = searchText;
            Token = token;
        }

        public string SearchText { get; set; }
        public int Token { get; set; }

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerFileSearch)
                .WriteInteger(Token)
                .WriteString(SearchText)
                .Build();
        }
    }
}
