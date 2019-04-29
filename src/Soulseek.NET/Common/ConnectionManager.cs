// <copyright file="ConnectionManager.cs" company="JP Dillingham">
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
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     Manages a queue of <see cref="IConnection"/>
    /// </summary>
    internal sealed class ConnectionManager : IConnectionManager
    {
        private int waitingPeerConnections = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="concurrentPeerConnections">The number of allowed concurrent peer message connections.</param>
        /// <param name="connectionFactory">The IConnectionFactory instance to use.</param>
        internal ConnectionManager(int concurrentPeerConnections, IConnectionFactory connectionFactory = null)
        {
            if (concurrentPeerConnections < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrentPeerConnections), $"Concurrent connection option must be greater than zero.");
            }

            ConcurrentPeerConnections = concurrentPeerConnections;

            PeerSemaphore = new SemaphoreSlim(ConcurrentPeerConnections, ConcurrentPeerConnections);
            PeerConnections = new ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

            TransferConnections = new ConcurrentDictionary<(ConnectionKey Key, int Token), IConnection>();

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();
            TokenFactory = new TokenFactory();
        }

        /// <summary>
        ///     Gets the number of active peer message connections.
        /// </summary>
        public int ActivePeerConnections => PeerConnections.Count;

        /// <summary>
        ///     Gets the number of active transfer connections.
        /// </summary>
        public int ActiveTransferConnections => TransferConnections.Count;

        /// <summary>
        ///     Gets the number of allowed concurrent peer message connections.
        /// </summary>
        public int ConcurrentPeerConnections { get; }

        /// <summary>
        ///     Gets the number of waiting peer message connections.
        /// </summary>
        public int WaitingPeerConnections => waitingPeerConnections;

        private IConnectionFactory ConnectionFactory { get; }
        private bool Disposed { get; set; }
        private ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)> PeerConnections { get; }
        private SemaphoreSlim PeerSemaphore { get; }
        private TokenFactory TokenFactory { get; }
        private ConcurrentDictionary<(ConnectionKey Key, int Token), IConnection> TransferConnections { get; }

        /// <summary>
        ///     Adds a new transfer <see cref="IConnection"/> and pierces the firewall.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests while the connection is connecting.</param>
        /// <returns>The new connection.</returns>
        public async Task<IConnection> AddSolicitedTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetConnection(connectToPeerResponse.IPAddress, connectToPeerResponse.Port, options);
            connection.Disconnected += (sender, e) => TransferConnections.TryRemove((connection.Key, connectToPeerResponse.Token), out _);

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            TransferConnections.AddOrUpdate((connection.Key, connectToPeerResponse.Token), connection, (k, v) => connection);

            var request = new PierceFirewallRequest(connectToPeerResponse.Token);
            await connection.WriteAsync(request.ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

            return connection;
        }

        /// <summary>
        ///     Adds a new transfer <see cref="IConnection"/> and sends a peer init request.
        /// </summary>
        /// <param name="connectionKey">The connection key, comprised of the remote IP address and port.</param>
        /// <param name="token">The transfer token.</param>
        /// <param name="localUsername">The username of the local user, required to initiate the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests while the connection is connecting.</param>
        /// <returns>The new connection.</returns>
        public async Task<IConnection> AddUnsolicitedTransferConnectionAsync(ConnectionKey connectionKey, int token, string localUsername, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetConnection(connectionKey.IPAddress, connectionKey.Port, options);
            connection.Disconnected += (sender, e) => TransferConnections.TryRemove((connection.Key, token), out _);

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            TransferConnections.AddOrUpdate((connection.Key, token), connection, (k, v) => connection);

            await connection.WriteAsync(new PeerInitRequest(localUsername, "F", token).ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

            return connection;
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets an existing peer <see cref="IMessageConnection"/>, or adds and initialized a new instance if one does not exist.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <param name="messageHandler">
        ///     The message handler to subscribe to the connection's <see cref="IMessageConnection.MessageRead"/> event.
        /// </param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests while the connection is connecting.</param>
        /// <returns>The existing or new connection.</returns>
        public async Task<IMessageConnection> GetOrAddSolicitedConnectionAsync(ConnectToPeerResponse connectToPeerResponse, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var key = new ConnectionKey(connectToPeerResponse.Username, connectToPeerResponse.IPAddress, connectToPeerResponse.Port, MessageConnectionType.Peer);
            var (semaphore, connection) = await GetOrAddMessageConnectionAsync(key).ConfigureAwait(false);

            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (connection != null)
                {
                    return connection;
                }
                else
                {
                    connection = ConnectionFactory.GetMessageConnection(MessageConnectionType.Peer, connectToPeerResponse.Username, connectToPeerResponse.IPAddress, connectToPeerResponse.Port, options);
                    connection.Context = connectToPeerResponse;

                    connection.MessageRead += messageHandler;
                    connection.Disconnected += (sender, e) => RemoveMessageConnection(connection);

                    await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    AddOrUpdateMessageConnection(key, connection);

                    var context = (ConnectToPeerResponse)connection.Context;
                    var request = new PierceFirewallRequest(context.Token).ToMessage();
                    await connection.WriteAsync(request.ToByteArray(), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }

            return connection;
        }

        /// <summary>
        ///     Gets an existing peer <see cref="IMessageConnection"/>, or adds and initializes new instance if one does not exist.
        /// </summary>
        /// <remarks>Solicited connections (such as one used to retrieve search results) will be reused if possible.</remarks>
        /// <param name="connectionKey">The connection key, comprised of the remote IP address and port.</param>
        /// <param name="localUsername">The username of the local user, required to initiate the connection.</param>
        /// <param name="messageHandler">
        ///     The message handler to substribe to the conection's <see cref="IMessageConnection.MessageRead"/> event.
        /// </param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests while the connection is connecting.</param>
        /// <returns>The existing or new connection.</returns>
        public async Task<IMessageConnection> GetOrAddUnsolicitedConnectionAsync(ConnectionKey connectionKey, string localUsername, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var (semaphore, connection) = await GetOrAddMessageConnectionAsync(connectionKey).ConfigureAwait(false);

            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (connection != null)
                {
                    return connection;
                }
                else
                {
                    connection = ConnectionFactory.GetMessageConnection(MessageConnectionType.Peer, connectionKey.Username, connectionKey.IPAddress, connectionKey.Port, options);
                    connection.MessageRead += messageHandler;
                    connection.Disconnected += (sender, e) => RemoveMessageConnection(connection);

                    await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    AddOrUpdateMessageConnection(connectionKey, connection);

                    await connection.WriteAsync(new PeerInitRequest(localUsername, "P", TokenFactory.GetToken()).ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }

            return connection;
        }

        /// <summary>
        ///     Disposes and removes all active and queued connections.
        /// </summary>
        public void RemoveAndDisposeAll()
        {
            while (!PeerConnections.IsEmpty)
            {
                if (PeerConnections.TryRemove(PeerConnections.Keys.First(), out var value))
                {
                    value.Semaphore?.Dispose();
                    value.Connection?.Dispose();
                }
            }

            TransferConnections.RemoveAndDisposeAll();
        }

        private void AddOrUpdateMessageConnection(ConnectionKey key, IMessageConnection connection)
        {
            PeerConnections.AddOrUpdate(key, (new SemaphoreSlim(1, 1), connection), (k, v) => (v.Semaphore, connection));
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    RemoveAndDisposeAll();
                    PeerSemaphore.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<(SemaphoreSlim Semaphore, IMessageConnection Connection)> GetOrAddMessageConnectionAsync(ConnectionKey key)
        {
            if (PeerConnections.ContainsKey(key))
            {
                return PeerConnections[key];
            }

            Interlocked.Increment(ref waitingPeerConnections);

            await PeerSemaphore.WaitAsync().ConfigureAwait(false);

            Interlocked.Decrement(ref waitingPeerConnections);

            return PeerConnections.GetOrAdd(key, (new SemaphoreSlim(1, 1), null));
        }

        private void RemoveMessageConnection(IMessageConnection connection)
        {
            PeerConnections.TryRemove(connection.Key, out _);
            PeerSemaphore.Release();
        }
    }
}