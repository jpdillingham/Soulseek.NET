// <copyright file="PeerConnectionManager.cs" company="JP Dillingham">
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

namespace Soulseek.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Manages peer <see cref="IConnection"/> instances for the application.
    /// </summary>
    internal sealed class PeerConnectionManager : IPeerConnectionManager
    {
        private int waitingMessageConnections = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerConnectionManager"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="connectionFactory">The IConnectionFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        internal PeerConnectionManager(
            ISoulseekClient soulseekClient,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = (SoulseekClient)soulseekClient;

            ConcurrentMessageConnectionLimit = SoulseekClient.Options.ConcurrentPeerMessageConnectionLimit;

            if (SoulseekClient.Listener != null)
            {
                SoulseekClient.Listener.Accepted += Listener_Accepted;
            }

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            MessageSemaphore = new SemaphoreSlim(ConcurrentMessageConnectionLimit, ConcurrentMessageConnectionLimit);
            MessageConnections = new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Gets the number of active peer message connections.
        /// </summary>
        public int ActiveMessageConnections => MessageConnections.Count;

        /// <summary>
        ///     Gets the number of allowed concurrent peer message connections.
        /// </summary>
        public int ConcurrentMessageConnectionLimit { get; }

        /// <summary>
        ///     Gets the number of waiting peer message connections.
        /// </summary>
        public int WaitingMessageConnections => waitingMessageConnections;

        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)> MessageConnections { get; set; }
        private SemaphoreSlim MessageSemaphore { get; }
        private ConcurrentDictionary<int, string> PendingSolicitations { get; set; } = new ConcurrentDictionary<int, string>();
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IPeerConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Returns an existing, or gets a new connection using the details in the specified
        ///     <paramref name="connectToPeerResponse"/> and pierces the remote peer's firewall.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context, including the new or updated connection.</returns>
        public async Task<IMessageConnection> GetOrAddMessageConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var key = new ConnectionKey(
                connectToPeerResponse.Username,
                connectToPeerResponse.IPAddress,
                connectToPeerResponse.Port);

            IMessageConnection connection = null;

            // get or add a connection. we only care about the semphore at this point, so discard the connection.
            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(key.Username).ConfigureAwait(false);

            // await the semaphore we got back to ensure exclusive access over the code that follows. this is important because
            // while the GetOrAdd above either gets or retrieves a connection in a thread safe manner (through
            // ConcurrentDictionary), the connection itself is not synchronized.
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                // retrieve the connection now that we have exclusive access to the record.
                (_, connection) = await GetOrAddMessageConnectionRecordAsync(key.Username).ConfigureAwait(false);

                // the connection is null when added, so if it is no longer null then it was either already established prior to
                // this method being invoked, or has been established by another thread between the first and second calls to
                // GetOrAddMessageConnectionAsync(). either way, return it as is.
                if (connection != null)
                {
                    return connection;
                }
                else
                {
                    connection = ConnectionFactory.GetMessageConnection(
                        connectToPeerResponse.Username,
                        connectToPeerResponse.IPAddress,
                        connectToPeerResponse.Port,
                        SoulseekClient.Options.PeerConnectionOptions);

                    connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
                    connection.Disconnected += MessageConnection_Disconnected;

                    await connection.ConnectAsync().ConfigureAwait(false);

                    var request = new PierceFirewallRequest(connectToPeerResponse.Token).ToByteArray();
                    await connection.WriteAsync(request).ConfigureAwait(false);

                    (_, connection) = AddOrUpdateMessageConnectionRecord(connectToPeerResponse.Username, connection);

                    Diagnostic.Debug($"Solicited direct connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}) established.");
                    return connection;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        ///     Gets a new or existing message connection to the specified <paramref name="username"/>.
        /// </summary>
        /// <remarks>
        ///     If a connection doesn't exist, a new direct connection is attempted first, and, if unsuccessful, an indirect
        ///     connection is attempted.
        /// </remarks>
        /// <param name="username">The username of the user to which to connect.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the new or existing connection.</returns>
        public async Task<IMessageConnection> GetOrAddMessageConnectionAsync(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);
            await semaphore.WaitAsync().ConfigureAwait(false);

            IMessageConnection connection;

            try
            {
                (_, connection) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);

                if (connection != null)
                {
                    return connection;
                }
                else
                {
                    var directCts = new CancellationTokenSource();
                    var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);

                    var indirectCts = new CancellationTokenSource();
                    var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);

                    var direct = GetMessageConnectionOutboundDirectAsync(username, ipAddress, port, directLinkedCts.Token);
                    var indirect = GetMessageConnectionOutboundIndirectAsync(username, indirectLinkedCts.Token);

                    Diagnostic.Debug($"Attempting direct and indirect message connections to {username} ({ipAddress}:{port})");

                    List<Task<IMessageConnection>> tasks = new List<Task<IMessageConnection>>() { direct, indirect };
                    Task<IMessageConnection> task;
                    do
                    {
                        task = await Task.WhenAny(direct, indirect).ConfigureAwait(false);
                        tasks.Remove(task);
                    }
                    while (task.IsFaulted && tasks.Count > 0);

                    if (task.IsFaulted)
                    {
                        throw new ConnectionException($"Failed to establish a transfer connection to {username} ({ipAddress}:{port})");
                    }

                    connection = await task.ConfigureAwait(false);
                    var isDirect = task == direct;

                    Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} message connection to {username} ({ipAddress}:{port}) established; cancelling {(isDirect ? "indirect" : "direct")} connection.");
                    (isDirect ? indirectCts : directCts).Cancel();

                    if (isDirect)
                    {
                        // if connecting directly, init the connection. for indirect connections the incoming peerinit is handled
                        // in the listener code to determine the connection type, so we don't need to handle it here.
                        var request = new PeerInitRequest(SoulseekClient.Username, Constants.ConnectionType.Peer, SoulseekClient.GetNextToken()).ToByteArray();
                        await connection.WriteAsync(request, cancellationToken).ConfigureAwait(false);
                    }

                    (_, connection) = AddOrUpdateMessageConnectionRecord(username, connection);

                    Diagnostic.Debug($"Unsolicited {(isDirect ? "direct" : "indirect")} message connection to {username} ({ipAddress}:{port}) established.");
                    return connection;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        ///     Gets a new transfer connection using the details in the specified <paramref name="connectToPeerResponse"/>, pierces
        ///     the remote peer's firewall, and retrieves the remote token.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context, including the new connection and the associated remote token.</returns>
        public async Task<(IConnection Connection, int RemoteToken)> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var connection = ConnectionFactory.GetConnection(
                connectToPeerResponse.IPAddress,
                connectToPeerResponse.Port,
                SoulseekClient.Options.TransferConnectionOptions);

            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Solicited transfer connection for token {connectToPeerResponse.Token} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}) disconnected.");

            await connection.ConnectAsync().ConfigureAwait(false);

            var request = new PierceFirewallRequest(connectToPeerResponse.Token);
            await connection.WriteAsync(request.ToByteArray()).ConfigureAwait(false);

            var remoteTokenBytes = await connection.ReadAsync(4).ConfigureAwait(false);
            var remoteToken = BitConverter.ToInt32(remoteTokenBytes, 0);

            return (connection, remoteToken);
        }

        /// <summary>
        ///     Gets a new transfer connection to the specified <paramref name="username"/> using the specified <paramref name="token"/>.
        /// </summary>
        /// <remarks>A direct connection is attempted first, and, if unsuccessful, an indirect connection is attempted.</remarks>
        /// <param name="username">The username of the user to which to connect.</param>
        /// <param name="token">The token with which to initialize the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the new connection.</returns>
        public async Task<IConnection> GetTransferConnectionAsync(string username, IPAddress ipAddress, int port, int token, CancellationToken cancellationToken)
        {
            var directCts = new CancellationTokenSource();
            var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);

            var indirectCts = new CancellationTokenSource();
            var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);

            var direct = GetTransferConnectionOutboundDirectAsync(ipAddress, port, token, directLinkedCts.Token);
            var indirect = GetTransferConnectionOutboundIndirectAsync(username, token, indirectLinkedCts.Token);

            Diagnostic.Debug($"Attempting direct and indirect transfer connections to {username} ({ipAddress}:{port})");

            List<Task<IConnection>> tasks = new List<Task<IConnection>>() { direct, indirect };
            Task<IConnection> task;
            do
            {
                task = await Task.WhenAny(direct, indirect).ConfigureAwait(false);
                tasks.Remove(task);
            }
            while (task.IsFaulted && tasks.Count > 0);

            if (task.IsFaulted)
            {
                throw new ConnectionException($"Failed to establish a transfer connection to {username} ({ipAddress}:{port})");
            }

            var connection = await task.ConfigureAwait(false);
            var isDirect = task == direct;

            Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} transfer connection to {username} ({ipAddress}:{port}) established; cancelling {(isDirect ? "indirect" : "direct")} connection.");
            (isDirect ? indirectCts : directCts).Cancel();

            if (isDirect)
            {
                // if connecting directly, init the connection. for indirect connections the incoming peerinit is handled in the
                // listener code to determine the connection type, so we don't need to handle it here.
                var request = new PeerInitRequest(SoulseekClient.Username, Constants.ConnectionType.Tranfer, token).ToByteArray();
                await connection.WriteAsync(request, cancellationToken).ConfigureAwait(false);
            }

            // send our token (the remote token from the other side's perspective)
            await connection.WriteAsync(BitConverter.GetBytes(token), cancellationToken).ConfigureAwait(false);
            return connection;
        }

        /// <summary>
        ///     Removes and disposes all active and queued connections.
        /// </summary>
        public void RemoveAndDisposeAll()
        {
            while (!MessageConnections.IsEmpty)
            {
                if (MessageConnections.TryRemove(MessageConnections.Keys.First(), out var value))
                {
                    value.Semaphore?.Dispose();
                    value.Connection?.Dispose();
                }
            }
        }

        private async Task<IMessageConnection> AddInboundMessageConnectionAsync(string username, IPAddress ipAddress, int port, ITcpClient tcpClient)
        {
            var connection = ConnectionFactory.GetMessageConnection(
                username,
                ipAddress,
                port,
                SoulseekClient.Options.PeerConnectionOptions,
                tcpClient);

            connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
            connection.Disconnected += MessageConnection_Disconnected;

            connection.StartReadingContinuously();

            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                AddOrUpdateMessageConnectionRecord(username, connection);
                Diagnostic.Debug($"Unsolicited inbound connection to {username} ({connection.IPAddress}:{connection.Port}) established.");
            }
            finally
            {
                semaphore.Release();
            }

            return connection;
        }

        private async Task<IConnection> AddInboundTransferConnectionAsync(string username, IPAddress ipAddress, int port, int token, ITcpClient tcpClient)
        {
            var connection = ConnectionFactory.GetConnection(ipAddress, port, SoulseekClient.Options.TransferConnectionOptions, tcpClient);

            var remoteTokenBytes = await connection.ReadAsync(4).ConfigureAwait(false);
            var remoteToken = BitConverter.ToInt32(remoteTokenBytes, 0);

            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Inbound transfer connection for token {token} (remote: {remoteToken}) ({ipAddress}:{port}) disconnected."); ;

            SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.DirectTransfer, username, remoteToken), connection);

            return connection;
        }

        private (SemaphoreSlim Semaphore, IMessageConnection Connection) AddOrUpdateMessageConnectionRecord(string username, IMessageConnection connection)
        {
            return MessageConnections.AddOrUpdate(username, (new SemaphoreSlim(1, 1), connection), (k, v) =>
            {
                // unassign the handler from the connection we are discarding to prevent it from removing a live connection.
                if (v.Connection != null)
                {
                    v.Connection.Disconnected -= MessageConnection_Disconnected;
                }

                return (v.Semaphore, connection);
            });
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    RemoveAndDisposeAll();
                    MessageSemaphore.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<IMessageConnection> GetMessageConnectionOutboundDirectAsync(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetMessageConnection(
                username,
                ipAddress,
                port,
                SoulseekClient.Options.PeerConnectionOptions);

            connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
            connection.Disconnected += MessageConnection_Disconnected;

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            return connection;
        }

        private async Task<IMessageConnection> GetMessageConnectionOutboundIndirectAsync(string username, CancellationToken cancellationToken)
        {
            var token = SoulseekClient.GetNextToken();

            try
            {
                PendingSolicitations.TryAdd(token, username);

                await SoulseekClient.ServerConnection
                    .WriteAsync(new ConnectToPeerRequest(token, username, Constants.ConnectionType.Peer).ToByteArray(), cancellationToken)
                    .ConfigureAwait(false);

                var incomingConnection = await SoulseekClient.Waiter
                    .Wait<IConnection>(new WaitKey(Constants.WaitKey.SolicitedConnection, username, token), null, cancellationToken)
                    .ConfigureAwait(false);

                var connection = ConnectionFactory.GetMessageConnection(
                    username,
                    incomingConnection.IPAddress,
                    incomingConnection.Port,
                    SoulseekClient.Options.PeerConnectionOptions,
                    incomingConnection.HandoffTcpClient());

                connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
                connection.Disconnected += MessageConnection_Disconnected;

                connection.StartReadingContinuously();

                return connection;
            }
            finally
            {
                PendingSolicitations.TryRemove(token, out var _);
            }
        }

        private async Task<(SemaphoreSlim Semaphore, IMessageConnection Connection)> GetOrAddMessageConnectionRecordAsync(string username)
        {
            if (MessageConnections.TryGetValue(username, out var record))
            {
                return record;
            }

            Interlocked.Increment(ref waitingMessageConnections);
            await MessageSemaphore.WaitAsync().ConfigureAwait(false);
            Interlocked.Decrement(ref waitingMessageConnections);

            record = MessageConnections.GetOrAdd(username, (k) => (new SemaphoreSlim(1, 1), null));

            if (record.Connection == null)
            {
                Diagnostic.Debug($"Initialized message connection to {username}");
            }

            return record;
        }

        private async Task<IConnection> GetTransferConnectionOutboundDirectAsync(IPAddress ipAddress, int port, int token, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetConnection(ipAddress, port, SoulseekClient.Options.TransferConnectionOptions);

            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Outbound direct transfer connection for token {token} ({ipAddress}:{port}) disconnected.");

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            return connection;
        }

        private async Task<IConnection> GetTransferConnectionOutboundIndirectAsync(string username, int token, CancellationToken cancellationToken)
        {
            var solicitationToken = SoulseekClient.GetNextToken();

            try
            {
                PendingSolicitations.TryAdd(solicitationToken, username);

                await SoulseekClient.ServerConnection
                    .WriteAsync(new ConnectToPeerRequest(solicitationToken, username, Constants.ConnectionType.Tranfer).ToByteArray(), cancellationToken)
                    .ConfigureAwait(false);

                var incomingConnection = await SoulseekClient.Waiter
                    .Wait<IConnection>(new WaitKey(Constants.WaitKey.SolicitedConnection, username, solicitationToken), null, cancellationToken)
                    .ConfigureAwait(false);

                var connection = ConnectionFactory.GetConnection(
                    incomingConnection.IPAddress,
                    incomingConnection.Port,
                    SoulseekClient.Options.TransferConnectionOptions,
                    incomingConnection.HandoffTcpClient());

                connection.Disconnected += (sender, e) => Diagnostic.Debug($"Outbound indirect transfer connection for token {token} ({incomingConnection.IPAddress}:{incomingConnection.Port}) disconnected.");

                return connection;
            }
            finally
            {
                PendingSolicitations.TryRemove(token, out var _);
            }
        }

        private async void Listener_Accepted(object sender, IConnection connection)
        {
            Diagnostic.Info($"Accepted incoming connection from {connection.IPAddress}:{SoulseekClient.Listener.Port}");

            try
            {
                var lengthBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);

                var bodyBytes = await connection.ReadAsync(length).ConfigureAwait(false);
                byte[] message = lengthBytes.Concat(bodyBytes).ToArray();

                if (PeerInitResponse.TryFromByteArray(message, out var peerInit))
                {
                    // this connection is the result of an unsolicited connection from the remote peer, either to request info or
                    // browse, or to send a file.
                    Diagnostic.Debug($"PeerInit for transfer type {peerInit.TransferType} received from {peerInit.Username} ({connection.IPAddress}:{SoulseekClient.Listener.Port})");

                    if (peerInit.TransferType == Constants.ConnectionType.Peer)
                    {
                        await AddInboundMessageConnectionAsync(
                            peerInit.Username,
                            connection.IPAddress,
                            SoulseekClient.Listener.Port,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                    else if (peerInit.TransferType == Constants.ConnectionType.Tranfer)
                    {
                        await AddInboundTransferConnectionAsync(
                            peerInit.Username,
                            connection.IPAddress,
                            SoulseekClient.Listener.Port,
                            peerInit.Token,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                }
                else if (PierceFirewallResponse.TryFromByteArray(message, out var pierceFirewall))
                {
                    // this connection is the result of a ConnectToPeer request sent to the user, and the incoming message will
                    // contain the token that was provided in the request. Ensure this token is among those expected, and use it to
                    // determine the username of the remote user.
                    if (PendingSolicitations.TryGetValue(pierceFirewall.Token, out var username))
                    {
                        Diagnostic.Debug($"PierceFirewall with token {pierceFirewall.Token} received from {username} ({connection.IPAddress}:{SoulseekClient.Listener.Port})");
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedConnection, username, pierceFirewall.Token), connection);
                    }
                    else
                    {
                        throw new ConnectionException($"Unknown PierceFirewall attempt with token {pierceFirewall.Token} from {connection.IPAddress}:{connection.Port}");
                    }
                }
                else
                {
                    throw new ConnectionException($"Unknown direct connection type from {connection.IPAddress}:{connection.Port}");
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Failed to initialize direct connection from {connection.IPAddress}:{connection.Port}: {ex.Message}");
                connection.Disconnect(ex.Message);
                connection.Dispose();
            }
        }

        private void MessageConnection_Disconnected(object sender, string message)
        {
            RemoveMessageConnectionRecord((IMessageConnection)sender);
        }

        private void RemoveMessageConnectionRecord(IMessageConnection connection)
        {
            if (MessageConnections.TryRemove(connection.Key.Username, out _))
            {
                Diagnostic.Debug($"Removing message connection to {connection.Key.Username} ({connection.IPAddress}:{connection.Port})");

                // only release if we successfully removed a connection. this can throw if another thread released it first and the
                // semaphore tries to release more than its capacity.
                MessageSemaphore.Release();
            }
        }
    }
}