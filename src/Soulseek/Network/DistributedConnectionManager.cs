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
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Manages distributed <see cref="IMessageConnection"/> instances for the application.
    /// </summary>
    internal sealed class DistributedConnectionManager : IDistributedConnectionManager
    {
        public DistributedConnectionManager(
            SoulseekClient soulseekClient,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient;

            ConcurrentChildrenConnectionLimit = SoulseekClient?.Options?.ConcurrentDistributedChildrenLimit
                ?? new ClientOptions().ConcurrentDistributedChildrenLimit;

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new ClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            StatusTimer = new SystemTimer()
            {
                Enabled = true,
                Interval = 5000,
            };

            StatusTimer.Elapsed += async (sender, e) =>
            {
                await UpdateStatusAsync();
            };

            ParentWatchdogTimer = new SystemTimer()
            {
                Enabled = false,
                AutoReset = false,
                Interval = SoulseekClient.Options.DistributedConnectionOptions.InactivityTimeout * 1000,
            };

            ParentWatchdogTimer.Elapsed += (sender, e) => ParentConnection.Disconnect($"Inactivity timeout of {SoulseekClient.Options.DistributedConnectionOptions.InactivityTimeout} seconds was reached; no broadcastable messages recieved");
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Gets the number of allowed concurrent child connections.
        /// </summary>
        public int ConcurrentChildrenConnectionLimit { get; }

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        public IReadOnlyDictionary<int, string> PendingSolicitations => new ReadOnlyDictionary<int, string>(PendingSolicitationDictionary);

        private ConcurrentDictionary<string, (string BranchRoot, int BranchLevel)> BranchInfo { get; } = new ConcurrentDictionary<string, (string BranchRoot, int BranchLevel)>();
        private bool CanAcceptChildren => ChildConnections.Count < ConcurrentChildrenConnectionLimit;
        private ConcurrentDictionary<string, IMessageConnection> ChildConnections { get; } = new ConcurrentDictionary<string, IMessageConnection>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private bool HaveParent => ParentConnection != null && ParentConnection.State == ConnectionState.Connected;
        private List<IMessageConnection> ParentCandidateConnections { get; } = new List<IMessageConnection>();
        private object ParentCandidateSyncRoot { get; } = new object();
        private IMessageConnection ParentConnection { get; set; }
        private SystemTimer ParentWatchdogTimer { get; }
        private ConcurrentDictionary<int, string> PendingSolicitationDictionary { get; } = new ConcurrentDictionary<int, string>();
        private SoulseekClient SoulseekClient { get; }
        private string StatusHash { get; set; }
        private object StatusSyncRoot { get; } = new object();
        private SystemTimer StatusTimer { get; }

        public async Task AddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var r = connectToPeerResponse;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Child connection to {r.Username} ({r.IPAddress}:{r.Port}) rejected: limit of {ConcurrentChildrenConnectionLimit} reached");
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            var connection = ConnectionFactory.GetMessageConnection(
                r.Username,
                r.IPAddress,
                r.Port,
                SoulseekClient.Options.DistributedConnectionOptions);

            connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessage;
            connection.Disconnected += ChildConnection_Disconnected;

            Diagnostic.Debug($"Attempting child connection to {r.Username} ({r.IPAddress}:{r.Port})");

            try
            {
                await connection.ConnectAsync().ConfigureAwait(false);

                var childDepthWait = SoulseekClient.Waiter.Wait<int>(new WaitKey(Constants.WaitKey.ChildDepthMessage, connection.Key));

                var request = new PierceFirewallRequest(r.Token);
                await connection.WriteAsync(request.ToByteArray()).ConfigureAwait(false);

                await childDepthWait.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                connection.Disconnected -= ChildConnection_Disconnected;
                Diagnostic.Debug($"Discarded child connection to {r.Username} ({r.IPAddress}:{r.Port}): {ex.Message}");
                connection.Dispose();
                BranchInfo.TryRemove(r.Username, out _);
                throw;
            }

            AddOrUpdateChildConnectionRecord(connection);

            Diagnostic.Info($"Added child {connection.Username} ({connection.IPAddress}:{connection.Port})");
        }

        public async Task AddChildConnectionAsync(string username, ITcpClient tcpClient)
        {
            var endpoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Child connection to {username} ({endpoint.Address}:{endpoint.Port}) rejected: limit of {ConcurrentChildrenConnectionLimit} reached");
                tcpClient.Dispose();
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            var connection = ConnectionFactory.GetMessageConnection(
                username,
                endpoint.Address,
                endpoint.Port,
                SoulseekClient.Options.DistributedConnectionOptions,
                tcpClient);

            connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessage;
            connection.Disconnected += ChildConnection_Disconnected;

            Diagnostic.Debug($"Accepted child connection to {username} ({endpoint.Address}:{endpoint.Port})");

            var childDepthWait = SoulseekClient.Waiter.Wait<int>(new WaitKey(Constants.WaitKey.ChildDepthMessage, connection.Key));

            connection.StartReadingContinuously();

            try
            {
                await childDepthWait.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                connection.Disconnected -= ChildConnection_Disconnected;
                Diagnostic.Debug($"Discarded child connection to {username} ({connection.IPAddress}:{connection.Port}): {ex.Message}");
                connection.Dispose();
                BranchInfo.TryRemove(username, out _);
                throw;
            }

            AddOrUpdateChildConnectionRecord(connection);

            Diagnostic.Info($"Added child {username} ({connection.IPAddress}:{connection.Port})");
        }

        public void AddOrUpdateBranchLevel(string username, int level)
        {
            // it seems like maybe we should add one to this before sending it to our children?
            BranchInfo.AddOrUpdate(username, (null, level), (k, v) =>
            {
                return (v.BranchRoot, level);
            });
        }

        public void AddOrUpdateBranchRoot(string username, string root)
        {
            BranchInfo.AddOrUpdate(username, (root, 0), (k, v) =>
            {
                return (root, v.BranchLevel);
            });
        }

        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parentCandidates)
        {
            if (HaveParent)
            {
                return;
            }

            using (var cts = new CancellationTokenSource())
            {
                var pendingConnectTasks = parentCandidates.Select(p => GetParentConnectionAsync(p.Username, p.IPAddress, p.Port, cts.Token)).ToList();
                Task<IMessageConnection> parentTask;

                do
                {
                    parentTask = await Task.WhenAny(pendingConnectTasks).ConfigureAwait(false);
                    pendingConnectTasks.Remove(parentTask);
                }
                while (parentTask.IsFaulted && pendingConnectTasks.Count > 0);

                if (parentTask.IsFaulted)
                {
                    Diagnostic.Warning($"Failed to connect to any of the distributed parent candidates");
                    await UpdateStatusAsync().ConfigureAwait(false);
                }

                ParentConnection = await parentTask.ConfigureAwait(false);

                ParentConnection.Disconnected += ParentConnection_Disconnected;
                ParentConnection.Disconnected -= ParentCandidateConnection_Disconnected;

                Diagnostic.Info($"Adopted parent {ParentConnection.Username} ({ParentConnection.IPAddress}:{ParentConnection.Port})");

                cts.Cancel();
                PendingSolicitationDictionary.Clear();

                lock (ParentCandidateSyncRoot)
                {
                    ParentCandidateConnections.Remove(ParentConnection);

                    foreach (var connection in ParentCandidateConnections)
                    {
                        BranchInfo.TryRemove(connection.Username, out _);
                        connection.Dispose();
                    }

                    ParentCandidateConnections.Clear();
                }

                await UpdateStatusAsync().ConfigureAwait(false);
            }
        }

        public async Task BroadcastMessageAsync(byte[] bytes)
        {
            ParentWatchdogTimer?.Reset();

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

        private void AddOrUpdateChildConnectionRecord(IMessageConnection connection)
        {
            ChildConnections.AddOrUpdate(connection.Username, connection, (k, v) =>
            {
                // suppress deletion from dictionary and server child count update by removing this
                v.Disconnected -= ChildConnection_Disconnected;
                v.Disconnect("Replaced with a newer connection");
                v.Dispose();

                Diagnostic.Debug($"Replaced existing child connection for {connection.Username} ({connection.IPAddress}:{connection.Port})");
                return connection;
            });
        }

        private void ChildConnection_Disconnected(object sender, string message)
        {
            var connection = (IMessageConnection)sender;
            ChildConnections.TryRemove(connection.Username, out _);
            Diagnostic.Debug($"Child {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected: {message}");
            connection.Dispose();

            UpdateStatusAsync().ConfigureAwait(false);
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

        private (string BranchRoot, int BranchLevel) GetBranchInfo()
        {
            if (HaveParent && BranchInfo.TryGetValue(ParentConnection.Username, out var info))
            {
                return (info.BranchRoot, info.BranchLevel);
            }

            return (null, 0);
        }

        private async Task<IMessageConnection> GetParentConnectionAsync(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            void HandleFirstSearchRequest(object sender, byte[] message)
            {
                try
                {
                    var connection = (IMessageConnection)sender;
                    DistributedSearchRequest.FromByteArray(message);

                    SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SearchRequestMessage, connection.Context, connection.Key));
                }
                catch
                {
                }
            }

            using (var directCts = new CancellationTokenSource())
            using (var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token))
            using (var indirectCts = new CancellationTokenSource())
            using (var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token))
            {
                var direct = GetParentConnectionDirectAsync(username, ipAddress, port, directLinkedCts.Token);
                var indirect = GetParentConnectionIndirectAsync(username, indirectLinkedCts.Token);

                var tasks = new[] { direct, indirect }.ToList();
                Task<IMessageConnection> task;
                do
                {
                    task = await Task.WhenAny(direct, indirect).ConfigureAwait(false);
                    tasks.Remove(task);
                }
                while (task.IsFaulted && tasks.Count > 0);

                var connection = await task.ConfigureAwait(false);
                var isDirect = task == direct;

                Diagnostic.Debug($"+++++++++++++++++++++++++++++++++++ {(isDirect ? "Direct" : "Indirect")} connection to {username} ({ipAddress}:{port}) established; cancelling {(isDirect ? "indirect" : "direct")} connection.");
                (isDirect ? indirectCts : directCts).Cancel();

                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessage;
                connection.MessageRead += HandleFirstSearchRequest;

                if (!isDirect)
                {
                    connection.StartReadingContinuously();
                }

                await SoulseekClient.Waiter.Wait(new WaitKey(Constants.WaitKey.SearchRequestMessage, connection.Context, connection.Key)).ConfigureAwait(false);

                connection.MessageRead -= HandleFirstSearchRequest;

                return connection;
            }
        }

        private async Task<IMessageConnection> GetParentConnectionDirectAsync(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetMessageConnection(username, ipAddress, port, SoulseekClient.Options.DistributedConnectionOptions);
            connection.Disconnected += ParentCandidateConnection_Disconnected;
            connection.Context = Constants.ConnectionMethod.Direct;

            try
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            await connection.WriteAsync(new PeerInitRequest(SoulseekClient.Username, Constants.ConnectionType.Distributed, SoulseekClient.GetNextToken()).ToByteArray(), cancellationToken).ConfigureAwait(false);

            lock (ParentCandidateSyncRoot)
            {
                ParentCandidateConnections.Add(connection);
            }

            Diagnostic.Debug($"Direct connection to parent candidate {connection.Username} ({connection.IPAddress}:{connection.Port}) connected.");
            return connection;
        }

        private async Task<IMessageConnection> GetParentConnectionIndirectAsync(string username, CancellationToken cancellationToken)
        {
            var token = SoulseekClient.GetNextToken();

            try
            {
                PendingSolicitationDictionary.TryAdd(token, username);

                await SoulseekClient.ServerConnection
                    .WriteAsync(new ConnectToPeerRequest(token, username, Constants.ConnectionType.Distributed).ToByteArray(), cancellationToken)
                    .ConfigureAwait(false);

                using (var incomingConnection = await SoulseekClient.Waiter
                    .Wait<IConnection>(new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, username, token), null, cancellationToken)
                    .ConfigureAwait(false))
                {
                    var connection = ConnectionFactory.GetMessageConnection(
                        username,
                        incomingConnection.IPAddress,
                        incomingConnection.Port,
                        SoulseekClient.Options.PeerConnectionOptions,
                        incomingConnection.HandoffTcpClient());

                    connection.Disconnected += ParentCandidateConnection_Disconnected;
                    connection.Context = Constants.ConnectionMethod.Indirect;

                    lock (ParentCandidateSyncRoot)
                    {
                        ParentCandidateConnections.Add(connection);
                    }

                    Diagnostic.Debug($"Indirect connection to parent candidate {connection.Username} ({connection.IPAddress}:{connection.Port}) connected.");

                    return connection;
                }
            }
            finally
            {
                PendingSolicitationDictionary.TryRemove(token, out var _);
            }
        }

        private void ParentCandidateConnection_Disconnected(object sender, string message)
        {
            var connection = (IMessageConnection)sender;

            Diagnostic.Debug($"Parent candidate {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected: {message}");
            connection.Dispose();
        }

        private void ParentConnection_Disconnected(object sender, string message)
        {
            var connection = (IMessageConnection)sender;
            Diagnostic.Debug($"Parent {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected{(message == null ? "." : $": {message}.")}");
            ParentConnection = null;
            BranchInfo.Clear();

            connection.Dispose();

            UpdateStatusAsync().ConfigureAwait(false);
        }

        private async Task UpdateStatusAsync()
        {
            // special thanks to @misterhat and livelook (https://github.com/misterhat/livelook) for guidance
            var branchInfo = GetBranchInfo();
            var payload = new List<byte>();

            payload.AddRange(new HaveNoParents(!HaveParent).ToByteArray());
            payload.AddRange(new ParentsIP(ParentConnection?.IPAddress ?? IPAddress.None).ToByteArray());
            payload.AddRange(new BranchLevel(branchInfo.BranchLevel).ToByteArray());
            payload.AddRange(new BranchRoot(branchInfo.BranchRoot ?? string.Empty).ToByteArray());
            payload.AddRange(new ChildDepth(ChildConnections.Count).ToByteArray());
            payload.AddRange(new AcceptChildren(CanAcceptChildren).ToByteArray());

            var statusHash = Convert.ToBase64String(payload.ToArray());

            lock (StatusSyncRoot)
            {
                if (statusHash == StatusHash && HaveParent)
                {
                    return;
                }

                StatusHash = statusHash;
            }

            var server = SoulseekClient.ServerConnection;
            await server.WriteAsync(payload.ToArray()).ConfigureAwait(false);

            await BroadcastMessageAsync(new DistributedBranchLevel(branchInfo.BranchLevel).ToByteArray()).ConfigureAwait(false);
            await BroadcastMessageAsync(new DistributedBranchRoot(branchInfo.BranchRoot ?? string.Empty).ToByteArray()).ConfigureAwait(false);

            if (HaveParent)
            {
                await ParentConnection.WriteAsync(new DistributedChildDepth(ChildConnections.Count).ToByteArray()).ConfigureAwait(false);
            }

            var sb = new StringBuilder("Updated distributed status; ");
            sb
                .Append($"HaveNoParents: {!HaveParent}, ")
                .Append($"ParentsIP: {ParentConnection?.IPAddress ?? IPAddress.None}, ")
                .Append($"BranchLevel: {branchInfo.BranchLevel}, BranchRoot: {branchInfo.BranchRoot ?? string.Empty}, ")
                .Append($"ChildDepth: {ChildConnections.Count}, AcceptChildren: {CanAcceptChildren}");

            Diagnostic.Debug(sb.ToString());
        }
    }
}