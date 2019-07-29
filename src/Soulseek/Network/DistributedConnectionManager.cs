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

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        public IReadOnlyDictionary<int, string> PendingSolicitations => new ReadOnlyDictionary<int, string>(PendingSolicitationDictionary);
        private ConcurrentDictionary<int, string> PendingSolicitationDictionary { get; } = new ConcurrentDictionary<int, string>();

        private object ParentCandidateSyncRoot { get; } = new object();
        private List<IMessageConnection> ParentCandidateConnections { get; } = new List<IMessageConnection>();

        private int BranchLevel => GetBranchInfo().BranchLevel;
        private string BranchRoot => GetBranchInfo().BranchRoot;
        private bool CanAcceptChildren => ChildConnections.Count < SoulseekClient.Options.ConcurrentDistributedChildrenLimit;
        private ConcurrentDictionary<string, IMessageConnection> ChildConnections { get; } = new ConcurrentDictionary<string, IMessageConnection>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private bool HaveParent => ParentConnection != null && ParentConnection.State == ConnectionState.Connected;
        private IMessageConnection ParentConnection { get; set; }
        private SoulseekClient SoulseekClient { get; }
        private string StatusHash { get; set; }
        private object StatusSyncRoot { get; } = new object();

        private ConcurrentDictionary<string, (string BranchRoot, int BranchLevel)> BranchInfo { get; } = new ConcurrentDictionary<string, (string BranchRoot, int BranchLevel)>();

        private (string BranchRoot, int BranchLevel) GetBranchInfo()
        {
            if (HaveParent && BranchInfo.TryGetValue(ParentConnection.Username, out var info))
            {
                return (info.BranchRoot, info.BranchLevel);
            }

            return (null, 0);
        }

        public async Task AddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var r = connectToPeerResponse;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Distributed child {r.Username} ({r.IPAddress}:{r.Port}) rejected; limit reached.");
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            var connection = ConnectionFactory.GetMessageConnection(
                r.Username,
                r.IPAddress,
                r.Port,
                SoulseekClient.Options.DistributedConnectionOptions);

            connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleChildMessage;
            connection.Disconnected += ChildConnection_Disconnected;

            try
            {
                await connection.ConnectAsync().ConfigureAwait(false);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            var request = new PierceFirewallRequest(r.Token);
            await connection.WriteAsync(request.ToByteArray()).ConfigureAwait(false);

            AddOrUpdateChildConnectionRecord(connection);

            Diagnostic.Debug($"Added distributed child {connection.Username} ({connection.IPAddress}:{connection.Port}).  Total children: {ChildConnections.Count}");

            await UpdateStatusAsync().ConfigureAwait(false);
        }

        private void AddOrUpdateChildConnectionRecord(IMessageConnection connection)
        {
            ChildConnections.AddOrUpdate(connection.Username, connection, (k, v) =>
            {
                // suppress deletion from dictionary and server child count update by removing this
                v.Disconnected -= ChildConnection_Disconnected;
                v.Disconnect("Replaced with a newer connection.");
                v.Dispose();

                Diagnostic.Debug($"Replaced existing distributed child connection for {connection.Username} ({connection.IPAddress}:{connection.Port}).");
                return connection;
            });
        }

        public async Task AddChildConnectionAsync(string username, ITcpClient tcpClient)
        {
            var endpoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Distributed child {username} ({endpoint.Address}:{endpoint.Port}) rejected; limit reached.");
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

            connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleChildMessage;
            connection.Disconnected += ChildConnection_Disconnected;

            connection.StartReadingContinuously();

            AddOrUpdateChildConnectionRecord(connection);

            Diagnostic.Debug($"Added distributed child {username} ({connection.IPAddress}:{connection.Port}).  Total children: {ChildConnections.Count}");

            await UpdateStatusAsync().ConfigureAwait(false);
        }

        private void ChildConnection_Disconnected(object sender, string message)
        {
            var connection = (IMessageConnection)sender;
            ChildConnections.TryRemove(connection.Username, out _);
            Diagnostic.Debug($"Distributed child {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected: {message}.  Total children: {ChildConnections.Count}");
            connection.Dispose();

            UpdateStatusAsync().ConfigureAwait(false);
        }

        private async Task<IMessageConnection> GetParentConnectionAsync(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            const string waitKey = "FirstSearchRequest";

            void HandleFirstSearchRequest(object sender, byte[] message)
            {
                try
                {
                    var connection = (IMessageConnection)sender;
                    DistributedSearchRequest.FromByteArray(message);

                    SoulseekClient.Waiter.Complete(new WaitKey(waitKey, connection.Context, connection.Key));
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

                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleParentMessage;
                connection.MessageRead += HandleFirstSearchRequest;

                if (!isDirect)
                {
                    connection.StartReadingContinuously();
                }

                Diagnostic.Debug($"Waiting for first search request");
                await SoulseekClient.Waiter.Wait(new WaitKey(waitKey, connection.Context, connection.Key)).ConfigureAwait(false);
                Diagnostic.Debug($"Got first search request from {username}/{connection.Context}");

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

        private void ParentCandidateConnection_Disconnected(object sender, string message)
        {
            var connection = (IMessageConnection)sender;

            Diagnostic.Debug($"{connection.Context} connection to parent candidate {connection.Username} ({connection.IPAddress}:{connection.Port}) disconnected{(message == null ? "." : $": {message}.")}");
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
                    Console.WriteLine($"Task done, remaining {pendingConnectTasks.Count()}");
                    pendingConnectTasks.Remove(parentTask);
                }
                while (parentTask.IsFaulted && pendingConnectTasks.Count > 0);

                Console.WriteLine($"Done with all the tasks");

                if (parentTask.IsFaulted)
                {
                    Diagnostic.Debug($"Failed to connect to any of the distributed parent candidates; notifying server.");
                    await UpdateStatusAsync().ConfigureAwait(false);
                }

                ParentConnection = await parentTask.ConfigureAwait(false);

                ParentConnection.Disconnected += ParentConnection_Disconnected;
                ParentConnection.Disconnected -= ParentCandidateConnection_Disconnected;

                Diagnostic.Debug($"Adopted distributed parent {ParentConnection.Username} ({ParentConnection.IPAddress}:{ParentConnection.Port})");

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

        public async Task AddOrUpdateBranchLevel(string username, int level)
        {
            // it seems like maybe we should add one to this before sending it to our children?
            BranchInfo.AddOrUpdate(username, (null, level), (k, v) =>
            {
                return (v.BranchRoot, level);
            });

            await UpdateStatusAsync().ConfigureAwait(false);
        }

        public async Task AddOrUpdateBranchRoot(string username, string root)
        {
            BranchInfo.AddOrUpdate(username, (root, 0), (k, v) =>
            {
                return (root, v.BranchLevel);
            });

            await UpdateStatusAsync().ConfigureAwait(false);
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

        private async Task UpdateStatusAsync()
        {
            // special thanks to @misterhat and livelook (https://github.com/misterhat/livelook) for guidance
            var payload = new List<byte>();

            payload.AddRange(new HaveNoParents(!HaveParent).ToByteArray());
            payload.AddRange(new ParentsIP(ParentConnection?.IPAddress ?? IPAddress.None).ToByteArray());
            payload.AddRange(new BranchLevel(BranchLevel).ToByteArray());
            payload.AddRange(new BranchRoot(BranchRoot ?? string.Empty).ToByteArray());
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