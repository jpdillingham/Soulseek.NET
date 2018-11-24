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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : IDisposable, ISoulseekClient
    {
        #region Public Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class with the specified <paramref name="address"/>
        ///     and <paramref name="port"/>.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client <see cref="SoulseekClientOptions"/>.</param>
        public SoulseekClient(string address = "vps.slsknet.org", int port = 2271, SoulseekClientOptions options = null)
            : this(address, port, options, null, null, null)
        {
        }

        #endregion Public Constructors

        #region Internal Constructors

        internal SoulseekClient(
            string address,
            int port,
            SoulseekClientOptions options,
            IMessageConnection serverConnection = null,
            IConnectionManager<IMessageConnection> peerConnectionManager = null,
            IMessageWaiter messageWaiter = null)
        {
            Address = address;
            Port = port;
            Options = options ?? new SoulseekClientOptions() { ConnectionOptions = new ConnectionOptions() { ReadTimeout = 0 } };
            ServerConnection = serverConnection ?? GetServerMessageConnection(Address, Port, Options.ConnectionOptions);
            PeerConnectionManager = peerConnectionManager ?? new ConnectionManager<IMessageConnection>(Options.ConcurrentPeerConnections);
            MessageWaiter = messageWaiter ?? new MessageWaiter(Options.MessageTimeout);
        }

        #endregion Internal Constructors

        #region Public Events

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

        #endregion Public Events

        #region Public Properties

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        public string Address { get; set; }

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
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public ConnectionState State => ServerConnection.State;

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        public string Username { get; private set; }

        #endregion Public Properties

        #region Private Properties

        private ConcurrentDictionary<int, Search> ActiveSearches { get; set; } = new ConcurrentDictionary<int, Search>();
        private bool Disposed { get; set; } = false;
        private IMessageWaiter MessageWaiter { get; set; }
        private IConnectionManager<IMessageConnection> PeerConnectionManager { get; set; }
        private ConcurrentDictionary<int, Download> QueuedDownloads { get; set; } = new ConcurrentDictionary<int, Download>();
        private ConcurrentDictionary<int, Download> ActiveDownloads { get; set; } = new ConcurrentDictionary<int, Download>();
        private Random Random { get; set; } = new Random();
        private IMessageConnection ServerConnection { get; set; }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The operation response.</returns>
        public async Task<BrowseResponse> BrowseAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (State != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!LoggedIn)
            {
                throw new LoginException($"A user must be logged in to browse.");
            }

            return await BrowseAsync(username, cancellationToken, null);
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ConnectionStateException">
        ///     Thrown when the client is already connected, or is transitioning between states.
        /// </exception>
        public async Task ConnectAsync()
        {
            if (ServerConnection.State == ConnectionState.Connected)
            {
                throw new ConnectionStateException($"Failed to connect; the client is already connected.");
            }

            if (ServerConnection.State == ConnectionState.Connecting || ServerConnection.State == ConnectionState.Disconnecting)
            {
                throw new ConnectionStateException($"Failed to connect; the client is transitioning between states.");
            }

            Console.WriteLine($"Connecting...");
            await ServerConnection.ConnectAsync();
            Console.WriteLine($"Connected.");
        }

        /// <summary>
        ///     Disconnects the client from the server.
        /// </summary>
        public void Disconnect()
        {
            ServerConnection.Disconnect("Client disconnected.");

            var searches = ActiveSearches;
            ActiveSearches = new ConcurrentDictionary<int, Search>();

            while (!searches.IsEmpty)
            {
                if (searches.TryRemove(searches.Keys.First(), out var search))
                {
                    search.Dispose();
                }
            }

            MessageWaiter.CancelAll();

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

        public async Task<byte[]> DownloadAsync(string username, string filename, int token, CancellationToken? cancellationToken = null)
        {
            return await DownloadAsync(username, filename, token, cancellationToken, null);
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

            var loginWait = MessageWaiter.Wait<LoginResponse>(MessageCode.ServerLogin);

            Console.WriteLine($"Sending login message");
            await ServerConnection.SendMessageAsync(new LoginRequest(username, password).ToMessage());
            Console.WriteLine($"Login message sent");

            var response = await loginWait;

            if (response.Succeeded)
            {
                Username = username;
                LoggedIn = true;
            }
            else
            {
                // upon login failure the server will refuse to allow any more input, eventually disconnecting.
                Disconnect();
                throw new LoginException($"Failed to log in as {username}: {response.Message}");
            }
        }

        /// <summary>
        ///     Asynchronously performs a search for the specified <paramref name="searchText"/> using the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="options">The options for the search.</param>
        /// <param name="cancellationToken">The optional cancellation token for the task.</param>
        /// <returns>The completed search.</returns>
        public async Task<IEnumerable<SearchResponse>> SearchAsync(string searchText, int token, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (State != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"The server connection must be Connected to perform a search (currently: {State})");
            }

            if (!LoggedIn)
            {
                throw new SearchException($"A user must be logged in to perform a search.");
            }

            options = options ?? new SearchOptions();

            var searchWait = MessageWaiter.WaitIndefinitely<Search>(MessageCode.ServerFileSearch, token.ToString(), cancellationToken);
            using (var search = new Search(searchText, token, options)
            {
                ResponseHandler = (s, response) =>
                {
                    var e = new SearchResponseReceivedEventArgs() { SearchText = s.SearchText, Token = s.Token, Response = response };
                    Task.Run(() => SearchResponseReceived?.Invoke(this, e)).Forget();
                },
                CompleteHandler = (s, message) => MessageWaiter.Complete(MessageCode.ServerFileSearch, token.ToString(), s),
            })
            {
                ActiveSearches.TryAdd(search.Token, search);
                await ServerConnection.SendMessageAsync(new SearchRequest(search.SearchText, search.Token).ToMessage());

                var result = await searchWait;

                ActiveSearches.TryRemove(search.Token, out var _);

                return result.Responses;
            }
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/> and <paramref name="connection"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <param name="connection">The peer connection over which to send the browse request.</param>
        /// <returns>The operation response.</returns>
        internal async Task<BrowseResponse> BrowseAsync(string username, CancellationToken? cancellationToken = null, IMessageConnection connection = null)
        {
            try
            {
                var browseWait = MessageWaiter.WaitIndefinitely<BrowseResponse>(MessageCode.PeerBrowseResponse, username, cancellationToken);

                connection = connection ?? await GetUnsolicitedPeerConnectionAsync(username, Options.PeerConnectionOptions);
                connection.DisconnectHandler += new Action<IConnection, string>((conn, message) =>
                {
                    MessageWaiter.Throw(MessageCode.PeerBrowseResponse, conn.Key.Username, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                });

                await connection.SendMessageAsync(new PeerBrowseRequest().ToMessage());

                var response = await browseWait;
                return response;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {Username}: {ex.Message}", ex);
            }
        }

        internal async Task<byte[]> DownloadAsync(string username, string filename, int token, CancellationToken? cancellationToken = null, IMessageConnection connection = null)
        {
            try
            {
                var download = new Download(username, filename, token);
                QueuedDownloads.TryAdd(download.Token, download);

                var downloadWait = MessageWaiter.WaitIndefinitely<byte[]>(MessageCode.PeerDownloadResponse, download.WaitKey, cancellationToken);

                connection = connection ?? await GetUnsolicitedPeerConnectionAsync(username, Options.PeerConnectionOptions);
                connection.DisconnectHandler += new Action<IConnection, string>((conn, message) =>
                {
                    MessageWaiter.Throw(MessageCode.PeerDownloadResponse, download.WaitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                });

                var incomingResponseWait = MessageWaiter.WaitIndefinitely<PeerTransferResponseIncoming>(MessageCode.PeerTransferResponse, Key(download.Username, download.Token), cancellationToken);
                var incomingRequestWait = MessageWaiter.WaitIndefinitely<PeerTransferRequestIncoming>(MessageCode.PeerTransferRequest, Key(download.Username, download.Filename), cancellationToken);

                await connection.SendMessageAsync(new PeerTransferRequestOutgoing(TransferDirection.Download, token, filename).ToMessage());

                var incomingResponse = await incomingResponseWait;
                Console.WriteLine($"Response");

                if (incomingResponse.Allowed)
                {
                    Console.WriteLine($"TODO: download now");
                }
                else
                {
                    var incomingRequest = await incomingRequestWait;

                    download.Size = incomingRequest.Size;
                    download.RemoteToken = incomingRequest.Token;

                    QueuedDownloads.TryRemove(download.Token, out var _);
                    ActiveDownloads.TryAdd(download.RemoteToken, download);

                    await connection.SendMessageAsync(new PeerTransferResponseOutgoing(download.RemoteToken, true, download.Size, string.Empty).ToMessage());
                    Console.WriteLine($"Confirm sent");
                }

                return await downloadWait;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
        }

        #endregion Internal Methods

        #region Protected Methods

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
                    MessageWaiter.Dispose();

                    var message = "Client is being disposed.";

                    ServerConnection?.Disconnect(message);
                    ServerConnection?.Dispose();
                }

                Disposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private async Task<ConnectionKey> GetPeerConnectionKeyAsync(string username)
        {
            var addressWait = MessageWaiter.Wait<GetPeerAddressResponse>(MessageCode.ServerGetPeerAddress, username);

            var request = new GetPeerAddressRequest(username);
            await ServerConnection.SendMessageAsync(request.ToMessage());

            var address = await addressWait;
            return new ConnectionKey() { Username = username, IPAddress = address.IPAddress, Port = address.Port, Type = ConnectionType.Peer };
        }

        private IMessageConnection GetServerMessageConnection(string address, int port, ConnectionOptions options)
        {
            return new MessageConnection(ConnectionType.Server, Address, Port, options)
            {
                ConnectHandler = (conn) =>
                {
                    Task.Run(() => ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(conn))).Forget();
                },
                DisconnectHandler = (conn, message) =>
                {
                    Disconnect();
                    Task.Run(() => ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(conn, message))).Forget();
                },
                MessageHandler = HandleServerMessage,
            };
        }

        private async Task<IMessageConnection> GetSolicitedPeerConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options)
        {
            var connection = new MessageConnection(ConnectionType.Peer, connectToPeerResponse.Username, connectToPeerResponse.IPAddress.ToString(), connectToPeerResponse.Port, options)
            {
                Context = connectToPeerResponse,
                ConnectHandler = async (conn) =>
                {
                    var context = (ConnectToPeerResponse)conn.Context;
                    var request = new PierceFirewallRequest(context.Token).ToMessage();
                    await conn.SendMessageAsync(request, suppressCodeNormalization: true);
                },
                DisconnectHandler = async (conn, message) =>
                {
                    await PeerConnectionManager.Remove(conn);
                },
                MessageHandler = HandlePeerMessage,
            };

            await PeerConnectionManager.Add(connection);
            return connection;
        }

        private async Task<IConnection> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options)
        {
            return await GetTransferConnectionAsync(connectToPeerResponse.IPAddress.ToString(), connectToPeerResponse.Port, connectToPeerResponse.Token, options);
        }

        private async Task<IConnection> GetTransferConnectionAsync(string ipAddress, int port, int token, ConnectionOptions options)
        {
            var connection = new Connection(ipAddress, port, options);
            await connection.ConnectAsync();

            var request = new PierceFirewallRequest(token);
            await connection.SendAsync(request.ToMessage().ToByteArray());

            return connection;
        }

        private async Task<IMessageConnection> GetUnsolicitedPeerConnectionAsync(string username, ConnectionOptions options)
        {
            var key = await GetPeerConnectionKeyAsync(username);
            var connection = PeerConnectionManager.Get(key);

            if (connection != default(IMessageConnection))
            {
                if (connection.State == ConnectionState.Disconnecting || connection.State == ConnectionState.Disconnected)
                {
                    await PeerConnectionManager.Remove(connection);
                    connection = default(IMessageConnection);
                }
            }

            if (connection == default(IMessageConnection))
            {
                connection = new MessageConnection(ConnectionType.Peer, key.Username, key.IPAddress.ToString(), key.Port, options)
                {
                    ConnectHandler = async (conn) =>
                    {
                        var token = new Random().Next(1, 2147483647);
                        await connection.SendMessageAsync(new PeerInitRequest(Username, "P", token).ToMessage(), suppressCodeNormalization: true);
                    },
                    DisconnectHandler = async (conn, msg) =>
                    {
                        await PeerConnectionManager.Remove(conn);
                    },
                    MessageHandler = HandlePeerMessage,
                };

                await PeerConnectionManager.Add(connection);
            }

            return connection;
        }

        private async Task HandleConnectToPeer(ConnectToPeerResponse response)
        {
            if (response.Type == "F")
            {
                var connection = await GetTransferConnectionAsync(response, Options.TransferConnectionOptions);
                var tokenBytes = await connection.ReadAsync(4);
                var token = BitConverter.ToInt32(tokenBytes, 0);

                if (ActiveDownloads.TryGetValue(token, out var download))
                {
                    await connection.SendAsync(new byte[8]);

                    var bytes = await connection.ReadAsync(download.Size);
                    connection.Disconnect($"Transfer complete.");

                    MessageWaiter.Complete(MessageCode.PeerDownloadResponse, download.WaitKey, bytes);
                }
            }
            else
            {
                await GetSolicitedPeerConnectionAsync(response, Options.PeerConnectionOptions);
            }
        }

        private void HandlePeerMessage(IMessageConnection connection, Message message)
        {
            Console.WriteLine($"[PEER MESSAGE]: {message.Code}");
            switch (message.Code)
            {
                case MessageCode.PeerSearchResponse:
                    var searchResponse = SearchResponse.Parse(message);
                    if (ActiveSearches.TryGetValue(searchResponse.Token, out var search))
                    {
                        search.AddResponse(searchResponse);
                    }

                    break;

                case MessageCode.PeerBrowseResponse:
                    MessageWaiter.Complete(MessageCode.PeerBrowseResponse, connection.Key.Username, BrowseResponse.Parse(message));
                    break;

                case MessageCode.PeerTransferResponse:
                    var transferResponse = PeerTransferResponseIncoming.Parse(message);
                    MessageWaiter.Complete(MessageCode.PeerTransferResponse, Key(connection.Username, transferResponse.Token), transferResponse);
                    break;

                case MessageCode.PeerTransferRequest:
                    var transferRequest = PeerTransferRequestIncoming.Parse(message);
                    MessageWaiter.Complete(MessageCode.PeerTransferRequest, Key(connection.Username, transferRequest.Filename), transferRequest);

                    break;

                case MessageCode.PeerQueueFailed:
                    var pqfResponse = PeerQueueFailedResponse.Parse(message);
                    Console.WriteLine($"[PEER QUEUE FAILED]: {pqfResponse.Filename}; {pqfResponse.Message}");

                    break;

                default:
                    Console.WriteLine($"Unknown message: [{connection.IPAddress}] {message.Code}: {message.Payload.Length} bytes");
                    break;
            }
        }

        private async void HandleServerMessage(IMessageConnection connection, Message message)
        {
            Console.WriteLine($"[SERVER MESSAGE]: {message.Code}");

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
                    await HandleConnectToPeer(ConnectToPeerResponse.Parse(message));
                    break;

                case MessageCode.ServerPrivateMessages:
                    var pm = PrivateMessage.Parse(message);
                    Console.WriteLine($"[{pm.Timestamp}][{pm.Username}]: {pm.Message}");
                    await ServerConnection.SendMessageAsync(new AcknowledgePrivateMessageRequest(pm.Id).ToMessage());
                    break;

                case MessageCode.ServerGetPeerAddress:
                    var response = GetPeerAddressResponse.Parse(message);
                    MessageWaiter.Complete(message.Code, response.Username, response);
                    break;

                default:
                    Console.WriteLine($"Unknown message: {message.Code}: {message.Payload.Length} bytes");
                    break;
            }
        }

        private string Key(params object[] parts)
        {
            return string.Join(":", parts);
        }

        #endregion Private Methods
    }
}