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
        private int waitingMessageConnections = 0;
        private int waitingTransferConnections = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="concurrentConnections">The number of allowed concurrent connections.</param>
        internal ConnectionManager(int concurrentMessageConnections = 500, int concurrentTransferConnections = 50)
        {
            ConcurrentMessageConnections = concurrentMessageConnections;
            MessageConnectionSemaphore = new SemaphoreSlim(ConcurrentMessageConnections, ConcurrentMessageConnections);

            ConcurrentTransferConnections = concurrentTransferConnections;
            TransferConnectionSemaphore = new SemaphoreSlim(ConcurrentTransferConnections, ConcurrentTransferConnections);
        }

        /// <summary>
        ///     Gets the number of allowed concurrent connections.
        /// </summary>
        public int ConcurrentMessageConnections { get; }

        public int ConcurrentTransferConnections { get; }

        private bool Disposed { get; set; }
        private ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)> MessageConnections { get; } = new ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();
        private SemaphoreSlim MessageConnectionSemaphore { get; }
        private ConcurrentDictionary<(ConnectionKey Key, int Token), IConnection> TransferConnections { get; } = new ConcurrentDictionary<(ConnectionKey Key, int Token), IConnection>();
        private SemaphoreSlim TransferConnectionSemaphore { get; }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IMessageConnection> GetSolicitedConnectionAsync(ConnectToPeerResponse connectToPeerResponse, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken)
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
                    connection = new MessageConnection(MessageConnectionType.Peer, connectToPeerResponse.Username, connectToPeerResponse.IPAddress, connectToPeerResponse.Port, options)
                    {
                        Context = connectToPeerResponse,
                    };

                    connection.MessageRead += messageHandler;
                    connection.Disconnected += (sender, e) => RemoveMessageConnection(connection);

                    await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    var context = (ConnectToPeerResponse)connection.Context;
                    var request = new PierceFirewallRequest(context.Token).ToMessage();
                    await connection.WriteAsync(request.ToByteArray(), cancellationToken).ConfigureAwait(false);

                    AddOrUpdateMessageConnection(key, connection);
                }
            }
            finally
            {
                semaphore.Release();
            }

            return connection;
        }

        public async Task<IConnection> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref waitingTransferConnections);

            await TransferConnectionSemaphore.WaitAsync().ConfigureAwait(false);

            Interlocked.Decrement(ref waitingTransferConnections);

            var connection = new Connection(connectToPeerResponse.IPAddress, connectToPeerResponse.Port, options);
            connection.Disconnected += (sender, e) => TransferConnections.TryRemove((connection.Key, connectToPeerResponse.Token), out _);

            TransferConnections.AddOrUpdate((connection.Key, connectToPeerResponse.Token), connection, (k, v) => connection);

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var request = new PierceFirewallRequest(connectToPeerResponse.Token);
            await connection.WriteAsync(request.ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

            return connection;
        }

        public async Task<IMessageConnection> GetUnsolicitedConnectionAsync(string localUsername, ConnectionKey connectionKey, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var (semaphore, connection) = await GetOrAddMessageConnectionAsync(connectionKey).ConfigureAwait(false);

            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (connection != null)
                {
                    if (connection.Context is ConnectToPeerResponse)
                    {
                        Console.WriteLine($"Reusing solicited connection for a download...");
                    }

                    return connection;
                }
                else
                {
                    connection = new MessageConnection(MessageConnectionType.Peer, connectionKey.Username, connectionKey.IPAddress, connectionKey.Port, options);
                    connection.MessageRead += messageHandler;
                    connection.Disconnected += (sender, e) => RemoveMessageConnection(connection);

                    await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    var token = new Random().Next(1, 2147483647);
                    await connection.WriteAsync(new PeerInitRequest(localUsername, "P", token).ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

                    AddOrUpdateMessageConnection(connectionKey, connection);
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
            while (!MessageConnections.IsEmpty)
            {
                if (MessageConnections.TryRemove(MessageConnections.Keys.First(), out var value))
                {
                    value.Semaphore.Dispose();
                    value.Connection.Dispose();
                }
            }

            TransferConnections.RemoveAndDisposeAll();
        }

        private void AddOrUpdateMessageConnection(ConnectionKey key, IMessageConnection connection)
        {
            MessageConnections.AddOrUpdate(key, (new SemaphoreSlim(1, 1), connection), (k, v) => (v.Semaphore, connection));
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    RemoveAndDisposeAll();
                }

                Disposed = true;
            }
        }

        private async Task<(SemaphoreSlim Semaphore, IMessageConnection Connection)> GetOrAddMessageConnectionAsync(ConnectionKey key)
        {
            if (MessageConnections.ContainsKey(key))
            {
                return MessageConnections[key];
            }

            Interlocked.Increment(ref waitingMessageConnections);

            await MessageConnectionSemaphore.WaitAsync().ConfigureAwait(false);

            Interlocked.Decrement(ref waitingMessageConnections);

            return MessageConnections.GetOrAdd(key, (new SemaphoreSlim(1, 1), null));
        }

        private void RemoveMessageConnection(IMessageConnection connection)
        {
            MessageConnections.TryRemove(connection.Key, out _);
            MessageConnectionSemaphore.Release(1);
        }
    }
}