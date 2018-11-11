// <copyright file="SoulseekClient.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.NET
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : IDisposable, ISoulseekClient
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class with the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The client options.</param>
        public SoulseekClient(SoulseekClientOptions options = null)
            : this("vps.slsknet.org", 2271, options)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class with the specified <paramref name="address"/>
        ///     and <paramref name="port"/>.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client <see cref="SoulseekClientOptions"/>.</param>
        public SoulseekClient(string address, int port, SoulseekClientOptions options)
        {
            Address = address;
            Port = port;
            Options = options ?? new SoulseekClientOptions();

            Connection = new Connection(ConnectionType.Server, Address, Port, Options);
            Connection.StateChanged += OnServerConnectionStateChanged;
            Connection.DataReceived += OnServerConnectionDataReceived;

            MessageWaiter = new MessageWaiter(Options.MessageTimeout);
        }

        /// <summary>
        ///     Occurs when the underlying TCP connection to the server changes state.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        ///     Occurs when raw data is received by the underlying TCP connection.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        ///     Occurs when a new message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Occurs when a new search result is received.
        /// </summary>
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public ConnectionState State => Connection.State;

        /// <summary>
        ///     Gets a value indicating whether a user is currently signed in.
        /// </summary>
        public bool LoggedIn { get; private set; } = false;

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        public SoulseekClientOptions Options { get; private set; }

        /// <summary>
        ///     Gets or sets the port to which to connect.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        public string Username { get; private set; }

        private Search ActiveSearch { get; set; }
        private Download ActiveDownload { get; set; } // todo: use a ConcurrentDictionary<string username, ConcurrentQueue> for this
        private Connection Connection { get; set; }
        private bool Disposed { get; set; } = false;
        private MessageWaiter MessageWaiter { get; set; }
        private Random Random { get; set; } = new Random();

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <param name="options">The operation <see cref="BrowseOptions"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The operation context, including the fetched list of files.</returns>
        public async Task<Browse> BrowseAsync(string username, BrowseOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (State != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!LoggedIn)
            {
                throw new BrowseException($"A user must be logged in to browse.");
            }

            options = options ?? new BrowseOptions();

            var address = await GetPeerAddressAsync(username);
            var browse = new Browse(username, address.IPAddress, address.Port, options);

            return await browse.BrowseAsync(cancellationToken);
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ConnectionStateException">Thrown when the client is already connected, or is transitioning between states.</exception>
        public async Task ConnectAsync()
        {
            if (Connection.State == ConnectionState.Connected)
            {
                throw new ConnectionStateException($"Failed to connect; the client is already connected.");
            }

            if (Connection.State == ConnectionState.Connecting || Connection.State == ConnectionState.Disconnecting)
            {
                throw new ConnectionStateException($"Failed to connect; the client is transitioning between states.");
            }

            await Connection.ConnectAsync();
        }

        /// <summary>
        ///     Disconnects the client from the server.
        /// </summary>
        public void Disconnect()
        {
            Connection.Disconnect("Client disconnected.");

            ActiveSearch?.Dispose();

            ActiveSearch = default(Search);
            Username = null;
            LoggedIn = false;
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        public async Task<Download> DownloadAsync(string username, string filename, DownloadOptions options = null, CancellationToken? cancellationToken = null)
        {
            var address = await GetPeerAddressAsync(username);

            Console.WriteLine($"[DOWNLOAD]: {username} {address.IPAddress}:{address.Port}");

            ActiveDownload = new Download(username, filename, address.IPAddress, address.Port, options);

            await ActiveDownload.DownloadAsync(cancellationToken);

            return ActiveDownload;
        }

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="LoginException">Thrown when the login fails.</exception>
        public async Task LoginAsync(string username, string password)
        {
            if (LoggedIn)
            {
                throw new LoginException($"Already logged in as {Username}.  Disconnect before logging in again.");
            }

            var login = MessageWaiter.Wait<LoginResponse>(MessageCode.ServerLogin);

            await Connection.SendAsync(new LoginRequest(username, password).ToMessage().ToByteArray());

            await login;

            if (login.Result.Succeeded)
            {
                Username = username;
                LoggedIn = true;
            }
            else
            {
                // upon login failure the server will refuse to allow any more input, eventually disconnecting.
                Disconnect();
                throw new LoginException($"Failed to log in as {username}: {login.Result.Message}");
            }
        }

        /// <summary>
        ///     Asynchronously performs a search for the specified <paramref name="searchText"/> using the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="options">The options for the search.</param>
        /// <param name="cancellationToken">The optional cancellation token for the task.</param>
        /// <returns>The completed search.</returns>
        public async Task<Search> SearchAsync(string searchText, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (State != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"The server connection must be Connected to perform a search (currently: {State})");
            }

            if (!LoggedIn)
            {
                throw new SearchException($"A user must be logged in to perform a search.");
            }

            if (ActiveSearch != default(Search))
            {
                throw new SearchException($"A search is already in progress.");
            }

            options = options ?? new SearchOptions();

            ActiveSearch = new Search(searchText, options, Connection);
            ActiveSearch.SearchResponseReceived += OnSearchResponseReceived;

            return await ActiveSearch.SearchAsync(cancellationToken);
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        /// <param name="disposing">A value indicating whether disposal is in progress.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    var message = "Client is being disposed.";

                    Connection?.Disconnect(message);
                    Connection?.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<GetPeerAddressResponse> GetPeerAddressAsync(string username)
        {
            var request = new GetPeerAddressRequest(username);
            await Connection.SendAsync(request.ToMessage().ToByteArray());

            return await MessageWaiter.Wait<GetPeerAddressResponse>(MessageCode.ServerGetPeerAddress, username);
        }

        private async Task HandlePrivateMessage(PrivateMessage message, NetworkEventArgs e)
        {
            Console.WriteLine($"[{message.Timestamp}][{message.Username}]: {message.Message}");
            await Connection.SendAsync(new AcknowledgePrivateMessageRequest(message.Id).ToByteArray());
        }

        private async Task HandleServerConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
            if (response.Type == "F")
            {
                await ActiveDownload.ConnectToPeer(response, e);
            }
            else
            {
                if (ActiveSearch != default(Search))
                {
                    await ActiveSearch.AddPeerConnection(response, e);
                }
            }
        }

        private void OnSearchResponseReceived(object sender, SearchResponseReceivedEventArgs e)
        {
            Task.Run(() => SearchResponseReceived?.Invoke(this, e));
        }

        private async void OnServerConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();

            var message = new Message(e.Data);
            var messageEventArgs = new MessageReceivedEventArgs(e) { Message = message };

            Task.Run(() => MessageReceived?.Invoke(this, messageEventArgs)).Forget();

            Console.WriteLine($"[MESSAGE]: {message.Code}");

            switch (message.Code)
            {
                case MessageCode.ServerParentMinSpeed:
                case MessageCode.ServerParentSpeedRatio:
                case MessageCode.ServerWishlistInterval:
                    MessageWaiter.Complete(message.Code, Integer.Parse(message));
                    break;

                case MessageCode.ServerLogin:
                    MessageWaiter.Complete(message.Code, LoginResponse.Parse(message));
                    break;

                case MessageCode.ServerRoomList:
                    MessageWaiter.Complete(message.Code, RoomList.Parse(message));
                    break;

                case MessageCode.ServerPrivilegedUsers:
                    MessageWaiter.Complete(message.Code, PrivilegedUserList.Parse(message));
                    break;

                case MessageCode.ServerConnectToPeer:
                    await HandleServerConnectToPeer(ConnectToPeerResponse.Parse(message), e);
                    break;

                case MessageCode.ServerPrivateMessages:
                    await HandlePrivateMessage(PrivateMessage.Parse(message), e);
                    break;

                case MessageCode.ServerGetPeerAddress:
                    var response = GetPeerAddressResponse.Parse(message);
                    MessageWaiter.Complete(message.Code, response.Username, response);
                    break;

                default:
                    Console.WriteLine($"Unknown message: [{e.IPAddress}] {message.Code}: {message.Payload.Length} bytes");
                    break;
            }
        }

        private async void OnServerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected)
            {
                Disconnect();
            }

            await Task.Run(() => ConnectionStateChanged?.Invoke(this, e));
        }
    }
}