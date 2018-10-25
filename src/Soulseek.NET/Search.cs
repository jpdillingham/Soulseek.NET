namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    public enum SearchState
    {
        Pending = 0,
        InProgress = 1,
        Cancelled = 2,
        Completed = 3,
    }

    public sealed class Search
    {
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

        public event EventHandler<SearchCancelledEventArgs> SearchCancelled;
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        public IEnumerable<SearchResponse> Responses => ResponseList.AsReadOnly();
        public string SearchText { get; private set; }
        public SearchState State { get; private set; } = SearchState.Pending;
        public int Ticket { get; private set; }
        private List<Connection> PeerConnections { get; set; } = new List<Connection>();
        private List<SearchResponse> ResponseList { get; set; } = new List<SearchResponse>();
        private int SearchTimeout { get; set; }
        private SystemTimer SearchTimeoutTimer { get; set; }
        private Connection ServerConnection { get; set; }

        public void Cancel()
        {
            State = SearchState.Cancelled;
            Complete();
        }

        public int Start()
        {
            return Task.Run(() => StartAsync()).GetAwaiter().GetResult();
        }

        public async Task<int> StartAsync()
        {
            if (State == SearchState.InProgress)
            {
                throw new SearchException($"The Search is already in progress.");
            }

            State = SearchState.InProgress;

            var request = new SearchRequest(SearchText, Ticket);

            Console.WriteLine($"Searching for {SearchText}...");
            await ServerConnection.SendAsync(request.ToMessage().ToByteArray());

            SearchTimeoutTimer.Reset();
            SearchTimeoutTimer.Elapsed += (sender, e) => Complete();

            return Ticket;
        }

        internal void AddResult(SearchResponseReceivedEventArgs result)
        {
            ResponseList.Add(result.Response);
            Task.Run(() => SearchResponseReceived?.Invoke(this, result)).Forget();

            SearchTimeoutTimer.Reset();
        }

        private void Complete()
        {
            State = SearchState.Completed;

            Task.Run(() => SearchCompleted?.Invoke(this, new SearchCompletedEventArgs() { Search = this })).Forget();
        }
    }
}