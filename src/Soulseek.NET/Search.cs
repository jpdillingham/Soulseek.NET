namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
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
        public IEnumerable<SearchResponse> Responses => ResponseList.AsReadOnly();
        public bool Cancelled { get; private set; }
        public bool InProgress { get; private set; }

        public event EventHandler<SearchResultReceivedEventArgs> SearchResultReceived;
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        private Connection ServerConnection { get; set; }
        private List<Connection> PeerConnections { get; set; } = new List<Connection>();
        private List<SearchResponse> ResponseList { get; set; } = new List<SearchResponse>();

        public int Start()
        {
            if (InProgress)
            {
                throw new SearchException($"The Search is already in progress.");
            }

            var request = new SearchRequest(SearchText, Ticket);

            Console.WriteLine($"Searching for {SearchText}...");
            Task.Run(() => ServerConnection.SendAsync(request.ToMessage().ToByteArray())).GetAwaiter().GetResult();

            return Ticket;
        }

        public void Cancel()
        {
            // todo: kill peer connections, ignore ConnectoToPeer messages
            Cancelled = true;
        }

        internal void AddResult(SearchResultReceivedEventArgs result)
        {
            ResponseList.Add(result.Result);
            Task.Run(() => SearchResultReceived?.Invoke(this, result)).Forget();
        }

        internal Search(Connection serverConnection, string searchText)
        {
            ServerConnection = serverConnection;
            SearchText = searchText;
            Ticket = new Random().Next(1, 2147483647);
        }
    }
}
