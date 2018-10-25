namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    public sealed class Search
    {
        public int Ticket { get; private set; }
        public string SearchText { get; private set; }
        public IEnumerable<SearchResponse> Results => ResponseList.AsReadOnly();
        public bool Cancelled { get; private set; }
        public bool InProgress { get; private set; }

        public event EventHandler<SearchResultReceivedEventArgs> SearchResultReceived;
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        private int SearchTimeout { get; set; }
        private SystemTimer SearchTimeoutTimer { get; set; }
        private Connection ServerConnection { get; set; }
        private List<Connection> PeerConnections { get; set; } = new List<Connection>();
        private List<SearchResponse> ResponseList { get; set; } = new List<SearchResponse>();

        public async Task<int> StartAsync()
        {
            if (InProgress)
            {
                throw new SearchException($"The Search is already in progress.");
            }

            var request = new SearchRequest(SearchText, Ticket);

            Console.WriteLine($"Searching for {SearchText}...");
            await ServerConnection.SendAsync(request.ToMessage().ToByteArray());

            SearchTimeoutTimer.Reset();
            SearchTimeoutTimer.Elapsed += (sender, e) => Complete();

            return Ticket;
        }

        public void Cancel()
        {
            // todo: kill peer connections, ignore ConnectoToPeer messages
            Cancelled = true;
            Complete();
        }

        internal void AddResult(SearchResultReceivedEventArgs result)
        {
            ResponseList.Add(result.Result);
            Task.Run(() => SearchResultReceived?.Invoke(this, result)).Forget();

            SearchTimeoutTimer.Reset();
        }

        private void Complete()
        {
            InProgress = false;

            Task.Run(() => SearchCompleted?.Invoke(this, new SearchCompletedEventArgs()
            {
                Result = new SearchResult()
                {
                    SearchText = SearchText,
                    Ticket = Ticket,
                    Results = Results,
                    Cancelled = Cancelled,
                }
            })).Forget();
        }

        internal Search(Connection serverConnection, string searchText, int searchTimeout = 15)
        {
            ServerConnection = serverConnection;
            SearchText = searchText;
            Ticket = new Random().Next(1, 2147483647);

            SearchTimeout = searchTimeout;
            SearchTimeoutTimer = new SystemTimer()
            {
                Interval = SearchTimeout * 1000,
                Enabled = false,
                AutoReset = false,
            };
        }
    }
}
