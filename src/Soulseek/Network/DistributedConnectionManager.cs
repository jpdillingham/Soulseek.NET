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
        bool HasParent { get; }
        Task AddParentConnectionAsync(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parentCandidates);
        Task SetDistributedBranchLevel(int level);
        Task SetDistributedBranchRoot(string username);
        Task WriteDistributedChildrenAsync(byte[] bytes);
    }

    internal sealed class DistributedConnectionManager : IDistributedConnectionManager
    {
        public DistributedConnectionManager(
            SoulseekClient soulseekClient,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient;
            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new ClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        public bool HasParent => ParentConnection != null && ParentConnection.State == ConnectionState.Connected;

        private IDistributedMessageHandler DistributedMessageHandler { get; }
        private IWaiter Waiter { get; }
        private SoulseekClient SoulseekClient { get; }
        private ConcurrentDictionary<string, IMessageConnection> ChildConnections { get; } = new ConcurrentDictionary<string, IMessageConnection>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private IMessageConnection ParentConnection { get; set; }

        private IMessageConnection DistributedParentConnection { get; set; }
        private string DistributedBranchRoot { get; set; }
        private int DistributedBranchLevel { get; set; }
        private List<IMessageConnection> DistributedChildConnections { get; } = new List<IMessageConnection>();

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

        public async Task SetDistributedBranchRoot(string username)
        {
            DistributedBranchRoot = username;
            await SoulseekClient.ServerConnection.WriteAsync(new DistributedBranchRoot(DistributedBranchRoot).ToByteArray()).ConfigureAwait(false);
            // todo: send to children
        }

        public async Task SetDistributedBranchLevel(int level)
        {
            DistributedBranchLevel = level;
            await SoulseekClient.ServerConnection.WriteAsync(new DistributedBranchLevel(DistributedBranchLevel).ToByteArray()).ConfigureAwait(false);
            // todo: send to children
        }

        public async Task WriteDistributedChildrenAsync(byte[] bytes)
        {
            var tasks = DistributedChildConnections.Select(async c =>
            {
                try
                {
                    await c.WriteAsync(bytes).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    c.Dispose();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parentCandidates)
        {
            if (DistributedParentConnection != null && DistributedParentConnection.State == ConnectionState.Connected)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            var options = SoulseekClient.Options.DistributedConnectionOptions;
            var server = SoulseekClient.ServerConnection;

            var pendingConnections = parentCandidates
                .Select(p => ConnectionFactory.GetMessageConnection(p.Username, p.IPAddress, p.Port, options))
                .ToList();

            pendingConnections.ForEach(c =>
            {
                c.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessage;
                c.Disconnected += (sender, args) =>
                {
                    var connection = (IMessageConnection)sender;

                    Diagnostic.Debug($"Discarded distributed parent candidate {connection.Username} ({connection.IPAddress}:{connection.Port})");

                    if (connection == DistributedParentConnection)
                    {
                        Diagnostic.Debug($"Distributed parent {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected, notifying server.");
                        server.WriteAsync(new HaveNoParents(true).ToByteArray());
                        // todo: disallow children
                    }
                };
            });

            var pendingConnectTasks = pendingConnections.Select(async c =>
            {
                await c.ConnectAsync(cts.Token).ConfigureAwait(false);
                await c.WriteAsync(new PeerInitRequest(SoulseekClient.Username, Constants.ConnectionType.Distributed, SoulseekClient.GetNextToken()).ToByteArray(), cts.Token).ConfigureAwait(false);
                Diagnostic.Debug($"Distributed connection to {c.Username} established.");
                return c;
            }).ToList();

            Task<IMessageConnection> parentTask;

            do
            {
                parentTask = await Task.WhenAny(pendingConnectTasks).ConfigureAwait(false);
                pendingConnectTasks.Remove(parentTask);
            }
            while (parentTask.IsFaulted && pendingConnectTasks.Count > 0);

            if (parentTask.IsFaulted)
            {
                Diagnostic.Debug($"Failed to connect to any of the distributed parent candidates; notifying server.");
                await server.WriteAsync(new HaveNoParents(true).ToByteArray()).ConfigureAwait(false);
                // todo: disallow children
                throw new ConnectionException($"Failed to connect to any of the parent candidates.");
            }

            DistributedParentConnection = await parentTask.ConfigureAwait(false);
            Diagnostic.Debug($"Adopted distributed parent {DistributedParentConnection.Username} ({DistributedParentConnection.IPAddress}:{DistributedParentConnection.Port})");

            cts.Cancel();
            pendingConnections.Remove(DistributedParentConnection);

            foreach (var connection in pendingConnections)
            {
                connection.Dispose();
            }

            await server.WriteAsync(new HaveNoParents(false).ToByteArray()).ConfigureAwait(false);
            await server.WriteAsync(new ParentsIP(DistributedParentConnection.IPAddress).ToByteArray()).ConfigureAwait(false);
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