namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public sealed class Search
    {
        public int Ticket { get; private set; }
        public string SearchText { get; private set; }
        public IEnumerable<SearchResponse> Responses { get; private set; } = new List<SearchResponse>();
        public bool Cancelled { get; private set; }

        private Connection Connection { get; set; }

        public int Start()
        {
            var request = new SearchRequest(SearchText, Ticket);

            Console.WriteLine($"Searching for {SearchText}...");
            Task.Run(() => Connection.SendAsync(request.ToMessage().ToByteArray())).GetAwaiter().GetResult();

            return Ticket;
        }

        public void Cancel()
        {
            Cancelled = true;
        }

        internal Search(Connection connection, string searchText)
        {
            Connection = connection;
            SearchText = searchText;
            Ticket = new Random().Next(1, 2147483647);
        }
    }
}
