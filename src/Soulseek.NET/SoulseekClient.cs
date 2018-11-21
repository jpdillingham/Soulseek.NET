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

        internal SoulseekClient(
            string address,
            int port,
            SoulseekClientOptions options,
            IMessageConnection serverConnection = null,
            IConnectionManager<IMessageConnection> messageConnectionManager = null,
            IMessageWaiter messageWaiter = null)
        {
            Address = address;
            Port = port;
            Options = options ?? new SoulseekClientOptions() { ConnectionOptions = new ConnectionOptions() { ReadTimeout = 0 } };
            ServerConnection = serverConnection ?? GetServerMessageConnection(Address, Port, Options.ConnectionOptions);
            MessageConnectionManager = messageConnectionManager ?? new ConnectionManager<IMessageConnection>(Options.ConcurrentPeerConnections);
            MessageWaiter = messageWaiter ?? new MessageWaiter(Options.MessageTimeout);
        }

        #endregion Public Constructors

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

        private ConcurrentDictionary<string, ConcurrentDictionary<int, PeerTransferRequestIncoming>> PendingDownloads { get; set; } = new ConcurrentDictionary<string, ConcurrentDictionary<int, PeerTransferRequestIncoming>>();
        private ConcurrentDictionary<int, Search> ActiveSearches { get; set; } = new ConcurrentDictionary<int, Search>();
        private bool Disposed { get; set; } = false;
        private IConnectionManager<IMessageConnection> MessageConnectionManager { get; set; }
        private IMessageWaiter MessageWaiter { get; set; }
        private Random Random { get; set; } = new Random();
        private IMessageConnection ServerConnection { get; set; }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
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

        internal async Task<BrowseResponse> BrowseAsync(string username, CancellationToken? cancellationToken = null, IMessageConnection connection = null)
        {
            try
            {
                connection = connection ?? await GetUnsolicitedPeerConnectionAsync(username, Options.PeerConnectionOptions);
                connection.DisconnectHandler += new Action<IConnection, string>((conn, message) =>
                {
                    MessageWaiter.Throw(MessageCode.PeerBrowseResponse, conn.Key.ToString(), new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                });

                var wait = MessageWaiter.WaitIndefinitely<BrowseResponse>(MessageCode.PeerBrowseResponse, connection.Key.ToString(), cancellationToken);
                await connection.SendMessageAsync(new PeerBrowseRequest().ToMessage());
                return await wait;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {Username}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ConnectionStateException">Thrown when the client is already connected, or is transitioning between states.</exception>
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

        public async Task<byte[]> DownloadAsync(string username, string filename, DownloadOptions options = null, CancellationToken? cancellationToken = null)
        {
            return await DownloadAsync(username, filename, options, cancellationToken, null);
        }

        internal async Task<byte[]> DownloadAsync(string username, string filename, DownloadOptions options = null, CancellationToken? cancellationToken = null, IMessageConnection connection = null)
        {
            try
            {
                connection = connection ?? await GetUnsolicitedPeerConnectionAsync(username, Options.PeerConnectionOptions);

                var downloadKey = $"{username}:{filename}";

                connection.DisconnectHandler += new Action<IConnection, string>((conn, message) =>
                {
                    MessageWaiter.Throw(MessageCode.PeerDownloadResponse, downloadKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                });

                var wait = MessageWaiter.WaitIndefinitely<byte[]>(MessageCode.PeerDownloadResponse, downloadKey, cancellationToken);

                // request the file from the peer using a new token.  wait for the response, which will be identified with the token.
                var token = new Random().Next();
                var responseToken = $"{connection.Key}:{token}";
                var peerTransferResponse = MessageWaiter.WaitIndefinitely<PeerTransferResponseIncoming>(MessageCode.PeerTransferResponse, responseToken, cancellationToken);

                // the peer will eventually signal that it is ready for the transfer by sending a transfer request, which will be identififed with a new token
                // and the filename.  wait for that message.
                var requestToken = $"{connection.Key}:{filename}";
                var peerTransferRequestResponse = MessageWaiter.WaitIndefinitely<PeerTransferRequestIncoming>(MessageCode.PeerTransferRequest, requestToken, cancellationToken);

                await connection.SendMessageAsync(new PeerTransferRequestOutgoing(TransferDirection.Download, token, filename).ToMessage());

                var transferResponse = await peerTransferResponse;

                if (transferResponse.Allowed)
                {
                    Console.WriteLine($"TODO: download now");
                }
                else
                {
                    //Console.WriteLine($"Download disallowed; wait for peer to call back.  Token: {transferRequest.Token}, Filename: {transferRequest.Filename}");
                    var transferRequest = await peerTransferRequestResponse;

                    // add the request to the list of pending downloads
                    if (PendingDownloads.TryGetValue(username, out var downloads))
                    {
                        downloads.TryAdd(transferRequest.Token, transferRequest);
                    }
                    else
                    {
                        var initialDownloads = new ConcurrentDictionary<int, PeerTransferRequestIncoming>();
                        initialDownloads.TryAdd(transferRequest.Token, transferRequest);

                        PendingDownloads.AddOrUpdate(username, initialDownloads, (key, existingDownloads) =>
                        {
                            existingDownloads.TryAdd(transferRequest.Token, transferRequest);
                            return existingDownloads;
                        });
                    }

                    // respond to the peer that we are ready to start
                    await connection.SendMessageAsync(new PeerTransferResponseOutgoing(transferRequest.Token, true, 0, string.Empty).ToMessage());
                }

                return await wait;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
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

            Console.WriteLine($"Sending login message");
            await ServerConnection.SendMessageAsync(new LoginRequest(username, password).ToMessage());
            Console.WriteLine($"Login message sent");

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

            options = options ?? new SearchOptions();

            var search = new Search(searchText, options, ServerConnection)
            {
                ResponseHandler = (s, response) =>
                {
                    var e = new SearchResponseReceivedEventArgs() { Search = s, Response = response };
                    Task.Run(() => SearchResponseReceived?.Invoke(this, e)).Forget();
                }
            };

            ActiveSearches.TryAdd(search.Ticket, search);

            return await search.SearchAsync(cancellationToken);
        }

        #endregion Public Methods

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

        private async Task ConnectToPeerHandler(ConnectToPeerResponse response)
        {
            if (response.Type == "F")
            {
                // check to make sure we are expecting a download from this peer, and if so, get the dict of pending downloads
                if (PendingDownloads.TryGetValue(response.Username, out var pendingDownloads))
                {
                    // connect, pierce the firewall, and retrieve the transfer token
                    var t = new Connection(response.IPAddress.ToString(), response.Port, Options.TransferConnectionOptions);
                    await t.ConnectAsync();

                    var request = new PierceFirewallRequest(response.Token);
                    await t.SendAsync(request.ToMessage().ToByteArray());

                    var tokenBytes = await t.ReadAsync(4);
                    var token = BitConverter.ToInt32(tokenBytes, 0);

                    // check to make sure we are expecting this particular file, and if so grab the original request
                    if (pendingDownloads.TryGetValue(token, out var peerTransferRequestIncoming))
                    {
                        await t.SendAsync(new byte[8]);

                        var bytes = await t.ReadAsync(peerTransferRequestIncoming.Size);
                        t.Disconnect($"Transfer complete.");

                        MessageWaiter.Complete(MessageCode.PeerDownloadResponse, $"{response.Username}:{peerTransferRequestIncoming.Filename}", bytes);
                    }
                }
            }
            else
            {
                await GetSolicitedPeerConnectionAsync(response, Options.PeerConnectionOptions);
            }
        }

        private async Task<GetPeerAddressResponse> GetPeerAddressAsync(string username)
        {
            var request = new GetPeerAddressRequest(username);
            await ServerConnection.SendMessageAsync(request.ToMessage());

            return await MessageWaiter.Wait<GetPeerAddressResponse>(MessageCode.ServerGetPeerAddress, username);
        }

        private void PeerMessageHandler(IMessageConnection connection, Message message)
        {
            //Console.WriteLine($"[PEER MESSAGE]: {message.Code}");
            switch (message.Code)
            {
                case MessageCode.PeerSearchResponse:
                    var searchResponse = SearchResponse.Parse(message);
                    if (ActiveSearches.TryGetValue(searchResponse.Ticket, out var search))
                    {
                        search.AddResponse(connection, searchResponse);
                    }

                    break;

                case MessageCode.PeerBrowseResponse:
                    MessageWaiter.Complete(MessageCode.PeerBrowseResponse, connection.Key.ToString(), BrowseResponse.Parse(message));
                    break;
                case MessageCode.PeerTransferResponse:
                    var transferResponse = PeerTransferResponseIncoming.Parse(message);
                    MessageWaiter.Complete(MessageCode.PeerTransferResponse, $"{connection.Key}:{transferResponse.Token}", transferResponse);
                    break;
                case MessageCode.PeerTransferRequest:
                    var transferRequest = PeerTransferRequestIncoming.Parse(message);
                    MessageWaiter.Complete(MessageCode.PeerTransferRequest, $"{connection.Key}:{transferRequest.Filename}", transferRequest);

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

        private async Task PrivateMessageHandler(PrivateMessage message)
        {
            Console.WriteLine($"[{message.Timestamp}][{message.Username}]: {message.Message}");
            await ServerConnection.SendMessageAsync(new AcknowledgePrivateMessageRequest(message.Id).ToMessage());
        }

        private async void ServerMessageHandler(IMessageConnection connection, Message message)
        {
            //Console.WriteLine($"[SERVER MESSAGE]: {message.Code}");

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
                    await ConnectToPeerHandler(ConnectToPeerResponse.Parse(message));
                    break;

                case MessageCode.ServerPrivateMessages:
                    await PrivateMessageHandler(PrivateMessage.Parse(message));
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

        #endregion Private Methods

        private async Task<ConnectionKey> GetPeerConnectionKeyAsync(string username)
        {
            var address = await GetPeerAddressAsync(username);
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
                MessageHandler = ServerMessageHandler,
            };
        }

        private async Task<IMessageConnection> GetUnsolicitedPeerConnectionAsync(string username, ConnectionOptions options)
        {
            var key = await GetPeerConnectionKeyAsync(username);
            var connection = MessageConnectionManager.Get(key);

            if (connection != default(IMessageConnection))
            {
                if (connection.State == ConnectionState.Disconnecting || connection.State == ConnectionState.Disconnected)
                {
                    await MessageConnectionManager.Remove(connection);
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
                        await MessageConnectionManager.Remove(conn);
                    },
                    MessageHandler = PeerMessageHandler,
                };

                await MessageConnectionManager.Add(connection);
            }

            return connection;
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
                    await MessageConnectionManager.Remove(conn);
                },
                MessageHandler = PeerMessageHandler,
            };

            await MessageConnectionManager.Add(connection);
            return connection;
        }

        private struct DownloadKey
        {
            ConnectionKey ConnectionKey;
            string Filename;
        }
    }
}