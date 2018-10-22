namespace Soulseek.NET.Messaging.Requests
{ 
    public class SearchRequest
    {
        public SearchRequest(string searchText, int ticket)
        {
            SearchText = searchText;
            Ticket = ticket;
        }

        public string SearchText { get; set; }
        public int Ticket { get; set; }

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerFileSearch)
                .WriteInteger(Ticket)
                .WriteString(SearchText)
                .Build();
        }
    }
}
