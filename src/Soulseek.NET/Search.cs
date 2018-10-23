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
        public IEnumerable<SearchResponse> Responses { get; private set; } = new List<SearchResponse>().AsReadOnly();
        public bool Cancelled { get; private set; }
        public bool InProgress { get; private set; }

        public event EventHandler<SearchResultReceivedEventArgs> SearchResultReceived;
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        private Connection ServerConnection { get; set; }
        private List<Connection> PeerConnections { get; set; } = new List<Connection>();

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

        internal Search(Connection serverConnection, string searchText)
        {
            ServerConnection = serverConnection;
            SearchText = searchText;
            Ticket = new Random().Next(1, 2147483647);
        }

        private void OnPeerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected && sender is Connection connection)
            {
                connection.Dispose();
                PeerConnections.Remove(connection);
            }
        }

        private async Task HandlePeerSearchReply(SearchResponse response, NetworkEventArgs e)
        {
            if (response.FileCount > 0)
            {
                await Task.Run(() => SearchResultReceived?.Invoke(this, new SearchResultReceivedEventArgs(e)
                {
                    Result = response,
                }));
            }
        }

        private async void OnPeerConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = new Message(e.Data);
            Console.WriteLine($"Peer Data: {message.Code}");

            switch (message.Code)
            {
                case MessageCode.PeerSearchReply:
                    await HandlePeerSearchReply(SearchResponse.Parse(message), e);
                    break;
                default:
                    break;
            }
        }

        internal async Task ConnectToPeer(ConnectToPeerResponse connectToPeerResponse)
        {
            var connection = new Connection(ConnectionType.Peer, connectToPeerResponse.IPAddress.ToString(), connectToPeerResponse.Port);
            PeerConnections.Add(connection);

            connection.DataReceived += OnPeerConnectionDataReceived;
            connection.StateChanged += OnPeerConnectionStateChanged;

            try
            {
                await connection.ConnectAsync();

                var request = new PierceFirewallRequest(connectToPeerResponse.Token);
                await connection.SendAsync(request.ToByteArray(), suppressCodeNormalization: true);
            }
            catch (ConnectionException)
            {
                //Console.WriteLine($"Failed to connect to Peer {response.Username}@{response.IPAddress}: {ex.Message}");
            }
        }
    }
}
