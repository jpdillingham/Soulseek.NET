// <copyright file="DistributedConnectionManager.cs" company="JP Dillingham">
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
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Manages distributed <see cref="IMessageConnection"/> instances for the application.
    /// </summary>
    internal sealed class DistributedConnectionManager : IDistributedConnectionManager
    {
        private readonly object parentCandidateSyncRoot = new object();
        private readonly object statusSyncRoot = new object();

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedConnectionManager"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="connectionFactory">The IConnectionFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public DistributedConnectionManager(
            SoulseekClient soulseekClient,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient;

            ConcurrentChildLimit = SoulseekClient?.Options?.ConcurrentDistributedChildrenLimit
                ?? new SoulseekClientOptions().ConcurrentDistributedChildrenLimit;

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new SoulseekClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            StatusTimer = new SystemTimer()
            {
                Enabled = true,
                Interval = 30000,
            };

            StatusTimer.Elapsed += async (sender, e) =>
            {
                await UpdateStatusAsync().ConfigureAwait(false);
            };

            ParentWatchdogTimer = new SystemTimer()
            {
                Enabled = false,
                AutoReset = false,
                Interval = SoulseekClient.Options.DistributedConnectionOptions.InactivityTimeout * 1000,
            };

            ParentWatchdogTimer.Elapsed += (sender, e) => ParentConnection?.Disconnect($"Inactivity timeout of {SoulseekClient.Options.DistributedConnectionOptions.InactivityTimeout} seconds was reached; no broadcastable messages recieved");
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Gets the current distributed branch level.
        /// </summary>
        public int BranchLevel { get; private set; } = 0;

        /// <summary>
        ///     Gets the current distributed branch root.
        /// </summary>
        public string BranchRoot { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets a value indicating whether child connections can be accepted.
        /// </summary>
        public bool CanAcceptChildren => ChildConnectionDictionary.Count < ConcurrentChildLimit;

        /// <summary>
        ///     Gets the number of allowed concurrent child connections.
        /// </summary>
        public int ConcurrentChildLimit { get; }

        /// <summary>
        ///     Gets a value indicating whether a parent connection is established.
        /// </summary>
        public bool HasParent => ParentConnection != null && ParentConnection?.State == ConnectionState.Connected;

        /// <summary>
        ///     Gets the current parent connection.
        /// </summary>
        public (string Username, IPEndPoint IPEndPoint) Parent =>
            ParentConnection == null ? (string.Empty, null) : (ParentConnection.Username, ParentConnection.IPEndPoint);

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        public IReadOnlyDictionary<int, string> PendingSolicitations => new ReadOnlyDictionary<int, string>(PendingSolicitationDictionary);

        private ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>> ChildConnectionDictionary { get; set; } = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private List<IMessageConnection> ParentCandidateConnections { get; } = new List<IMessageConnection>();
        private IMessageConnection ParentConnection { get; set; }
        private SystemTimer ParentWatchdogTimer { get; }
        private ConcurrentDictionary<int, string> PendingSolicitationDictionary { get; } = new ConcurrentDictionary<int, string>();
        private SoulseekClient SoulseekClient { get; }
        private string StatusHash { get; set; }
        private SystemTimer StatusTimer { get; }

        /// <summary>
        ///     Adds a new child connection using the details in the specified <paramref name="connectToPeerResponse"/> and
        ///     pierces the remote peer's firewall.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context.</returns>
        public async Task AddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var r = connectToPeerResponse;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Inbound child connection solicitation from {r.Username} ({r.IPEndPoint}) for token {r.Token} rejected: limit of {ConcurrentChildLimit} reached");
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            await ChildConnectionDictionary.GetOrAdd(
                connectToPeerResponse.Username,
                key => new Lazy<Task<IMessageConnection>>(() => Task.Run(() => GetConnection()))).Value.ConfigureAwait(false);

            async Task<IMessageConnection> GetConnection()
            {
                Diagnostic.Debug($"Attempting indirect child connection to {r.Username} ({r.IPEndPoint}) for token {r.Token}");

                var connection = ConnectionFactory.GetMessageConnection(
                    r.Username,
                    r.IPEndPoint,
                    SoulseekClient.Options.DistributedConnectionOptions);

                connection.Type = ConnectionTypes.Inbound | ConnectionTypes.Indirect;
                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessageRead;
                connection.Disconnected += ChildConnection_Disconnected;

                try
                {
                    await connection.ConnectAsync().ConfigureAwait(false);

                    var request = new PierceFirewall(r.Token);
                    await connection.WriteAsync(request.ToByteArray()).ConfigureAwait(false);

                    await connection.WriteAsync(GetBranchInformation<MessageCode.Peer>()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Diagnostic.Debug($"Indirect child connection to {r.Username} ({r.IPEndPoint}) discarded: {ex.Message}. (id: {connection.Id})");
                    connection.Dispose();
                    throw;
                }

                Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) added. (type: {connection.Type}, id: {connection.Id})");
                Diagnostic.Info($"Added child connection to {connection.Username} ({connection.IPEndPoint})");

                return connection;
            }
        }

        /// <summary>
        ///     Adds a new child connection from an incoming connection.
        /// </summary>
        /// <param name="username">The username from which the connection originated.</param>
        /// <param name="incomingConnection">The accepted connection.</param>
        /// <returns>The operation context.</returns>
        public async Task AddChildConnectionAsync(string username, IConnection incomingConnection)
        {
            var endpoint = incomingConnection.IPEndPoint;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Direct child connection to {username} ({endpoint}) rejected: limit of {ConcurrentChildLimit} reached.");
                incomingConnection.Dispose();
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            await ChildConnectionDictionary.AddOrUpdate(
                username,
                new Lazy<Task<IMessageConnection>>(() => Task.Run(() => GetConnection())),
                (key, cachedConnectionRecord) => new Lazy<Task<IMessageConnection>>(() => Task.Run(() => GetConnection(cachedConnectionRecord)))).Value.ConfigureAwait(false);

            async Task<IMessageConnection> GetConnection(Lazy<Task<IMessageConnection>> cachedConnectionRecord = null)
            {
                Diagnostic.Debug($"Inbound child connection to {username} ({incomingConnection.IPEndPoint}) accepted. (type: {incomingConnection.Type}, id: {incomingConnection.Id}");

                var connection = ConnectionFactory.GetMessageConnection(
                    username,
                    endpoint,
                    SoulseekClient.Options.DistributedConnectionOptions,
                    incomingConnection.HandoffTcpClient());

                connection.Type = ConnectionTypes.Inbound | ConnectionTypes.Direct;
                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessageRead;

                Diagnostic.Debug($"Inbound child connection to {username} ({connection.IPEndPoint}) handed off. (old: {incomingConnection.Id}, new: {connection.Id})");

                if (cachedConnectionRecord != null)
                {
                    var cachedConnection = await cachedConnectionRecord.Value.ConfigureAwait(false);
                    cachedConnection.Disconnected -= ChildConnection_Disconnected;
                    Diagnostic.Debug($"Superceding cached child connection to {username} ({cachedConnection.IPEndPoint}) (old: {cachedConnection.Id}, new: {connection.Id}");
                    cachedConnection.Disconnect("Superceded.");
                    cachedConnection.Dispose();
                }

                try
                {
                    connection.StartReadingContinuously();

                    await connection.WriteAsync(GetBranchInformation<MessageCode.Peer>()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Diagnostic.Debug($"Inbound child connection to {username} ({connection.IPEndPoint}) discarded: {ex.Message}. (id: {connection.Id})");
                    connection.Dispose();
                    throw;
                }

                Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) added. (type: {connection.Type}, id: {connection.Id})");
                Diagnostic.Info($"Added child connection to {connection.Username} ({connection.IPEndPoint})");

                return connection;
            }
        }

        /// <summary>
        ///     Asynchronously connects to one of the specified <paramref name="parentCandidates"/>.
        /// </summary>
        /// <param name="parentCandidates">The list of parent connection candidates provided by the server.</param>
        /// <returns>The operation context.</returns>
        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPEndPoint IPEndPoint)> parentCandidates)
        {
            if (HasParent || !parentCandidates.Any())
            {
                return;
            }

            Diagnostic.Info($"Attempting to select a new parent connection from {parentCandidates.Count()} candidates");

            using (var cts = new CancellationTokenSource())
            {
                var pendingConnectTasks = parentCandidates.Select(p => GetParentConnectionAsync(p.Username, p.IPEndPoint, cts.Token)).ToList();
                Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)> parentTask;

                do
                {
                    parentTask = await Task.WhenAny(pendingConnectTasks).ConfigureAwait(false);
                    pendingConnectTasks.Remove(parentTask);
                }
                while (parentTask.Status != TaskStatus.RanToCompletion && pendingConnectTasks.Count > 0);

                if (parentTask.Status != TaskStatus.RanToCompletion)
                {
                    var msg = "Failed to connect to any of the distributed parent candidates";
                    Diagnostic.Warning(msg);
                    await UpdateStatusAsync().ConfigureAwait(false);
                    throw new ConnectionException(msg);
                }

                (ParentConnection, BranchLevel, BranchRoot) = await parentTask.ConfigureAwait(false);

                ParentConnection.Disconnected += ParentConnection_Disconnected;
                ParentConnection.Disconnected -= ParentCandidateConnection_Disconnected;

                Diagnostic.Debug($"Adopted parent {ParentConnection.Username} ({ParentConnection.IPEndPoint}) (id: {ParentConnection.Id})");
                Diagnostic.Info($"Adopted parent {ParentConnection.Username} ({ParentConnection.IPEndPoint})");

                cts.Cancel();
                PendingSolicitationDictionary.Clear();

                lock (parentCandidateSyncRoot)
                {
                    ParentCandidateConnections.Remove(ParentConnection);

                    foreach (var connection in ParentCandidateConnections)
                    {
                        connection.Dispose();
                    }

                    ParentCandidateConnections.Clear();
                }

                await UpdateStatusAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Asynchronously writes the specified bytes to each of the connected child connections.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context.</returns>
        public async Task BroadcastMessageAsync(byte[] bytes, CancellationToken? cancellationToken = null)
        {
            ParentWatchdogTimer?.Reset();

            var tasks = ChildConnectionDictionary.Values.Select(async c =>
            {
                IMessageConnection connection = default;

                try
                {
                    await (await c.Value.ConfigureAwait(false)).WriteAsync(bytes, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    connection?.Dispose();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IDistributedConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Removes and disposes all active and queued connections.
        /// </summary>
        public async void RemoveAndDisposeAll()
        {
            PendingSolicitationDictionary.Clear();
            ParentConnection?.Dispose();

            while (!ChildConnectionDictionary.IsEmpty)
            {
                if (ChildConnectionDictionary.TryRemove(ChildConnectionDictionary.Keys.First(), out var value))
                {
                    (await value.Value.ConfigureAwait(false))?.Dispose();
                }
            }
        }

        /// <summary>
        ///     Sets the distributed <paramref name="branchLevel"/>.
        /// </summary>
        /// <param name="branchLevel">The distributed branch level.</param>
        public void SetBranchLevel(int branchLevel)
        {
            BranchLevel = branchLevel;
        }

        /// <summary>
        ///     Sets the distributed <paramref name="branchRoot"/>.
        /// </summary>
        /// <param name="branchRoot">The distributed branch root.</param>
        public void SetBranchRoot(string branchRoot)
        {
            BranchRoot = branchRoot;
        }

        private void ChildConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;
            ChildConnectionDictionary.TryRemove(connection.Username, out _);
            Diagnostic.Debug($"Child {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (id: {connection.Id})");
            Diagnostic.Info($"Removed child connection to {connection.Username} ({connection.IPEndPoint}): {e.Message}");
            connection.Dispose();

            UpdateStatusAsync().ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    StatusTimer?.Dispose();
                    ParentWatchdogTimer?.Dispose();
                    RemoveAndDisposeAll();
                }

                Disposed = true;
            }
        }

        private byte[] GetBranchInformation<T>()
        {
            var branchLevel = HasParent ? BranchLevel + 1 : 0;
            var branchRoot = HasParent ? BranchRoot : string.Empty;

            var isPeer = typeof(T) == typeof(MessageCode.Peer);
            var payload = new List<byte>();

            payload.AddRange(isPeer ? new DistributedBranchLevel(branchLevel).ToByteArray() :
                new BranchLevelCommand(branchLevel).ToByteArray());

            if (!string.IsNullOrEmpty(branchRoot))
            {
                payload.AddRange(isPeer ? new DistributedBranchRoot(branchRoot).ToByteArray() :
                    new BranchRootCommand(branchRoot).ToByteArray());
            }

            return payload.ToArray();
        }

        private async Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)> GetParentConnectionAsync(string username, IPEndPoint ipEndPoint, CancellationToken cancellationToken)
        {
            using (var directCts = new CancellationTokenSource())
            using (var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token))
            using (var indirectCts = new CancellationTokenSource())
            using (var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token))
            {
                var direct = GetParentConnectionDirectAsync(username, ipEndPoint, directLinkedCts.Token);
                var indirect = GetParentConnectionIndirectAsync(username, indirectLinkedCts.Token);

                var tasks = new[] { direct, indirect }.ToList();
                Task<IMessageConnection> task;

                do
                {
                    task = await Task.WhenAny(tasks).ConfigureAwait(false);
                    tasks.Remove(task);
                }
                while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);

                if (task.Status != TaskStatus.RanToCompletion)
                {
                    throw new ConnectionException($"Failed to establish a distributed parent connection to {username} ({ipEndPoint})");
                }

                var connection = await task.ConfigureAwait(false);
                var isDirect = task == direct;

                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessageRead;

                Diagnostic.Debug($"{connection.Type} parent candidate connection to {username} ({ipEndPoint}) established.  Waiting for branch information and first SearchRequest message. (id: {connection.Id})");
                (isDirect ? indirectCts : directCts).Cancel();

                if (!isDirect)
                {
                    connection.StartReadingContinuously();
                }

                var branchLevelWait = SoulseekClient.Waiter.Wait<int>(new WaitKey(Constants.WaitKey.BranchLevelMessage, connection.Id), cancellationToken: cancellationToken);
                var branchRootWait = SoulseekClient.Waiter.Wait<string>(new WaitKey(Constants.WaitKey.BranchRootMessage, connection.Id), cancellationToken: cancellationToken);
                var searchWait = SoulseekClient.Waiter.Wait(new WaitKey(Constants.WaitKey.SearchRequestMessage, connection.Id), cancellationToken: cancellationToken);

                // wait for the branch level and first search request. branch roots will not send the root.
                var waits = new[] { branchLevelWait, searchWait }.ToList();
                var waitsTask = Task.WhenAll(waits);

                int? branchLevel = default;
                string branchRoot;

                try
                {
                    await waitsTask.ConfigureAwait(false);

                    branchLevel = await branchLevelWait.ConfigureAwait(false);

                    // if we didn't connect to a root, ensure we get the name of the root.
                    if (branchLevel > 0)
                    {
                        branchRoot = await branchRootWait.ConfigureAwait(false);
                    }
                    else
                    {
                        Diagnostic.Debug($"Received branch level 0 from {username}; this user is a branch root");
                        branchRoot = username;
                    }

                    await searchWait.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    connection.Disconnect("One or more required messages was not received");
                    connection.Dispose();

                    throw new ConnectionException($"Failed to initialize parent connection to {username} ({ipEndPoint}); one or more required messages was not received. (id: {connection.Id})");
                }

                Diagnostic.Debug($"Received branch level {branchLevel}, root {branchRoot} and first search request from {username} ({ipEndPoint}) (id: {connection.Id})");

                return (connection, branchLevel.Value, branchRoot);
            }
        }

        private async Task<IMessageConnection> GetParentConnectionDirectAsync(string username, IPEndPoint ipEndPoint, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetMessageConnection(username, ipEndPoint, SoulseekClient.Options.DistributedConnectionOptions);
            connection.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct;
            connection.Disconnected += ParentCandidateConnection_Disconnected;

            try
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
                await connection.WriteAsync(new PeerInit(SoulseekClient.Username, Constants.ConnectionType.Distributed, SoulseekClient.GetNextToken()).ToByteArray(), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            lock (parentCandidateSyncRoot)
            {
                ParentCandidateConnections.Add(connection);
            }

            Diagnostic.Debug($"{connection.Type} parent candidate connection to {connection.Username} ({connection.IPEndPoint}) connected. (id: {connection.Id})");
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
                        incomingConnection.IPEndPoint,
                        SoulseekClient.Options.DistributedConnectionOptions,
                        incomingConnection.HandoffTcpClient());

                    connection.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect;
                    connection.Disconnected += ParentCandidateConnection_Disconnected;

                    lock (parentCandidateSyncRoot)
                    {
                        ParentCandidateConnections.Add(connection);
                    }

                    Diagnostic.Debug($"{connection.Type} parent candidate connection to {connection.Username} ({connection.IPEndPoint}) connected. (id: {connection.Id})");

                    return connection;
                }
            }
            finally
            {
                PendingSolicitationDictionary.TryRemove(token, out var _);
            }
        }

        private void ParentCandidateConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;

            Diagnostic.Debug($"{connection.Type} Parent candidate {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? string.Empty : $": {e.Message}")}. (id: {connection.Id})");
            connection.Dispose();
        }

        private void ParentConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;
            Diagnostic.Info($"Parent {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? "." : $": {e.Message}")}. (id: {connection.Id})");
            ParentConnection = null;
            BranchLevel = 0;
            BranchRoot = string.Empty;

            connection.Dispose();

            UpdateStatusAsync().ConfigureAwait(false);
        }

        private async Task UpdateStatusAsync()
        {
            if (!SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || (!SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn)))
            {
                return;
            }

            // special thanks to @misterhat and livelook (https://github.com/misterhat/livelook) for guidance
            var payload = new List<byte>();

            var haveNoParents = !HasParent;
            var parentsIp = HasParent ? ParentConnection?.IPEndPoint?.Address ?? IPAddress.None : IPAddress.None;
            var branchLevel = HasParent ? BranchLevel + 1 : 0;
            var branchRoot = HasParent ? BranchRoot : string.Empty;

            payload.AddRange(new HaveNoParentsCommand(haveNoParents).ToByteArray());
            payload.AddRange(new ParentsIPCommand(parentsIp).ToByteArray());
            payload.AddRange(GetBranchInformation<MessageCode.Server>());
            payload.AddRange(new ChildDepthCommand(ChildConnectionDictionary.Count).ToByteArray());
            payload.AddRange(new AcceptChildrenCommand(CanAcceptChildren).ToByteArray());

            var statusHash = Convert.ToBase64String(payload.ToArray());

            lock (statusSyncRoot)
            {
                if (statusHash == StatusHash && HasParent)
                {
                    return;
                }

                StatusHash = statusHash;
            }

            try
            {
                await SoulseekClient.ServerConnection.WriteAsync(payload.ToArray()).ConfigureAwait(false);

                await BroadcastMessageAsync(GetBranchInformation<MessageCode.Peer>()).ConfigureAwait(false);

                if (HasParent)
                {
                    await ParentConnection.WriteAsync(new DistributedChildDepth(ChildConnectionDictionary.Count).ToByteArray()).ConfigureAwait(false);
                }

                var sb = new StringBuilder("Updated distributed status; ");
                sb
                    .Append($"HaveNoParents: {haveNoParents}, ")
                    .Append($"ParentsIP: {parentsIp}, ")
                    .Append($"BranchLevel: {branchLevel}, BranchRoot: {branchRoot}, ")
                    .Append($"ChildDepth: {ChildConnectionDictionary.Count}, AcceptChildren: {CanAcceptChildren}");

                Diagnostic.Debug(sb.ToString());
            }
            catch (Exception ex)
            {
                var msg = $"Failed to update distributed status: {ex.Message}";

                if (SoulseekClient.State != SoulseekClientStates.Disconnected)
                {
                    Diagnostic.Warning(msg, ex);
                }
                else
                {
                    Diagnostic.Debug(msg, ex);
                }
            }
        }
    }
}