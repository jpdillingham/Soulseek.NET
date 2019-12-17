// <copyright file="SoulseekClient.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
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
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
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
        public SoulseekClient(SoulseekClientOptions options)
            : this(DefaultAddress, DefaultPort, options)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client options.</param>
        public SoulseekClient(string address = DefaultAddress, int port = DefaultPort, SoulseekClientOptions options = null)
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
        /// <param name="distributedConnectionManager">The IDistributedConnectionManager instance to use.</param>
        /// <param name="serverMessageHandler">The IServerMessageHandler instance to use.</param>
        /// <param name="peerMessageHandler">The IPeerMessageHandler instance to use.</param>
        /// <param name="listener">The IListener instance to use.</param>
        /// <param name="listenerHandler">The IListenerHandler instance to use.</param>
        /// <param name="waiter">The IWaiter instance to use.</param>
        /// <param name="tokenFactory">The ITokenFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        internal SoulseekClient(
            string address,
            int port,
            SoulseekClientOptions options = null,
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

            Options = options ?? new SoulseekClientOptions();

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

            ServerConnection.Connected += (sender, e) => ChangeState(SoulseekClientStates.Connected, $"Connected to {Address}:{Port}");
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
            ServerMessageHandler.RoomMessageReceived += (sender, e) => RoomMessageReceived?.Invoke(this, e);
            ServerMessageHandler.RoomJoined += (sender, e) => RoomJoined?.Invoke(this, e);
            ServerMessageHandler.RoomLeft += (sender, e) => RoomLeft?.Invoke(this, e);
            ServerMessageHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            ServerConnection.MessageRead += ServerMessageHandler.HandleMessage;
        }

        /// <summary>
        ///     Occurs when the client connects.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when the client disconnects.
        /// </summary>
        public event EventHandler<SoulseekClientDisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     Occurs when the client is logged in.
        /// </summary>
        public event EventHandler LoggedIn;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessageEventArgs> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when a user joins a chat room.
        /// </summary>
        public event EventHandler<RoomJoinedEventArgs> RoomJoined;

        /// <summary>
        ///     Occurs when a user leaves a chat room.
        /// </summary>
        public event EventHandler<RoomLeftEventArgs> RoomLeft;

        /// <summary>
        ///     Occurs when a chat room message is received.
        /// </summary>
        public event EventHandler<RoomMessageEventArgs> RoomMessageReceived;

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
        /// <remarks>Add a user to the server watch list with <see cref="AddUserAsync(string, CancellationToken?)"/>.</remarks>
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
        public virtual SoulseekClientOptions Options { get; }

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
        public virtual string Username { get; private set; }

#pragma warning disable SA1600 // Elements should be documented
        internal virtual IDistributedConnectionManager DistributedConnectionManager { get; }
        internal virtual IDistributedMessageHandler DistributedMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, TransferInternal> Downloads { get; set; } = new ConcurrentDictionary<int, TransferInternal>();
        internal virtual IListener Listener { get; }
        internal virtual IListenerHandler ListenerHandler { get; }
        internal virtual IPeerConnectionManager PeerConnectionManager { get; }
        internal virtual IPeerMessageHandler PeerMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, SearchInternal> Searches { get; set; } = new ConcurrentDictionary<int, SearchInternal>();
        internal virtual IMessageConnection ServerConnection { get; }
        internal virtual IServerMessageHandler ServerMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, TransferInternal> Uploads { get; set; } = new ConcurrentDictionary<int, TransferInternal>();
        internal virtual IWaiter Waiter { get; }
#pragma warning restore SA1600 // Elements should be documented

        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; } = false;
        private ITokenFactory TokenFactory { get; }
        private ConcurrentDictionary<string, SemaphoreSlim> UploadSemaphores { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        ///     Asynchronously sends a private message acknowledgement for the specified <paramref name="privateMessageId"/>.
        /// </summary>
        /// <param name="privateMessageId">The unique id of the private message to acknowledge.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="privateMessageId"/> is less than zero.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        public virtual Task AcknowledgePrivateMessageAsync(int privateMessageId, CancellationToken? cancellationToken = null)
        {
            if (privateMessageId < 0)
            {
                throw new ArgumentException($"The private message ID must be greater than zero.", nameof(privateMessageId));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to acknowledge private messages (currently: {State})");
            }

            return AcknowledgePrivateMessageInternalAsync(privateMessageId, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="username"/> to the server watch list for the current session.
        /// </summary>
        /// <remarks>
        ///     Once a user is added the server will begin sending status updates for that user, which will generate
        ///     <see cref="UserStatusChanged"/> events.
        /// </remarks>
        /// <param name="username">The username of the user to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserNotFoundException">Thrown when the specified user is not registered.</exception>
        /// <exception cref="AddUserException">Thrown when an exception is encountered during the operation.</exception>
        public Task<UserData> AddUserAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
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
        /// <remarks>
        ///     By default, this operation will not time out locally, but rather will wait until the remote connection is broken.
        ///     If a local timeout is desired, specify an appropriate <see cref="CancellationToken"/>.
        /// </remarks>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the fetched list of files.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="BrowseException">Thrown when an exception is encountered during the operation.</exception>
        public Task<IReadOnlyCollection<Directory>> BrowseAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to browse (currently: {State})");
            }

            return BrowseInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="ConnectionException">Thrown when an exception is encountered during the operation.</exception>
        public async Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            if (State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The client is already connected");
            }

            try
            {
                Listener?.Start();

                await ServerConnection.ConnectAsync(cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !(ex is OperationCanceledException))
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
            Disconnect(message, null);
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
        /// <returns>The Task representing the asynchronous operation, including a byte array containing the file contents.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        public Task<byte[]> DownloadAsync(string username, string filename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace", nameof(filename));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to download files (currently: {State})");
            }

            token = token ?? GetNextToken();

            if (Uploads.ContainsKey(token.Value) || Downloads.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (Downloads.Values.Any(d => d.Username == username && d.Filename == filename))
            {
                throw new DuplicateTransferException($"An active or queued download of {filename} from {username} is already in progress");
            }

            options = options ?? new TransferOptions();

            return DownloadToByteArrayAsync(username, filename, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously downloads the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>
        ///     to the specified <paramref name="outputStream"/>.
        /// </summary>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="filename">The file to download.</param>
        /// <param name="outputStream">The stream to which to write the file contents.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including a byte array containing the file contents.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="outputStream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the specified <paramref name="outputStream"/> is not writeable.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        public Task DownloadAsync(string username, string filename, Stream outputStream, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace", nameof(filename));
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream), "The specified output stream is null.");
            }

            if (!outputStream.CanWrite)
            {
                throw new InvalidOperationException("The specified output stream is not writeable.");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to download files (currently: {State})");
            }

            token = token ?? GetNextToken();

            if (Uploads.ContainsKey(token.Value) || Downloads.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (Downloads.Values.Any(d => d.Username == username && d.Filename == filename))
            {
                throw new DuplicateTransferException($"An active or queued download of {filename} from {username} is already in progress");
            }

            options = options ?? new TransferOptions();

            return DownloadToStreamAsync(username, filename, outputStream, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously fetches the current place of the specified <paramref name="filename"/> in the queue of the
        ///     specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user whose queue to check.</param>
        /// <param name="filename">The file to check.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the current place of the file in the queue.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="TransferNotFoundException">Thrown when a corresponding download is not active.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="DownloadPlaceInQueueException">Thrown when an exception is encountered during the operation.</exception>
        public Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace", nameof(filename));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be Connected and LoggedIn to check download queue position (currently: {State})");
            }

            if (!Downloads.Any(d => d.Value.Username == username && d.Value.Filename == filename))
            {
                throw new TransferNotFoundException($"A download of {filename} from user {username} is not active");
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
        public virtual int GetNextToken() => TokenFactory.NextToken();

        /// <summary>
        ///     Asynchronously fetches the list of chat rooms from the server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the list of server rooms.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomListException">Thrown when an exception is encountered during the operation.</exception>
        public async Task<IReadOnlyCollection<Room>> GetRoomListAsync(CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch the list of chat rooms (currently: {State})");
            }

            try
            {
                var roomListWait = Waiter.Wait<IReadOnlyCollection<Room>>(new WaitKey(MessageCode.Server.RoomList), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new RoomListRequest().ToByteArray(), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

                var response = await roomListWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new RoomListException($"Failed to fetch the list of chat rooms from the server: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously fetches the IP address and port of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the connection information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the connection information.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="UserAddressException">Thrown when an exception is encountered during the operation.</exception>
        public virtual Task<(IPAddress IPAddress, int Port)> GetUserAddressAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
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
        /// <returns>The Task representing the asynchronous operation, including the information response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="UserInfoException">Thrown when an exception is encountered during the operation.</exception>
        public Task<UserInfo> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
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
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="UserStatusException">Thrown when an exception is encountered during the operation.</exception>
        public Task<(UserStatus Status, bool IsPrivileged)> GetUserStatusAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user status (currently: {State})");
            }

            return GetUserStatusInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously joins the chat room with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <remarks>When successful, a corresponding <see cref="RoomJoined"/> event will be raised.</remarks>
        /// <param name="roomName">The name of the chat room to join.</param>
        /// <param name="cancellationToken">The token to minotor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomJoinException">Thrown when an exception is encountered during the operation.</exception>
        public Task<RoomData> JoinRoomAsync(string roomName, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException($"The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to join a chat room (currently: {State})");
            }

            return JoinRoomInternalAsync(roomName, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously leaves the chat room with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The name of the chat room to leave.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomJoinException">Thrown when an exception is encountered during the operation.</exception>
        public Task LeaveRoomAsync(string roomName, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException($"The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to leave a chat room (currently: {State})");
            }

            return LeaveRoomInternalAsync(roomName, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a user is already logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="LoginRejectedException">Thrown when the login is rejected by the remote server.</exception>
        /// <exception cref="LoginException">Thrown when an exception is encountered during the operation.</exception>
        public Task LoginAsync(string username, string password, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Username may not be null or an empty string", nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password may not be null or an empty string", nameof(password));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The client must be connected to log in");
            }

            if (State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"Already logged in as {Username}.  Disconnect before logging in again");
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
        /// <returns>The Task representing the asynchronous operation, including the search results.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="searchText"/> is null, empty, or consists of only whitespace.
        /// </exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SearchException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        public Task<IReadOnlyCollection<SearchResponse>> SearchAsync(string searchText, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException($"Search text must not be a null or empty string, or one consisting only of whitespace", nameof(searchText));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to perform a search (currently: {State})");
            }

            token = token ?? TokenFactory.NextToken();

            if (Searches.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"An active search with token {token.Value} is already in progress");
            }

            options = options ?? new SearchOptions();

            return SearchToCollectionAsync(searchText, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="searchText"/> using the specified unique
        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="responseReceived">The delegate to invoke for each response.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the search results.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="searchText"/> is null, empty, or consists of only whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the specified <paramref name="responseReceived"/> delegate is null.
        /// </exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SearchException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        public Task SearchAsync(string searchText, Action<SearchResponse> responseReceived, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException($"Search text must not be a null or empty string, or one consisting only of whitespace", nameof(searchText));
            }

            if (responseReceived == default)
            {
                throw new ArgumentNullException(nameof(responseReceived), "The specified Response delegate is null.");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to perform a search (currently: {State})");
            }

            token = token ?? TokenFactory.NextToken();

            if (Searches.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"An active search with token {token.Value} is already in progress");
            }

            options = options ?? new SearchOptions();

            return SearchToCallbackAsync(searchText, responseReceived, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously sends the specified private <paramref name="message"/> to the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="message"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        public Task SendPrivateMessageAsync(string username, string message, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException($"The message must not be a null or empty string, or one consisting only of whitespace", nameof(message));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to send a private message (currently: {State})");
            }

            return SendPrivateMessageInternalAsync(username, message, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously sends the specified chat room <paramref name="message"/> to the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The name of the room to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="message"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomMessageException">Thrown when an exception is encountered during the operation.</exception>
        public Task SendRoomMessageAsync(string roomName, string message, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException($"The room name must not be a null or empty string, or one consisting only of whitespace", nameof(roomName));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException($"The message must not be a null or empty string, or one consisting only of whitespace", nameof(message));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to send a private message (currently: {State})");
            }

            return SendRoomMessageInternalAsync(roomName, message, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously informs the server of the number of shared <paramref name="directories"/> and <paramref name="files"/>.
        /// </summary>
        /// <param name="directories">The number of shared directories.</param>
        /// <param name="files">The number of shared files.</param>
        /// <param name="cancellationToken">The token to monitor for cancelation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the value of <paramref name="directories"/> or <paramref name="files"/> is less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SharedCountsException">Thrown when an exception is encountered during the operation.</exception>
        public Task SetSharedCountsAsync(int directories, int files, CancellationToken? cancellationToken = null)
        {
            if (directories < 0)
            {
                throw new ArgumentException($"The directory count must be equal to or greater than zero", nameof(directories));
            }

            if (files < 0)
            {
                throw new ArgumentException($"The file count must be equal to or greater than zero", nameof(files));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set shared counts (currently: {State})");
            }

            try
            {
                return ServerConnection.WriteAsync(new SetSharedCounts(directories, files).ToByteArray(), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SharedCountsException($"Failed to set shared counts to {directories} directories and {files} files: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously informs the server of the current online <paramref name="status"/> of the client.
        /// </summary>
        /// <param name="status">The current status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserStatusException">Thrown when an exception is encountered during the operation.</exception>
        public Task SetUserStatusAsync(UserStatus status, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set online status (currently: {State})");
            }

            try
            {
                return ServerConnection.WriteAsync(new SetOnlineStatus(status).ToByteArray(), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new UserStatusException($"Failed to set user status to {status}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously uploads the specified <paramref name="filename"/> containing <paramref name="data"/> to the the
        ///     specified <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="filename">The filename of the file to upload.</param>
        /// <param name="data">The file contents.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="data"/> is null or of zero length.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="filename"/> to the specified <paramref name="username"/> is
        ///     already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        public Task UploadAsync(string username, string filename, byte[] data, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace", nameof(filename));
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException($"The data must not be a null or zero length array.", nameof(data));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to upload files (currently: {State})");
            }

            token = token ?? GetNextToken();

            if (Uploads.ContainsKey(token.Value) || Downloads.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (Uploads.Values.Any(d => d.Username == username && d.Filename == filename))
            {
                throw new DuplicateTransferException($"An active or queued upload of {filename} to {username} is already in progress");
            }

            options = options ?? new TransferOptions();

            return UploadFromByteArrayAsync(username, filename, data, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously uploads the specified <paramref name="filename"/> from the specified <paramref name="inputStream"/>
        ///     to the the specified <paramref name="username"/> using the specified unique <paramref name="token"/> and
        ///     optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="filename">The filename of the file to upload.</param>
        /// <param name="length">The size of the file to upload, in bytes.</param>
        /// <param name="inputStream">The stream from which to retrieve the file contents.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="length"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="inputStream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the specified <paramref name="inputStream"/> is not readable.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="filename"/> to the specified <paramref name="username"/> is
        ///     already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        public Task UploadAsync(string username, string filename, long length, Stream inputStream, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace", nameof(filename));
            }

            if (length <= 0)
            {
                throw new ArgumentException("The requested length must be greater than or equal to zero.", nameof(length));
            }

            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream), "The specified input stream is null.");
            }

            if (!inputStream.CanRead)
            {
                throw new InvalidOperationException("The specified input stream is not readable.");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to upload files (currently: {State})");
            }

            token = token ?? GetNextToken();

            if (Uploads.ContainsKey(token.Value) || Downloads.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (Uploads.Values.Any(d => d.Username == username && d.Filename == filename))
            {
                throw new DuplicateTransferException($"An active or queued upload of {filename} to {username} is already in progress");
            }

            options = options ?? new TransferOptions();

            return UploadFromStreamAsync(username, filename, length, inputStream, token.Value, options, cancellationToken ?? CancellationToken.None);
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
                    Disconnect("Client is being disposed", new ObjectDisposedException(GetType().Name));
                    PeerConnectionManager?.Dispose();
                    DistributedConnectionManager?.Dispose();
                    Waiter?.Dispose();
                    ServerConnection?.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task AcknowledgePrivateMessageInternalAsync(int privateMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new AcknowledgePrivateMessageCommand(privateMessageId).ToByteArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new PrivateMessageException($"Failed to acknowledge private message with ID {privateMessageId}: {ex.Message}", ex);
            }
        }

        private async Task<UserData> AddUserInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var addUserWait = Waiter.Wait<AddUserResponse>(new WaitKey(MessageCode.Server.AddUser, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new AddUserRequest(username).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await addUserWait.ConfigureAwait(false);

                if (!response.Exists)
                {
                    throw new UserNotFoundException($"User {Username} does not exist.");
                }

                return response.UserData;
            }
            catch (Exception ex) when (!(ex is UserNotFoundException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new AddUserException($"Failed to retrieve information for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task<IReadOnlyCollection<Directory>> BrowseInternalAsync(string username, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.BrowseResponse, username);
                var browseWait = Waiter.WaitIndefinitely<BrowseResponse>(waitKey, cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                connection.Disconnected += (sender, args) =>
                {
                    Waiter.Throw(waitKey, args.Exception ?? new ConnectionException($"Peer connection disconnected unexpectedly: {args.Message}"));
                };

                connection.PreventInactivityTimeout(true);

                var sw = new System.Diagnostics.Stopwatch();
                Diagnostic.Debug($"Sending browse request to peer {username}");
                sw.Start();

                await connection.WriteAsync(new BrowseRequest().ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await browseWait.ConfigureAwait(false);

                sw.Stop();
                Diagnostic.Debug($"Browse of {username} completed in {sw.ElapsedMilliseconds}ms.  {response.DirectoryCount} directories fetched");

                return response.Directories;
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new BrowseException($"Failed to browse user {username}: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    connection?.PreventInactivityTimeout(false);
                }
                catch (ObjectDisposedException)
                {
                    // swallow; inactivity is irrelevant
                }
            }
        }

        private void ChangeState(SoulseekClientStates state, string message, Exception exception = null)
        {
            var previousState = State;
            State = state;

            StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(previousState, State, message, exception));

            if (State == SoulseekClientStates.Connected)
            {
                Connected?.Invoke(this, EventArgs.Empty);
            }
            else if (State == (SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn))
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
            else if (State == SoulseekClientStates.Disconnected)
            {
                Disconnected?.Invoke(this, new SoulseekClientDisconnectedEventArgs(message, exception));
            }
        }

        private void Disconnect(string message, Exception exception = null)
        {
            if (State != SoulseekClientStates.Disconnected)
            {
                message = message ?? exception?.Message ?? "Client disconnected";

                ServerConnection.Disconnected -= ServerConnection_Disconnected;
                ServerConnection?.Disconnect(message, exception);

                Listener?.Stop();

                UploadSemaphores?.RemoveAndDisposeAll();

                PeerConnectionManager?.RemoveAndDisposeAll();
                DistributedConnectionManager?.RemoveAndDisposeAll();

                Searches?.RemoveAndDisposeAll();
                Downloads?.RemoveAll();
                Uploads?.RemoveAll();

                Waiter?.CancelAll();

                Username = null;

                ChangeState(SoulseekClientStates.Disconnected, message, exception);
            }
        }

        private async Task<byte[]> DownloadToByteArrayAsync(string username, string filename, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            // overwrite provided options to ensure the stream disposal flags are false; this will prevent the enclosing memory
            // stream from capturing the output.
            options = new TransferOptions(
                options.Governor,
                options.StateChanged,
                options.ProgressUpdated,
                disposeInputStreamOnCompletion: false,
                disposeOutputStreamOnCompletion: false);

            using (var memoryStream = new MemoryStream())
            {
                await DownloadToStreamAsync(username, filename, memoryStream, token, options, cancellationToken).ConfigureAwait(false);
                return memoryStream.ToArray();
            }
        }

        private async Task DownloadToStreamAsync(string username, string filename, Stream outputStream, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            var download = new TransferInternal(TransferDirection.Download, username, filename, token, options);
            Downloads.TryAdd(download.Token, download);

            Task downloadCompleted = null;
            var lastState = TransferStates.None;

            void UpdateState(TransferStates state)
            {
                download.State = state;
                var args = new TransferStateChangedEventArgs(previousState: lastState, transfer: new Transfer(download));
                lastState = state;
                options.StateChanged?.Invoke(args);
                TransferStateChanged?.Invoke(this, args);
            }

            void UpdateProgress(long bytesDownloaded)
            {
                var lastBytes = download.BytesTransferred;
                download.UpdateProgress(bytesDownloaded);
                var eventArgs = new TransferProgressUpdatedEventArgs(lastBytes, new Transfer(download));
                options.ProgressUpdated?.Invoke(eventArgs);
                TransferProgressUpdated?.Invoke(this, eventArgs);
            }

            try
            {
                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);
                var peerConnection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);

                // prepare two waits; one for the transfer response to confirm that our request is acknowledged and another for
                // the eventual transfer request sent when the peer is ready to send the file. the response message should be
                // returned immediately, while the request will be sent only when we've reached the front of the remote queue.
                var transferRequestAcknowledged = Waiter.Wait<TransferResponse>(
                    new WaitKey(MessageCode.Peer.TransferResponse, download.Username, download.Token), null, cancellationToken);
                var transferStartRequested = Waiter.WaitIndefinitely<TransferRequest>(
                    new WaitKey(MessageCode.Peer.TransferRequest, download.Username, download.Filename), cancellationToken);

                // request the file
                await peerConnection.WriteAsync(new TransferRequest(TransferDirection.Download, token, filename).ToByteArray(), cancellationToken).ConfigureAwait(false);
                UpdateState(TransferStates.Requested);

                var transferRequestAcknowledgement = await transferRequestAcknowledged.ConfigureAwait(false);

                if (transferRequestAcknowledgement.IsAllowed)
                {
                    // the peer is ready to initiate the transfer immediately; we are bypassing their queue. note that only the
                    // legacy client operates this way; SoulseekQt always returns Allowed = false regardless of the current queue.
                    UpdateState(TransferStates.Initializing);

                    download.Size = transferRequestAcknowledgement.FileSize;

                    // prepare a wait for the overall completion of the download
                    downloadCompleted = Waiter.WaitIndefinitely(download.WaitKey, cancellationToken);

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
                    // the download is remotely queued, so put it in the local queue.
                    UpdateState(TransferStates.Queued);

                    // wait for the peer to respond that they are ready to start the transfer
                    var transferStartRequest = await transferStartRequested.ConfigureAwait(false);

                    download.Size = transferStartRequest.FileSize;
                    download.RemoteToken = transferStartRequest.Token;
                    UpdateState(TransferStates.Initializing);

                    // set up a wait for an indirect connection. this wait is completed in ServerMessageHandler upon receipt of a ConnectToPeerResponse.
                    var indirectTransferConnectionInitialized = Waiter.Wait<IConnection>(
                        key: new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken),
                        timeout: Options.PeerConnectionOptions.ConnectTimeout,
                        cancellationToken: cancellationToken);

                    // set up a wait for a direct connection. this wait is completed in
                    // PeerConnectionManager.AddTransferConnectionAsync when handling the incoming connection within ListenerHandler.
                    var directTransferConnectionInitialized = Waiter.Wait<IConnection>(
                        key: new WaitKey(Constants.WaitKey.DirectTransfer, download.Username, download.RemoteToken),
                        timeout: Options.PeerConnectionOptions.ConnectTimeout,
                        cancellationToken: cancellationToken);

                    // also prepare a wait for the overall completion of the download
                    downloadCompleted = Waiter.WaitIndefinitely(download.WaitKey, cancellationToken);

                    // respond to the peer that we are ready to accept the file but first, get a fresh connection (or maybe it's
                    // cached in the manager) to the peer in case it disconnected and was purged while we were waiting.
                    peerConnection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);

                    await peerConnection.WriteAsync(new TransferResponse(download.RemoteToken.Value, download.Size).ToByteArray(), cancellationToken).ConfigureAwait(false);

                    try
                    {
                        var connectionInitialized = await Task.WhenAny(indirectTransferConnectionInitialized, directTransferConnectionInitialized).ConfigureAwait(false);
                        download.Connection = connectionInitialized.Result;
                    }
                    catch (AggregateException ex)
                    {
                        throw new ConnectionException("Failed to establish a direct or indirect connection.", ex);
                    }
                }

                download.Connection.DataRead += (sender, e) => UpdateProgress(e.CurrentLength);
                download.Connection.Disconnected += (sender, e) =>
                {
                    if (download.State.HasFlag(TransferStates.Succeeded))
                    {
                        Waiter.Complete(download.WaitKey);
                    }
                    else if (e.Exception is TimeoutException)
                    {
                        download.State = TransferStates.TimedOut;
                        Waiter.Throw(download.WaitKey, e.Exception);
                    }
                    else if (e.Exception is OperationCanceledException)
                    {
                        download.State = TransferStates.Cancelled;
                        Waiter.Throw(download.WaitKey, e.Exception);
                    }
                    else
                    {
                        Waiter.Throw(download.WaitKey, new ConnectionException($"Transfer failed: {e.Message}", e.Exception));
                    }
                };

                try
                {
                    // this needs to be 16? bytes for transfers beginning immediately, or 8 for queued. not sure what this is; it
                    // was identified via WireShark.
                    await download.Connection.WriteAsync(new byte[8], cancellationToken).ConfigureAwait(false);

                    UpdateState(TransferStates.InProgress);

                    await download.Connection.ReadAsync(download.Size, outputStream, (cancelToken) => options.Governor(new Transfer(download), cancelToken), cancellationToken).ConfigureAwait(false);

                    download.State = TransferStates.Succeeded;

                    download.Connection.Disconnect("Transfer complete.");
                    Diagnostic.Info($"Download of {Path.GetFileName(download.Filename)} from {username} complete ({outputStream.Position} of {download.Size} bytes).");
                }
                catch (Exception ex)
                {
                    download.Connection.Disconnect(exception: ex);
                }

                // wait for the download to complete this wait is either completed (on success) or thrown (on anything other than
                // success) in the Disconnected event handler of the transfer connection
                await downloadCompleted.ConfigureAwait(false);
            }
            catch (TransferRejectedException ex)
            {
                download.State = TransferStates.Rejected;
                download.Connection?.Disconnect("Transfer rejected.", ex);

                throw new TransferException($"Download of file {filename} rejected by user {username}: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                download.State = TransferStates.Cancelled;
                download.Connection?.Disconnect("Transfer cancelled.", ex);

                Diagnostic.Debug(ex.ToString());
                throw;
            }
            catch (TimeoutException ex)
            {
                download.State = TransferStates.TimedOut;
                download.Connection?.Disconnect("Transfer timed out.", ex);

                Diagnostic.Debug(ex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                download.State = TransferStates.Errored;
                download.Connection?.Disconnect("Transfer error.", ex);

                Diagnostic.Debug(ex.ToString());

                if (ex is UserOfflineException)
                {
                    throw;
                }

                throw new TransferException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
            finally
            {
                // clean up the wait in case the code threw before it was awaited.
                Waiter.Complete(download.WaitKey);

                download.Connection?.Dispose();

                // change state so we can fire the progress update a final time with the updated state little bit of a hack to
                // avoid cloning the download
                download.State = TransferStates.Completed | download.State;
                UpdateProgress((int)outputStream.Position);
                UpdateState(download.State);

                if (options.DisposeOutputStreamOnCompletion)
                {
                    await outputStream.FlushAsync().ConfigureAwait(false);
                    outputStream.Dispose();
                }

                Downloads.TryRemove(download.Token, out _);
            }
        }

        private async Task<int> GetDownloadPlaceInQueueInternalAsync(string username, string filename, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.PlaceInQueueResponse, username, filename);
                var responseWait = Waiter.Wait<PlaceInQueueResponse>(waitKey, null, cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);
                var connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                await connection.WriteAsync(new PlaceInQueueRequest(filename).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await responseWait.ConfigureAwait(false);

                return response.PlaceInQueue;
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new DownloadPlaceInQueueException($"Failed to fetch place in queue for download of {filename} from {username}: {ex.Message}", ex);
            }
        }

        private async Task<(IPAddress IPAddress, int Port)> GetUserAddressInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.GetPeerAddress, username);
                var addressWait = Waiter.Wait<UserAddressResponse>(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new UserAddressRequest(username).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await addressWait.ConfigureAwait(false);

                if (response.IPAddress.Equals(IPAddress.Parse("0.0.0.0")))
                {
                    throw new UserOfflineException($"User {username} appears to be offline.");
                }

                return (response.IPAddress, response.Port);
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new UserAddressException($"Failed to retrieve address for user {username}: {ex.Message}", ex);
            }
        }

        private async Task<UserInfo> GetUserInfoInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.InfoResponse, username);
                var infoWait = Waiter.Wait<UserInfo>(waitKey, cancellationToken: cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                var connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                await connection.WriteAsync(new UserInfoRequest().ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await infoWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new UserInfoException($"Failed to retrieve information for user {username}: {ex.Message}", ex);
            }
        }

        private async Task<(UserStatus Status, bool IsPrivileged)> GetUserStatusInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var getStatusWait = Waiter.Wait<UserStatusResponse>(new WaitKey(MessageCode.Server.GetStatus, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new UserStatusRequest(username).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await getStatusWait.ConfigureAwait(false);

                return (response.Status, response.IsPrivileged);
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new UserStatusException($"Failed to retrieve status for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task<RoomData> JoinRoomInternalAsync(string roomName, CancellationToken cancellationToken)
        {
            try
            {
                var joinRoomWait = Waiter.Wait<RoomData>(new WaitKey(MessageCode.Server.JoinRoom, roomName), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new JoinRoomRequest(roomName).ToByteArray(), cancellationToken).ConfigureAwait(false);

                var response = await joinRoomWait.ConfigureAwait(false);
                return response;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new RoomJoinException($"Failed to join chat room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task LeaveRoomInternalAsync(string roomName, CancellationToken cancellationToken)
        {
            try
            {
                var leaveRoomWait = Waiter.Wait(new WaitKey(MessageCode.Server.LeaveRoom, roomName), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new LeaveRoomRequest(roomName).ToByteArray(), cancellationToken).ConfigureAwait(false);

                await leaveRoomWait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new RoomLeaveException($"Failed to leave chat room {roomName}: {ex.Message}", ex);
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
                    ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, "Logged in.");

                    if (Options.ListenPort.HasValue)
                    {
                        // the client sends an undocumented message in the format 02/listen port/01/obfuscated port we don't
                        // support obfuscation, so we send only the listen port. it probably wouldn't hurt to send an 00 afterwards.
                        await ServerConnection.WriteAsync(new SetListenPort(Options.ListenPort.Value).ToByteArray(), cancellationToken).ConfigureAwait(false);
                    }

                    await ServerConnection.WriteAsync(new HaveNoParents(true).ToByteArray(), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var ex = new LoginRejectedException($"The server rejected login attempt: {response.Message}");
                    Disconnect(ex.Message, exception: ex); // upon login failure the server will refuse to allow any more input, eventually disconnecting.
                    throw ex;
                }
            }
            catch (Exception ex) when (!(ex is LoginRejectedException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new LoginException($"Failed to log in as {username}: {ex.Message}", ex);
            }
        }

        private async Task SearchToCallbackAsync(string searchText, Action<SearchResponse> responseReceived, int token, SearchOptions options, CancellationToken cancellationToken)
        {
            var search = new SearchInternal(searchText, token, options);
            var lastState = SearchStates.None;

            void UpdateState(SearchStates state)
            {
                search.State = state;
                var args = new SearchStateChangedEventArgs(previousState: lastState, search: new Search(search));
                lastState = state;
                options.StateChanged?.Invoke(args);
                SearchStateChanged?.Invoke(this, args);
            }

            try
            {
                search.ResponseReceived = (response) =>
                {
                    responseReceived(response);

                    var eventArgs = new SearchResponseReceivedEventArgs(response, new Search(search));
                    options.ResponseReceived?.Invoke(eventArgs);
                    SearchResponseReceived?.Invoke(this, eventArgs);
                };

                Searches.TryAdd(search.Token, search);
                UpdateState(SearchStates.Requested);

                await ServerConnection.WriteAsync(new SearchRequest(search.SearchText, search.Token).ToByteArray(), cancellationToken).ConfigureAwait(false);
                UpdateState(SearchStates.InProgress);

                await search.WaitForCompletion(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                search.Complete(SearchStates.Cancelled);
                throw;
            }
            catch (TimeoutException)
            {
                search.Complete(SearchStates.Errored);
                throw;
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

        private async Task<IReadOnlyCollection<SearchResponse>> SearchToCollectionAsync(string searchText, int token, SearchOptions options, CancellationToken cancellationToken)
        {
            var responseBag = new ConcurrentBag<SearchResponse>();

            void ResponseReceived(SearchResponse response)
            {
                responseBag.Add(response);
            }

            await SearchToCallbackAsync(searchText, ResponseReceived, token, options, cancellationToken).ConfigureAwait(false);
            return responseBag.ToList().AsReadOnly();
        }

        private async Task SendPrivateMessageInternalAsync(string username, string message, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new PrivateMessageCommand(username, message).ToByteArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new PrivateMessageException($"Failed to send private message to user {username}: {ex.Message}", ex);
            }
        }

        private async Task SendRoomMessageInternalAsync(string roomName, string message, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new RoomMessageCommand(roomName, message).ToByteArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new RoomMessageException($"Failed to send message to room {roomName}: {ex.Message}", ex);
            }
        }

        private void ServerConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            Disconnect(e.Message, e.Exception);
        }

        private async Task UploadFromByteArrayAsync(string username, string filename, byte[] data, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            // overwrite provided options to ensure the stream disposal flags are false; this will prevent the enclosing memory
            // stream from capturing the output.
            options = new TransferOptions(
                options.Governor,
                options.StateChanged,
                options.ProgressUpdated,
                disposeInputStreamOnCompletion: false,
                disposeOutputStreamOnCompletion: false);

            using (var memoryStream = new MemoryStream(data))
            {
                await UploadFromStreamAsync(username, filename, data.Length, memoryStream, token, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task UploadFromStreamAsync(string username, string filename, long length, Stream inputStream, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            var upload = new TransferInternal(TransferDirection.Upload, username, filename, token, options)
            {
                Size = length,
            };

            Uploads.TryAdd(upload.Token, upload);

            var lastState = TransferStates.None;

            void UpdateState(TransferStates state)
            {
                upload.State = state;
                var args = new TransferStateChangedEventArgs(previousState: lastState, transfer: new Transfer(upload));
                lastState = state;
                options.StateChanged?.Invoke(args);
                TransferStateChanged?.Invoke(this, args);
            }

            void UpdateProgress(long bytesUploaded)
            {
                var lastBytes = upload.BytesTransferred;
                upload.UpdateProgress(bytesUploaded);
                var eventArgs = new TransferProgressUpdatedEventArgs(lastBytes, new Transfer(upload));
                options.ProgressUpdated?.Invoke(eventArgs);
                TransferProgressUpdated?.Invoke(this, eventArgs);
            }

            // fetch (or create) the semaphore for this user. the official client can't handle concurrent downloads, so we need to
            // enforce this regardless of what downstream implementations do.
            var semaphore = UploadSemaphores.GetOrAdd(username, new SemaphoreSlim(1, 1));

            UpdateState(TransferStates.Queued);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            (IPAddress IPAddress, int Port) address = default;

            try
            {
                // in case the upload record was removed via cleanup while we were waiting, add it back.
                semaphore = UploadSemaphores.AddOrUpdate(username, semaphore, (k, v) => semaphore);

                address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);
                var messageConnection = await PeerConnectionManager
                    .GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken)
                    .ConfigureAwait(false);

                // prepare a wait for the transfer response
                var transferRequestAcknowledged = Waiter.Wait<TransferResponse>(
                    new WaitKey(MessageCode.Peer.TransferResponse, upload.Username, upload.Token), null, cancellationToken);

                // request to start the upload
                var transferRequest = new TransferRequest(TransferDirection.Upload, upload.Token, upload.Filename, length);
                await messageConnection.WriteAsync(transferRequest.ToByteArray(), cancellationToken).ConfigureAwait(false);
                UpdateState(TransferStates.Requested);

                var transferRequestAcknowledgement = await transferRequestAcknowledged.ConfigureAwait(false);

                if (!transferRequestAcknowledgement.IsAllowed)
                {
                    throw new TransferRejectedException(transferRequestAcknowledgement.Message);
                }

                UpdateState(TransferStates.Initializing);

                var uploadCompleted = Waiter.WaitIndefinitely(upload.WaitKey, cancellationToken);

                upload.Connection = await PeerConnectionManager
                    .GetTransferConnectionAsync(upload.Username, address.IPAddress, address.Port, upload.Token, cancellationToken)
                    .ConfigureAwait(false);

                upload.Connection.DataWritten += (sender, e) => UpdateProgress(e.CurrentLength);
                upload.Connection.Disconnected += (sender, e) =>
                {
                    if (upload.State.HasFlag(TransferStates.Succeeded))
                    {
                        Waiter.Complete(upload.WaitKey);
                    }
                    else if (e.Exception is TimeoutException)
                    {
                        upload.State = TransferStates.TimedOut;
                        Waiter.Throw(upload.WaitKey, e.Exception);
                    }
                    else if (e.Exception is OperationCanceledException)
                    {
                        upload.State = TransferStates.Cancelled;
                        Waiter.Throw(upload.WaitKey, e.Exception);
                    }
                    else
                    {
                        Waiter.Throw(upload.WaitKey, new ConnectionException($"Transfer failed: {e.Message}", e.Exception));
                    }
                };

                try
                {
                    // read the 8 magic bytes. not sure why.
                    await upload.Connection.ReadAsync(8, cancellationToken).ConfigureAwait(false);

                    UpdateState(TransferStates.InProgress);

                    await upload.Connection.WriteAsync(length, inputStream, (cancelToken) => options.Governor(new Transfer(upload), cancelToken), cancellationToken).ConfigureAwait(false);

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

                    Diagnostic.Info($"Upload of {Path.GetFileName(upload.Filename)} from {username} complete ({inputStream.Position} of {upload.Size} bytes).");
                }
                catch (Exception ex)
                {
                    upload.Connection.Disconnect(exception: ex);
                }

                await uploadCompleted.ConfigureAwait(false);
            }
            catch (TransferRejectedException ex)
            {
                upload.State = TransferStates.Rejected;
                upload.Connection?.Disconnect("Transfer rejected.", ex);

                throw new TransferException($"Upload of file {filename} rejected by user {username}: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                upload.State = TransferStates.Cancelled;
                upload.Connection?.Disconnect("Transfer cancelled.", ex);

                Diagnostic.Debug(ex.ToString());
                throw;
            }
            catch (TimeoutException ex)
            {
                upload.State = TransferStates.TimedOut;
                upload.Connection?.Disconnect("Transfer timed out.", ex);

                Diagnostic.Debug(ex.ToString());
                throw;
            }
            catch (Exception ex)
            {
                upload.State = TransferStates.Errored;
                upload.Connection?.Disconnect("Transfer error.", ex);

                Diagnostic.Debug(ex.ToString());

                if (ex is UserOfflineException)
                {
                    throw;
                }

                throw new TransferException($"Failed to upload file {filename} to user {username}: {ex.Message}", ex);
            }
            finally
            {
                // clean up the wait in case the code threw before it was awaited.
                Waiter.Complete(upload.WaitKey);

                // release the semaphore and remove the record to prevent dangling records. the semaphore object is retained if
                // there are other threads waiting on it, and it is added back after it is awaited above.
                semaphore.Release();
                UploadSemaphores.TryRemove(username, out var _);

                upload.Connection?.Dispose();

                upload.State = TransferStates.Completed | upload.State;
                UpdateProgress((int)inputStream.Position);
                UpdateState(upload.State);

                if (!upload.State.HasFlag(TransferStates.Succeeded))
                {
                    try
                    {
                        // if the upload failed, send a message to the user informing them.
                        var messageConnection = await PeerConnectionManager
                            .GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken)
                            .ConfigureAwait(false);

                        await messageConnection.WriteAsync(new UploadFailed(filename).ToByteArray()).ConfigureAwait(false);
                    }
                    catch
                    {
                        // swallow any exceptions here
                    }
                }

                if (options.DisposeInputStreamOnCompletion)
                {
                    inputStream.Dispose();
                }

                Uploads.TryRemove(upload.Token, out _);
            }
        }
    }
}