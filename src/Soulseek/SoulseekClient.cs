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

namespace Soulseek
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : ISoulseekClient
    {
        private const string DefaultAddress = "vps.slsknet.org";
        private const int DefaultPort = 2271;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="options">The client options.</param>
        public SoulseekClient(ClientOptions options)
            : this(DefaultAddress, DefaultPort, options)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client options.</param>
        public SoulseekClient(string address = DefaultAddress, int port = DefaultPort, ClientOptions options = null)
            : this(address, port, options, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client options.</param>
        /// <param name="serverConnection">The IMessageConnection instance to use.</param>
        /// <param name="peerConnectionManager">The IPeerConnectionManager instance to use.</param>
        /// <param name="serverMessageHandler">The IServerMessageHandler instance to use.</param>
        /// <param name="peerMessageHandler">The IPeerMessageHandler instance to use.</param>
        /// <param name="listener">The IListener instance to use.</param>
        /// <param name="waiter">The IWaiter instance to use.</param>
        /// <param name="tokenFactory">The ITokenFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        internal SoulseekClient(
            string address,
            int port,
            ClientOptions options = null,
            IMessageConnection serverConnection = null,
            IPeerConnectionManager peerConnectionManager = null,
            IDistributedConnectionManager distributedConnectionManager = null,
            IServerMessageHandler serverMessageHandler = null,
            IPeerMessageHandler peerMessageHandler = null,
            IListener listener = null,
            IListenerHandler listenerHandler = null,
            IWaiter waiter = null,
            ITokenFactory tokenFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            Address = address;
            Port = port;

            Options = options ?? new ClientOptions();

            Waiter = waiter ?? new Waiter(Options.MessageTimeout);
            TokenFactory = tokenFactory ?? new TokenFactory(Options.StartingToken);
            Diagnostic = diagnosticFactory ?? new DiagnosticFactory(this, Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            ServerConnection = serverConnection;

            if (ServerConnection == null)
            {
                try
                {
                    IPAddress = Address.ResolveIPAddress();
                }
                catch (SocketException ex)
                {
                    throw new SoulseekClientException($"Failed to resolve address '{address}': {ex.Message}", ex);
                }

                // substitute the existing inactivity value with -1 to keep the connection open indefinitely
                var (readBufferSize, writeBufferSize, connectTimeout, _) = Options.ServerConnectionOptions;
                var connectionOptions = new ConnectionOptions(readBufferSize, writeBufferSize, connectTimeout, inactivityTimeout: -1);

                ServerConnection = new MessageConnection(IPAddress, Port, connectionOptions);
            }

            ServerConnection.Connected += (sender, e) => ChangeState(SoulseekClientStates.Connected);
            ServerConnection.Disconnected += ServerConnection_Disconnected;

            ListenerHandler = listenerHandler ?? new ListenerHandler(this);
            ListenerHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            Listener = listener;

            if (Listener == null && Options.ListenPort.HasValue)
            {
                Listener = new Listener(Options.ListenPort.Value, connectionOptions: Options.IncomingConnectionOptions);
            }

            if (Listener != null)
            {
                Listener.Accepted += ListenerHandler.HandleConnection;
            }

            PeerMessageHandler = peerMessageHandler ?? new PeerMessageHandler(this);
            PeerMessageHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            DistributedMessageHandler = DistributedMessageHandler ?? new DistributedMessageHandler(this);
            DistributedMessageHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            PeerConnectionManager = peerConnectionManager ?? new PeerConnectionManager(this);
            PeerConnectionManager.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            DistributedConnectionManager = distributedConnectionManager ?? new DistributedConnectionManager(this);
            DistributedConnectionManager.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            ServerMessageHandler = serverMessageHandler ?? new ServerMessageHandler(this);
            ServerMessageHandler.UserStatusChanged += (sender, e) => UserStatusChanged?.Invoke(this, e);
            ServerMessageHandler.PrivateMessageReceived += (sender, e) => PrivateMessageReceived?.Invoke(this, e);
            ServerMessageHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            ServerConnection.MessageRead += ServerMessageHandler.HandleMessage;
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessage> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when a new search result is received.
        /// </summary>
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Occurs when a search changes state.
        /// </summary>
        public event EventHandler<SearchStateChangedEventArgs> SearchStateChanged;

        /// <summary>
        ///     Occurs when the client changes state.
        /// </summary>
        public event EventHandler<SoulseekClientStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Occurs when an active transfer sends or receives data.
        /// </summary>
        public event EventHandler<TransferProgressUpdatedEventArgs> TransferProgressUpdated;

        /// <summary>
        ///     Occurs when a transfer changes state.
        /// </summary>
        public event EventHandler<TransferStateChangedEventArgs> TransferStateChanged;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        public event EventHandler<UserStatusChangedEventArgs> UserStatusChanged;

        /// <summary>
        ///     Gets the unresolved server address.
        /// </summary>
        public string Address { get; }

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        public virtual ClientOptions Options { get; }

        /// <summary>
        ///     Gets server port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public virtual SoulseekClientStates State { get; private set; } = SoulseekClientStates.Disconnected;

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        public string Username { get; private set; }

        internal virtual IDistributedConnectionManager DistributedConnectionManager { get; }
        internal virtual IDistributedMessageHandler DistributedMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, Transfer> Downloads { get; set; } = new ConcurrentDictionary<int, Transfer>();
        internal virtual IListener Listener { get; }
        internal virtual IListenerHandler ListenerHandler { get; }
        internal virtual IPeerConnectionManager PeerConnectionManager { get; }
        internal virtual IPeerMessageHandler PeerMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, Search> Searches { get; set; } = new ConcurrentDictionary<int, Search>();
        internal virtual IMessageConnection ServerConnection { get; }
        internal virtual IServerMessageHandler ServerMessageHandler { get; }
        internal virtual IWaiter Waiter { get; }

        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; } = false;
        private ITokenFactory TokenFactory { get; }
        private ConcurrentDictionary<string, SemaphoreSlim> Uploads { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        ///     Asynchronously sends a private message acknowledgement for the specified <paramref name="privateMessageId"/>.
        /// </summary>
        /// <param name="privateMessageId">The unique id of the private message to acknowledge.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        public virtual async Task AcknowledgePrivateMessageAsync(int privateMessageId, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to acknowledge private messages (currently: {State})");
            }

            try
            {
                await ServerConnection.WriteAsync(new AcknowledgePrivateMessageRequest(privateMessageId).ToByteArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new PrivateMessageException($"Failed to acknowledge private message with ID {privateMessageId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="username"/> to the server watch list.
        /// </summary>
        /// <param name="username">The username of the user to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="AddUserException">Thrown when an exception is encountered during the operation.</exception>
        public Task<AddUserResponse> AddUserAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to add users (currently: {State})");
            }

            return AddUserInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The operation response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="BrowseException">Thrown when an exception is encountered during the operation.</exception>
        public Task<BrowseResponse> BrowseAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to browse.");
            }

            return BrowseInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
        /// <exception cref="ConnectionException">Thrown when an exception is encountered during the operation.</exception>
        public async Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            if (State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"Failed to connect; the client is already connected.");
            }

            try
            {
                Listener?.Start();

                await ServerConnection.ConnectAsync(cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ConnectionException($"Failed to connect: {ex.Message}.", ex);
            }
        }

        /// <summary>
        ///     Disconnects the client from the server.
        /// </summary>
        /// <param name="message">An optional message describing the reason the client is being disconnected.</param>
        public void Disconnect(string message = null)
        {
            ServerConnection.Disconnected -= ServerConnection_Disconnected;
            ServerConnection?.Disconnect(message ?? "Client disconnected.");

            Listener?.Stop();

            PeerConnectionManager?.RemoveAndDisposeAll();
            DistributedConnectionManager?.RemoveAndDisposeAll();

            Searches?.RemoveAndDisposeAll();
            Downloads?.RemoveAll();

            Waiter?.CancelAll();

            Username = null;

            if (State != SoulseekClientStates.Disconnected)
            {
                ChangeState(SoulseekClientStates.Disconnected, message);
            }
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Asynchronously downloads the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="filename">The file to download.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including a byte array containing the file contents.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        public Task<byte[]> DownloadAsync(string username, string filename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace.", nameof(filename));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to browse.");
            }

            token = token ?? TokenFactory.NextToken();

            if (Downloads.ContainsKey((int)token))
            {
                throw new ArgumentException($"An active or queued download with token {token} is already in progress.", nameof(token));
            }

            if (Downloads.Values.Any(d => d.Username == username && d.Filename == filename))
            {
                throw new ArgumentException($"An active of queued download of {filename} from {username} is already in progress.");
            }

            options = options ?? new TransferOptions();

            return DownloadInternalAsync(username, filename, (int)token, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously gets the current place of the specified <paramref name="filename"/> in the queue of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user whose queue to check.</param>
        /// <param name="filename">The file to check.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The current place of the file in the queue.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DownloadNotFoundException">Thrown when a corresponding download is not active.</exception>
        /// <exception cref="DownloadPlaceInQueueException">Thrown when an exception is encountered during the operation.</exception>
        public Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace.", nameof(filename));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be Connected and LoggedIn to check download queue position (currently: {State})");
            }

            if (!Downloads.Any(d => d.Value.Username == username && d.Value.Filename == filename))
            {
                throw new DownloadNotFoundException($"A download of {filename} from user {username} is not active.");
            }

            return GetDownloadPlaceInQueueInternalAsync(username, filename, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Gets the next token for use in client operations.
        /// </summary>
        /// <remarks>
        ///     <para>Tokens are returned sequentially and the token value rolls over to 0 when it has reached <see cref="int.MaxValue"/>.</para>
        ///     <para>This operation is thread safe.</para>
        /// </remarks>
        /// <returns>The next token.</returns>
        /// <threadsafety instance="true"/>
        public int GetNextToken() => TokenFactory.NextToken();

        /// <summary>
        ///     Asynchronously fetches the IP address and port of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the connection information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the connection information.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the client is not connected to the server, or no user is logged in.
        /// </exception>
        /// <exception cref="UserAddressException">Thrown when an exception is encountered during the operation.</exception>
        public Task<(IPAddress IPAddress, int Port)> GetUserAddressAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user information (currently: {State})");
            }

            return GetUserAddressInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously fetches information about the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the information response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the client is not connected to the server, or no user is logged in.
        /// </exception>
        /// <exception cref="UserInfoException">Thrown when an exception is encountered during the operation.</exception>
        public Task<UserInfoResponse> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user information (currently: {State})");
            }

            return GetUserInfoInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously fetches the status of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user for which to fetch the status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="UserStatusException">Thrown when an exception is encountered during the operation.</exception>
        public Task<GetStatusResponse> GetUserStatusAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user status (currently: {State})");
            }

            return GetUserStatusInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="LoginException">
        ///     Thrown when the login fails, or when an exception is encountered during the operation.
        /// </exception>
        public Task LoginAsync(string username, string password, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Username may not be null or an empty string.", nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password may not be null or an empty string.", nameof(password));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The client must be connected to log in.");
            }

            if (State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"Already logged in as {Username}.  Disconnect before logging in again.");
            }

            return LoginInternalAsync(username, password, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="searchText"/> using the specified unique
        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the search results.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the client is not connected to the server, or no user is logged in.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="searchText"/> is null, empty, or consists of only whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when a search with the specified <paramref name="token"/> is already in progress.
        /// </exception>
        /// <exception cref="SearchException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        public Task<IReadOnlyCollection<SearchResponse>> SearchAsync(string searchText, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException($"Search text must not be a null or empty string, or one consisting only of whitespace.", nameof(searchText));
            }

            token = token ?? TokenFactory.NextToken();

            if (Searches.ContainsKey((int)token))
            {
                throw new ArgumentException($"An active search with token {token} is already in progress.", nameof(token));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to search (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to search.");
            }

            options = options ?? new SearchOptions();

            return SearchInternalAsync(searchText, (int)token, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously sends the specified private <paramref name="message"/> to the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="message"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        public Task SendPrivateMessageAsync(string username, string message, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException($"The message must not be a null or empty string, or one consisting only of whitespace.", nameof(message));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to browse.");
            }

            return SendPrivateMessageInternalAsync(username, message, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously informs the server of the number of shared <paramref name="directories"/> and <paramref name="files"/>.
        /// </summary>
        /// <param name="directories">The number of shared directories.</param>
        /// <param name="files">The number of shared files.</param>
        /// <param name="cancellationToken">The token to monitor for cancelation requests.</param>
        /// <returns>The operation context.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="ConnectionWriteException">Thrown when an exception is encountered during the operation.</exception>
        public Task SetSharedCountsAsync(int directories, int files, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set shared counts (currently: {State})");
            }

            return ServerConnection.WriteAsync(new SetSharedCountsRequest(directories, files).ToByteArray(), cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously informs the server of the current online <paramref name="status"/> of the client.
        /// </summary>
        /// <param name="status">The current status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="ConnectionWriteException">Thrown when an exception is encountered during the operation.</exception>
        public Task SetStatusAsync(UserStatus status, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set status (currently: {State})");
            }

            return ServerConnection.WriteAsync(new SetStatusRequest(status).ToByteArray(), cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously uploads the specified <paramref name="filename"/> and <paramref name="data"/> to the the specified
        ///     <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="filename">The filename of the file to upload.</param>
        /// <param name="data">The data to upload.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        public Task UploadAsync(string username, string filename, byte[] data, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            token = token ?? GetNextToken();
            options = options ?? new TransferOptions();

            return UploadInternalAsync(username, filename, data, (int)token, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        /// <param name="disposing">A value indicating whether disposal is in progress.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                Disconnect("Client is being disposed.");

                if (disposing)
                {
                    PeerConnectionManager?.Dispose();
                    DistributedConnectionManager?.Dispose();
                    Waiter?.Dispose();
                    ServerConnection?.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<AddUserResponse> AddUserInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var addUserWait = Waiter.Wait<AddUserResponse>(new WaitKey(MessageCode.Server.AddUser, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new AddUserRequest(username).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await addUserWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                throw new AddUserException($"Failed to retrieve information for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task<BrowseResponse> BrowseInternalAsync(string username, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.BrowseResponse, username);
                var browseWait = Waiter.WaitIndefinitely<BrowseResponse>(waitKey, cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    Waiter.Throw(waitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                var sw = new System.Diagnostics.Stopwatch();
                Diagnostic.Debug($"Sending browse request to peer {username}");
                sw.Start();

                await connection.WriteAsync(new BrowseRequest().ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await browseWait.ConfigureAwait(false);

                sw.Stop();
                Diagnostic.Debug($"Browse of {username} completed in {sw.ElapsedMilliseconds}ms.  {response.DirectoryCount} directories fetched.");

                return response;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {username}: {ex.Message}", ex);
            }
        }

        private void ChangeState(SoulseekClientStates state, string message = null)
        {
            var previousState = State;
            State = state;
            StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(previousState, State, message));
        }

        private async Task<byte[]> DownloadInternalAsync(string username, string filename, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            var download = new Transfer(TransferDirection.Download, username, filename, token, options);
            Task<byte[]> downloadCompleted = null;
            var lastState = TransferStates.None;

            void UpdateState(TransferStates state)
            {
                download.State = state;
                var args = new TransferStateChangedEventArgs(previousState: lastState, transfer: download);
                lastState = state;
                options.StateChanged?.Invoke(args);
                TransferStateChanged?.Invoke(this, args);
            }

            void UpdateProgress(int bytesDownloaded)
            {
                var lastBytes = download.BytesTransferred;
                download.UpdateProgress(bytesDownloaded);
                var eventArgs = new TransferProgressUpdatedEventArgs(lastBytes, download);
                options.ProgressUpdated?.Invoke(eventArgs);
                TransferProgressUpdated?.Invoke(this, eventArgs);
            }

            try
            {
                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);
                var peerConnection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);

                Downloads.TryAdd(download.Token, download);

                // prepare two waits; one for the transfer response to confirm that our request is acknowledged and another for the
                // eventual transfer request sent when the peer is ready to send the file. the response message should be returned
                // immediately, while the request will be sent only when we've reached the front of the remote queue.
                var transferRequestAcknowledged = Waiter.Wait<TransferResponse>(
                    new WaitKey(MessageCode.Peer.TransferResponse, download.Username, download.Token), null, cancellationToken);
                var transferStartRequested = Waiter.WaitIndefinitely<TransferRequest>(
                    new WaitKey(MessageCode.Peer.TransferRequest, download.Username, download.Filename), cancellationToken);

                // request the file
                await peerConnection.WriteAsync(new TransferRequest(TransferDirection.Download, token, filename).ToByteArray(), cancellationToken).ConfigureAwait(false);
                UpdateState(TransferStates.Requested);

                Console.WriteLine($"Waiting for ACK {download.Username} {download.Token}");
                var transferRequestAcknowledgement = await transferRequestAcknowledged.ConfigureAwait(false);

                Console.WriteLine($"Transfer request ACKed");

                if (transferRequestAcknowledgement.Allowed)
                {
                    var tfa = transferRequestAcknowledgement;
                    Console.WriteLine($"Transfer allowed {tfa.Token} {tfa.FileSize}");

                    // the peer is ready to initiate the transfer immediately; we are bypassing their queue. note that only the
                    // legacy client operates this way; SoulseekQt always returns Allowed = false regardless of the current queue.
                    UpdateState(TransferStates.Initializing);

                    download.Size = transferRequestAcknowledgement.FileSize;

                    // prepare a wait for the overall completion of the download
                    downloadCompleted = Waiter.WaitIndefinitely<byte[]>(download.WaitKey, cancellationToken);

                    // connect to the peer to retrieve the file; for these types of transfers, we must initiate the transfer connection.
                    download.Connection = await PeerConnectionManager
                        .GetTransferConnectionAsync(username, address.IPAddress, address.Port, transferRequestAcknowledgement.Token, cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (transferRequestAcknowledgement.Message.Equals("File not shared.", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new TransferRejectedException(transferRequestAcknowledgement.Message);
                }
                else
                {
                    Console.WriteLine($"Transfer disallowed");

                    // the download is remotely queued, so put it in the local queue.
                    UpdateState(TransferStates.Queued);

                    // wait for the peer to respond that they are ready to start the transfer
                    var transferStartRequest = await transferStartRequested.ConfigureAwait(false);

                    var tsr = transferStartRequest;
                    Console.WriteLine($"Start request: token {tsr.Token} filename {tsr.Filename} size: {tsr.FileSize}");

                    download.Size = transferStartRequest.FileSize;
                    download.RemoteToken = transferStartRequest.Token;
                    UpdateState(TransferStates.Initializing);

                    // wait for both direct and indirect connections, since the official client attempts both types immediately
                    var indirectTransferConnectionInitialized = Waiter.Wait<IConnection>(
                        key: new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken),
                        timeout: Options.PeerConnectionOptions.ConnectTimeout,
                        cancellationToken: cancellationToken);

                    var directTransferConnectionInitialized = Waiter.Wait<IConnection>(
                        key: new WaitKey(Constants.WaitKey.DirectTransfer, download.Username, download.RemoteToken),
                        timeout: Options.PeerConnectionOptions.ConnectTimeout,
                        cancellationToken: cancellationToken);

                    // also prepare a wait for the overall completion of the download
                    downloadCompleted = Waiter.WaitIndefinitely<byte[]>(download.WaitKey, cancellationToken);

                    Console.WriteLine($"Transfer start recieved.  Trying to connect.");

                    // respond to the peer that we are ready to accept the file but first, get a fresh connection (or maybe it's
                    // cached in the manager) to the peer in case it disconnected and was purged while we were waiting.
                    peerConnection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);

                    Console.WriteLine($"Sending transfer response.");
                    await peerConnection.WriteAsync(new TransferResponse(download.RemoteToken.Value, download.Size).ToByteArray(), cancellationToken).ConfigureAwait(false);
                    Console.WriteLine($"Response sent.  Waiting for connection...");

                    try
                    {
                        var connectionInitialized = await Task.WhenAny(indirectTransferConnectionInitialized, directTransferConnectionInitialized).ConfigureAwait(false);
                        download.Connection = connectionInitialized.Result;
                    }
                    catch (AggregateException ex)
                    {
                        // todo: write some tests to make sure this surfaces realistic exceptions for different scenarios. bubbling
                        // an AggregateException here leaks too many implementation details.
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }

                download.Connection.DataRead += (sender, e) => UpdateProgress(e.CurrentLength);
                download.Connection.Disconnected += (sender, message) =>
                {
                    if (download.State.HasFlag(TransferStates.Succeeded))
                    {
                        Waiter.Complete(download.WaitKey, download.Data);
                    }
                    else if (download.State.HasFlag(TransferStates.TimedOut))
                    {
                        Waiter.Throw(download.WaitKey, new TimeoutException(message));
                    }
                    else
                    {
                        Waiter.Throw(download.WaitKey, new ConnectionException($"Transfer failed: {message}"));
                    }
                };

                try
                {
                    Console.WriteLine($"Download connection established.  Sending magic bytes...");

                    // this needs to be 16? bytes for transfers beginning immediately, or 8 for queued. not sure what this is; it
                    // was identified via WireShark.
                    await download.Connection.WriteAsync(new byte[8], cancellationToken).ConfigureAwait(false);

                    UpdateState(TransferStates.InProgress);

                    var bytes = await download.Connection.ReadAsync(download.Size, cancellationToken).ConfigureAwait(false);

                    download.Data = bytes.ToArray();
                    download.State = TransferStates.Succeeded;

                    download.Connection.Disconnect("Transfer complete.");
                    Diagnostic.Info($"Download of {System.IO.Path.GetFileName(download.Filename)} from {username} complete ({download.Data.Length} of {download.Size} bytes).");
                }
                catch (TimeoutException)
                {
                    download.State = TransferStates.TimedOut;
                    download.Connection.Disconnect($"Transfer timed out after {Options.TransferConnectionOptions.InactivityTimeout} seconds of inactivity.");
                }
                catch (Exception ex)
                {
                    download.Connection.Disconnect(ex.Message);
                }

                // wait for the download to complete this wait is either completed (on success) or thrown (on anything other than
                // success) in the Disconnected event handler of the transfer connection
                download.Data = await downloadCompleted.ConfigureAwait(false);
                return download.Data;
            }
            catch (TransferRejectedException ex)
            {
                download.State = TransferStates.Rejected;
                download.Connection?.Disconnect("Transfer rejected.");

                throw new TransferException($"Download of file {filename} rejected by user {username}: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                download.State = TransferStates.Cancelled;
                download.Connection?.Disconnect("Transfer cancelled.");

                Diagnostic.Debug(ex.ToString());
                throw;
            }
            catch (TimeoutException ex)
            {
                download.State = TransferStates.TimedOut;
                download.Connection?.Disconnect("Transfer timed out.");

                Diagnostic.Debug(ex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                download.State = TransferStates.Errored;
                download.Connection?.Disconnect("Transfer error.");

                Diagnostic.Debug(ex.ToString());
                throw new TransferException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
            finally
            {
                // clean up the wait in case the code threw before it was awaited.
                Waiter.Complete<byte[]>(download.WaitKey, null);
                Downloads.TryRemove(download.Token, out var _);

                download.Connection?.Dispose();

                // change state so we can fire the progress update a final time with the updated state little bit of a hack to
                // avoid cloning the download
                download.State = TransferStates.Completed | download.State;
                UpdateProgress(download.Data?.Length ?? 0);
                UpdateState(download.State);
            }
        }

        private async Task<int> GetDownloadPlaceInQueueInternalAsync(string username, string filename, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.PlaceInQueueResponse, username, filename);
                var responseWait = Waiter.Wait<PlaceInQueueResponse>(waitKey, null, cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    Waiter.Throw(waitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                await connection.WriteAsync(new PeerPlaceInQueueRequest(filename).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await responseWait.ConfigureAwait(false);

                return response.PlaceInQueue;
            }
            catch (Exception ex)
            {
                throw new DownloadPlaceInQueueException($"Failed to fetch place in queue for download of {filename} from {username}: {ex.Message}", ex);
            }
        }

        private async Task<(IPAddress IPAddress, int Port)> GetUserAddressInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.GetPeerAddress, username);
                var addressWait = Waiter.Wait<GetPeerAddressResponse>(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new GetPeerAddressRequest(username).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var address = await addressWait.ConfigureAwait(false);

                if (address.IPAddress.Equals(IPAddress.Parse("0.0.0.0")))
                {
                    throw new PeerOfflineException($"User {username} appears to be offline.");
                }

                return (address.IPAddress, address.Port);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new UserAddressException($"Failed to retrieve address for user {username}: {ex.Message}", ex);
            }
        }

        private async Task<UserInfoResponse> GetUserInfoInternalAsync(string username, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.InfoResponse, username);
                var infoWait = Waiter.Wait<UserInfoResponse>(waitKey, cancellationToken: cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    Waiter.Throw(waitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                await connection.WriteAsync(new UserInfoRequest().ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await infoWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                throw new UserInfoException($"Failed to retrieve information for user {username}: {ex.Message}", ex);
            }
        }

        private async Task<GetStatusResponse> GetUserStatusInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var getStatusWait = Waiter.Wait<GetStatusResponse>(new WaitKey(MessageCode.Server.GetStatus, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new GetStatusRequest(username).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await getStatusWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                throw new UserStatusException($"Failed to retrieve status for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task LoginInternalAsync(string username, string password, CancellationToken cancellationToken)
        {
            try
            {
                var loginWait = Waiter.Wait<LoginResponse>(new WaitKey(MessageCode.Server.Login), cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new LoginRequest(username, password).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await loginWait.ConfigureAwait(false);

                if (response.Succeeded)
                {
                    Username = username;
                    ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    if (Options.ListenPort.HasValue)
                    {
                        // the client sends an undocumented message in the format 02/listen port/01/obfuscated port we don't
                        // support obfuscation, so we send only the listen port. it probably wouldn't hurt to send an 00 afterwards.
                        await ServerConnection.WriteAsync(new SetListenPortRequest(Options.ListenPort.Value).ToByteArray(), cancellationToken).ConfigureAwait(false);
                    }

                    await ServerConnection.WriteAsync(new HaveNoParents(true).ToByteArray(), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Disconnect($"The server rejected login attempt: {response.Message}"); // upon login failure the server will refuse to allow any more input, eventually disconnecting.
                    throw new LoginException($"The server rejected login attempt: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new LoginException($"Failed to log in as {username}: {ex.Message}", ex);
            }
        }

        private async Task<IReadOnlyCollection<SearchResponse>> SearchInternalAsync(string searchText, int token, SearchOptions options, CancellationToken cancellationToken)
        {
            var search = new Search(searchText, token, options);
            var lastState = SearchStates.None;

            void UpdateState(SearchStates state)
            {
                search.State = state;
                var args = new SearchStateChangedEventArgs(previousState: lastState, search: search);
                lastState = state;
                options.StateChanged?.Invoke(args);
                SearchStateChanged?.Invoke(this, args);
            }

            try
            {
                search.ResponseReceived = (response) =>
                {
                    var eventArgs = new SearchResponseReceivedEventArgs(response, search);
                    options.ResponseReceived?.Invoke(eventArgs);
                    SearchResponseReceived?.Invoke(this, eventArgs);
                };

                Searches.TryAdd(search.Token, search);
                UpdateState(SearchStates.Requested);

                await ServerConnection.WriteAsync(new SearchRequest(search.SearchText, search.Token).ToByteArray(), cancellationToken).ConfigureAwait(false);
                UpdateState(SearchStates.InProgress);

                var responses = await search.WaitForCompletion(cancellationToken).ConfigureAwait(false);
                return responses.ToList().AsReadOnly();
            }
            catch (OperationCanceledException ex)
            {
                search.Complete(SearchStates.Cancelled);
                throw new SearchException($"Search for {searchText} ({token}) was cancelled.", ex);
            }
            catch (Exception ex)
            {
                search.Complete(SearchStates.Errored);
                throw new SearchException($"Failed to search for {searchText} ({token}): {ex.Message}", ex);
            }
            finally
            {
                Searches.TryRemove(search.Token, out _);

                UpdateState(SearchStates.Completed | search.State);
                search.Dispose();
            }
        }

        private async Task SendPrivateMessageInternalAsync(string username, string message, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new PrivateMessageRequest(username, message).ToByteArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new PrivateMessageException($"Failed to send private message to user {username}: {ex.Message}", ex);
            }
        }

        private void ServerConnection_Disconnected(object sender, string e)
        {
            Disconnect(e);
        }

        private async Task UploadInternalAsync(string username, string filename, byte[] data, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            var upload = new Transfer(TransferDirection.Upload, username, filename, token, options)
            {
                Size = data.Length,
            };

            var lastState = TransferStates.None;

            void UpdateState(TransferStates state)
            {
                upload.State = state;
                var args = new TransferStateChangedEventArgs(previousState: lastState, transfer: upload);
                lastState = state;
                options.StateChanged?.Invoke(args);
                TransferStateChanged?.Invoke(this, args);
            }

            void UpdateProgress(int bytesUploaded)
            {
                var lastBytes = upload.BytesTransferred;
                upload.UpdateProgress(bytesUploaded);
                var eventArgs = new TransferProgressUpdatedEventArgs(lastBytes, upload);
                options.ProgressUpdated?.Invoke(eventArgs);
                TransferProgressUpdated?.Invoke(this, eventArgs);
            }

            // fetch (or create) the semaphore for this user. the official client can't handle concurrent downloads, so we need to
            // enforce this regardless of what downstream implementations do.
            var semaphore = Uploads.GetOrAdd(username, new SemaphoreSlim(1, 1));

            UpdateState(TransferStates.Queued);
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                // in case the Upload record was removed via cleanup while we were waiting, add it back.
                semaphore = Uploads.AddOrUpdate(username, semaphore, (k, v) => semaphore);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);
                var messageConnection = await PeerConnectionManager
                    .GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken)
                    .ConfigureAwait(false);

                // prepare a wait for the transfer response
                var transferRequestAcknowledged = Waiter.Wait<TransferResponse>(
                    new WaitKey(MessageCode.Peer.TransferResponse, upload.Username, upload.Token));

                // request to start the upload
                var transferRequest = new TransferRequest(TransferDirection.Upload, upload.Token, upload.Filename, data.Length);
                await messageConnection.WriteAsync(transferRequest.ToByteArray(), cancellationToken).ConfigureAwait(false);
                UpdateState(TransferStates.Requested);

                var transferRequestAcknowledgement = await transferRequestAcknowledged.ConfigureAwait(false);

                if (!transferRequestAcknowledgement.Allowed)
                {
                    throw new TransferRejectedException(transferRequestAcknowledgement.Message);
                }

                UpdateState(TransferStates.Initializing);

                var uploadCompleted = Waiter.WaitIndefinitely(upload.WaitKey, cancellationToken);

                upload.Connection = await PeerConnectionManager
                    .GetTransferConnectionAsync(upload.Username, address.IPAddress, address.Port, upload.Token, cancellationToken)
                    .ConfigureAwait(false);

                upload.Connection.DataWritten += (sender, e) => UpdateProgress(e.CurrentLength);
                upload.Connection.Disconnected += (sender, message) =>
                {
                    if (upload.State.HasFlag(TransferStates.Succeeded))
                    {
                        Waiter.Complete(upload.WaitKey);
                    }
                    else if (upload.State.HasFlag(TransferStates.TimedOut))
                    {
                        Waiter.Throw(upload.WaitKey, new TimeoutException(message));
                    }
                    else
                    {
                        Waiter.Throw(upload.WaitKey, new ConnectionException($"Transfer failed: {message}"));
                    }
                };

                try
                {
                    // read the 8 magic bytes. not sure why.
                    await upload.Connection.ReadAsync(8).ConfigureAwait(false);

                    UpdateState(TransferStates.InProgress);

                    await upload.Connection.WriteAsync(data).ConfigureAwait(false);

                    upload.State = TransferStates.Succeeded;

                    // force a disconnect of the connection by trying to read. this may be unreliable if a client actually sends
                    // data after the magic bytes.
                    try
                    {
                        await upload.Connection.ReadAsync(1, cancellationToken).ConfigureAwait(false);
                    }
                    catch (ConnectionReadException ex) when (ex.InnerException is ConnectionException && ex.InnerException.Message == "Remote connection closed.")
                    {
                        // swallow this specific exception
                    }

                    Diagnostic.Info($"Upload of {System.IO.Path.GetFileName(upload.Filename)} from {username} complete ({upload.Data.Length} of {upload.Size} bytes).");
                }
                catch (TimeoutException)
                {
                    upload.State = TransferStates.TimedOut;
                    upload.Connection.Disconnect($"Transfer timed out after {Options.TransferConnectionOptions.InactivityTimeout} seconds of inactivity.");
                }
                catch (Exception ex)
                {
                    upload.Connection.Disconnect(ex.Message);
                }

                await uploadCompleted.ConfigureAwait(false);
            }
            catch (TransferRejectedException ex)
            {
                upload.State = TransferStates.Rejected;
                upload.Connection?.Disconnect("Transfer rejected.");

                throw new TransferException($"Upload of file {filename} rejected by user {username}: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                upload.State = TransferStates.Cancelled;
                upload.Connection?.Disconnect("Transfer cancelled.");

                throw new TransferException($"Upload of file {filename} to user {username} was cancelled.", ex);
            }
            catch (TimeoutException ex)
            {
                upload.State = TransferStates.TimedOut;
                upload.Connection?.Disconnect("Transfer timed out.");

                Diagnostic.Debug(ex.ToString());
                throw new TransferException($"Failed to upload file {filename} to user {username}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                upload.State = TransferStates.Errored;
                upload.Connection?.Disconnect("Transfer error.");

                Diagnostic.Debug(ex.ToString());
                throw new TransferException($"Failed to upload file {filename} to user {username}: {ex.Message}", ex);
            }
            finally
            {
                // clean up the wait in case the code threw before it was awaited.
                Waiter.Complete(upload.WaitKey);

                // release the semaphore and remove the record to prevent dangling records. the semaphore object is retained if
                // there are other threads waiting on it, and it is added back after the await above.
                semaphore.Release();
                Uploads.TryRemove(username, out var _);

                upload.Connection?.Dispose();

                upload.State = TransferStates.Completed | upload.State;
                UpdateState(upload.State);
            }
        }
    }
}