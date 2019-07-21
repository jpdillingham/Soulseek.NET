// <copyright file="DistributedConnectionManager.cs" company="JP Dillingham">
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
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;

    internal interface IDistributedConnectionManager : IDisposable, IDiagnosticGenerator
    {
        Task UpdateParentPool(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents);
    }

    internal sealed class DistributedConnectionManager : IDistributedConnectionManager
    {
        public DistributedConnectionManager(
            ISoulseekClient soulseekClient,
            IWaiter waiter,
            IDistributedMessageHandler distributedMessageHandler,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient;
            Waiter = waiter;
            DistributedMessageHandler = distributedMessageHandler;

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new ClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        private IDistributedMessageHandler DistributedMessageHandler { get; }
        private IWaiter Waiter { get; }
        private ISoulseekClient SoulseekClient { get; }
        private ConcurrentDictionary<string, IMessageConnection> ChildConnections { get; } = new ConcurrentDictionary<string, IMessageConnection>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private IMessageConnection ParentConnection { get; set; }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IPeerConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Removes and disposes all active and queued connections.
        /// </summary>
        public void RemoveAndDisposeAll()
        {
            ParentConnection?.Dispose();

            while (!ChildConnections.IsEmpty)
            {
                if (ChildConnections.TryRemove(ChildConnections.Keys.First(), out var value))
                {
                    value?.Dispose();
                }
            }
        }

        public async Task UpdateParentPool(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            // if we're connected to a parent already, ignore this.
            if (ParentConnection != null && ParentConnection.State == ConnectionState.Connected)
            {
                return;
            }

            var parentTasks = parents.Select(p => GetConnection(p.Username, p.IPAddress, p.Port, CancellationToken.None)).ToList();

            Task<IMessageConnection> parent;

            do
            {
                parent = await Task.WhenAny(parentTasks).ConfigureAwait(false);
                parentTasks.Remove(parent);
            }
            while (parent.IsFaulted && parentTasks.Count > 0);

            if (parent.IsFaulted)
            {
                Console.WriteLine($"Failed to connect to any of the given parents.");
            }

            ParentConnection = await parent.ConfigureAwait(false);

            // todo: disconnect all others
            Diagnostic.Debug($"Adopted parent {ParentConnection.Username} ({ParentConnection.IPAddress}:{ParentConnection.Port})");
        }

        private async Task<IMessageConnection> GetConnection(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetMessageConnection(username, ipAddress, port, SoulseekClient.Options.DistributedConnectionOptions);
            connection.MessageRead += DistributedMessageHandler.HandleMessage;

            Diagnostic.Debug($"Attempting distributed connection to {username} ({ipAddress}:{port})");
            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            await connection.WriteAsync(new PeerInitRequest(SoulseekClient.Username, Constants.ConnectionType.Distributed, SoulseekClient.GetNextToken()).ToByteArray(), cancellationToken).ConfigureAwait(false);
            Diagnostic.Debug($"Distributed connection to {username} established.");

            return connection;
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
    }
}