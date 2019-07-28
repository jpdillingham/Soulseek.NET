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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

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

        private int BranchLevel { get; set; }
        private string BranchRoot { get; set; }
        private bool CanAcceptChildren => ChildConnections.Count < SoulseekClient.Options.ConcurrentDistributedChildrenLimit;
        private ConcurrentDictionary<string, IMessageConnection> ChildConnections { get; } = new ConcurrentDictionary<string, IMessageConnection>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private bool HaveParent => ParentConnection != null && ParentConnection.State == ConnectionState.Connected;
        private IMessageConnection ParentConnection { get; set; }
        private SoulseekClient SoulseekClient { get; }

        public async Task AddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var r = connectToPeerResponse;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Distributed child {r.Username} ({r.IPAddress}:{r.Port}) rejected; limit reached.");
                await WriteStatusAsync().ConfigureAwait(false);
                return;
            }

            var connection = ConnectionFactory.GetMessageConnection(
                r.Username,
                r.IPAddress,
                r.Port,
                SoulseekClient.Options.DistributedConnectionOptions);

            connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleChildMessage;
            connection.Disconnected += async (sender, args) =>
            {
                ChildConnections.TryRemove(connection.Username, out _);
                Diagnostic.Debug($"Distributed child {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected.  Total children: {ChildConnections.Count}");
                await WriteStatusAsync().ConfigureAwait(false);
            };

            await connection.ConnectAsync().ConfigureAwait(false);

            var request = new PierceFirewallRequest(r.Token);
            await connection.WriteAsync(request.ToByteArray()).ConfigureAwait(false);

            ChildConnections.AddOrUpdate(connection.Username, connection, (k, v) =>
            {
                v.Dispose();
                return connection;
            });

            Diagnostic.Debug($"Added distributed child {connection.Username} ({connection.IPAddress}:{connection.Port}).  Total children: {ChildConnections.Count}");

            await WriteStatusAsync().ConfigureAwait(false);
        }

        public async Task AddChildConnectionAsync(string username, ITcpClient tcpClient)
        {
            var endpoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Distributed child {username} ({endpoint.Address}:{endpoint.Port}) rejected; limit reached.");
                tcpClient.Dispose();
                await WriteStatusAsync().ConfigureAwait(false);
                return;
            }

            var connection = ConnectionFactory.GetMessageConnection(
                username,
                endpoint.Address,
                endpoint.Port,
                SoulseekClient.Options.DistributedConnectionOptions,
                tcpClient);

            connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleChildMessage;
            connection.Disconnected += async (sender, args) =>
            {
                ChildConnections.TryRemove(username, out _);
                Diagnostic.Debug($"Distributed child {username} ({connection.IPAddress}:{connection.Port}) disconnected.  Total children: {ChildConnections.Count}");
                await WriteStatusAsync().ConfigureAwait(false);
            };

            ChildConnections.AddOrUpdate(username, connection, (k, v) =>
            {
                v.Dispose();
                return connection;
            });

            connection.StartReadingContinuously();

            Diagnostic.Debug($"Added distributed child {username} ({connection.IPAddress}:{connection.Port}).  Total children: {ChildConnections.Count}");

            await WriteStatusAsync().ConfigureAwait(false);
        }

        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parentCandidates)
        {
            if (HaveParent)
            {
                return;
            }

            using (var cts = new CancellationTokenSource())
            {
                var options = SoulseekClient.Options.DistributedConnectionOptions;

                // todo: also attempt an indirect connection here
                var pendingConnections = parentCandidates
                    .Select(p => ConnectionFactory.GetMessageConnection(p.Username, p.IPAddress, p.Port, options))
                    .ToList();

                pendingConnections.ForEach(c =>
                {
                    c.MessageRead += SoulseekClient.DistributedMessageHandler.HandleParentMessage;
                    c.Disconnected += async (sender, args) =>
                    {
                        var connection = (IMessageConnection)sender;

                        Diagnostic.Debug($"Discarded distributed parent candidate {connection.Username} ({connection.IPAddress}:{connection.Port})");

                        if (connection == ParentConnection)
                        {
                            Diagnostic.Debug($"Distributed parent {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected, notifying server.");
                            ParentConnection = null;
                            await WriteStatusAsync().ConfigureAwait(false);
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
                    await WriteStatusAsync().ConfigureAwait(false);
                    throw new ConnectionException($"Failed to connect to any of the parent candidates.");
                }

                ParentConnection = await parentTask.ConfigureAwait(false);
                Diagnostic.Debug($"Adopted distributed parent {ParentConnection.Username} ({ParentConnection.IPAddress}:{ParentConnection.Port})");

                cts.Cancel();
                pendingConnections.Remove(ParentConnection);

                foreach (var connection in pendingConnections)
                {
                    connection.Dispose();
                }

                await WriteStatusAsync().ConfigureAwait(false);
            }
        }

        public async Task BroadcastAsync(byte[] bytes)
        {
            var tasks = ChildConnections.Values.Select(async c =>
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

        public async Task SetBranchLevel(int level)
        {
            BranchLevel = level;
            await WriteStatusAsync().ConfigureAwait(false);
        }

        public async Task SetBranchRoot(string username)
        {
            BranchRoot = username;
            await WriteStatusAsync().ConfigureAwait(false);
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

        private async Task WriteStatusAsync()
        {
            // special thanks to @misterhat and livelook (https://github.com/misterhat/livelook) for guidance
            var server = SoulseekClient.ServerConnection;

            await server.WriteAsync(new HaveNoParents(!HaveParent).ToByteArray()).ConfigureAwait(false);
            await server.WriteAsync(new ParentsIP(ParentConnection?.IPAddress ?? IPAddress.None).ToByteArray()).ConfigureAwait(false);
            await server.WriteAsync(new BranchLevel(BranchLevel).ToByteArray()).ConfigureAwait(false);
            await server.WriteAsync(new BranchRoot(BranchRoot ?? string.Empty).ToByteArray()).ConfigureAwait(false);
            await server.WriteAsync(new ChildDepth(ChildConnections.Count).ToByteArray()).ConfigureAwait(false);
            await server.WriteAsync(new AcceptChildren(CanAcceptChildren).ToByteArray()).ConfigureAwait(false);

            await BroadcastAsync(new DistributedBranchLevel(BranchLevel).ToByteArray()).ConfigureAwait(false);
            await BroadcastAsync(new DistributedBranchRoot(BranchRoot ?? string.Empty).ToByteArray()).ConfigureAwait(false);

            if (HaveParent)
            {
                await ParentConnection.WriteAsync(new DistributedChildDepth(ChildConnections.Count).ToByteArray()).ConfigureAwait(false);
            }

            var sb = new StringBuilder("Updated distributed status; ");
            sb
                .Append($"HaveNoParents: {!HaveParent}, ")
                .Append($"ParentsIP: {ParentConnection?.IPAddress ?? IPAddress.None}, ")
                .Append($"BranchLevel: {BranchLevel}, BranchRoot: {BranchRoot ?? string.Empty}, ")
                .Append($"ChildDepth: {ChildConnections.Count}, AcceptChildren: {CanAcceptChildren}");

            Diagnostic.Debug(sb.ToString());
        }
    }
}