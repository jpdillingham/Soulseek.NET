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

            AcceptChildren = SoulseekClient?.Options?.AcceptDistributedChildren ?? new SoulseekClientOptions().AcceptDistributedChildren;
            ConcurrentChildLimit = SoulseekClient?.Options?.DistributedChildLimit
                ?? new SoulseekClientOptions().DistributedChildLimit;

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new SoulseekClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            StatusWatchdogTimer = new SystemTimer()
            {
                Enabled = true,
                AutoReset = true,
                Interval = 300000,
            };

            StatusWatchdogTimer.Elapsed += (sender, e) =>
            {
                Diagnostic.Info($"[{DateTime.Now}] Parent: {HasParent}, last distributed message: {LastSearchRequest}");
                if (!HasParent)
                {
                    Diagnostic.Warning($"No parent connection.");
                    //_ = UpdateStatusAsync();
                }
            };

            ParentWatchdogTimer = new SystemTimer()
            {
                Enabled = false,
                AutoReset = false,
                Interval = SoulseekClient.Options.DistributedConnectionOptions.InactivityTimeout,
            };

            ParentWatchdogTimer.Elapsed += (sender, e) => ParentConnection?.Disconnect($"Inactivity timeout of {SoulseekClient.Options.DistributedConnectionOptions.InactivityTimeout} milliseconds was reached; no broadcastable messages recieved");
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
        public bool CanAcceptChildren => AcceptChildren && ChildConnectionDictionary.Count < ConcurrentChildLimit;

        /// <summary>
        ///     Gets the current list of child connections.
        /// </summary>
        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children => ChildConnectionDictionary.Values
            .Select(async c => await c.Value.ConfigureAwait(false))
            .Select(c => (c.Result.Username, c.Result.IPEndPoint)).ToList().AsReadOnly();

        /// <summary>
        ///     Gets the number of allowed concurrent child connections.
        /// </summary>
        public int ConcurrentChildLimit { get; }

        /// <summary>
        ///     Gets a value indicating whether a parent connection is established.
        /// </summary>
        public bool HasParent => ParentConnection != null && ParentConnection.State == ConnectionState.Connected;

        /// <summary>
        ///     Gets the current parent connection.
        /// </summary>
        public (string Username, IPEndPoint IPEndPoint) Parent =>
            ParentConnection == null ? (string.Empty, null) : (ParentConnection.Username, ParentConnection.IPEndPoint);

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        public IReadOnlyDictionary<int, string> PendingSolicitations => new ReadOnlyDictionary<int, string>(PendingSolicitationDictionary);

        private bool AcceptChildren { get; }
        private ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>> ChildConnectionDictionary { get; set; } = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private DateTime LastSearchRequest { get; set; }
        private List<(string Username, IPEndPoint IPEndPoint)> ParentCandidateList { get; set; } = new List<(string Username, IPEndPoint iPEndPoint)>();
        private IMessageConnection ParentConnection { get; set; }
        private SystemTimer ParentWatchdogTimer { get; }
        private ConcurrentDictionary<int, string> PendingSolicitationDictionary { get; } = new ConcurrentDictionary<int, string>();
        private SoulseekClient SoulseekClient { get; }
        private string StatusHash { get; set; }
        private SystemTimer StatusWatchdogTimer { get; }

        /// <summary>
        ///     Adds a new child connection using the details in the specified <paramref name="connectToPeerResponse"/> and
        ///     pierces the remote peer's firewall.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context.</returns>
        public async Task AddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            bool cached = true;
            var r = connectToPeerResponse;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Child connection from {r.Username} ({r.IPEndPoint}) for token {r.Token} rejected: limit of {ConcurrentChildLimit} reached");
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            try
            {
                await ChildConnectionDictionary.GetOrAdd(
                    r.Username,
                    key => new Lazy<Task<IMessageConnection>>(() => GetConnection())).Value.ConfigureAwait(false);

                if (cached)
                {
                    Diagnostic.Debug($"Child connection from {r.Username} ({r.IPEndPoint}) for token {r.Token} ignored; connection already exists.");
                }
            }
            catch (Exception ex)
            {
                var msg = $"Failed to establish an inbound indirect child connection to {r.Username} ({r.IPEndPoint}): {ex.Message}";
                Diagnostic.Debug(msg);
                Diagnostic.Debug($"Purging child connection cache of failed connection to {r.Username} ({r.IPEndPoint}).");
                ChildConnectionDictionary.TryRemove(r.Username, out _);
                throw new ConnectionException(msg, ex);
            }

            async Task<IMessageConnection> GetConnection()
            {
                cached = false;

                Diagnostic.Debug($"Attempting inbound indirect child connection to {r.Username} ({r.IPEndPoint}) for token {r.Token}");

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
                catch
                {
                    connection.Dispose();
                    throw;
                }

                Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
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
            var c = incomingConnection;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Inbound child connection to {username} ({c.IPEndPoint}) rejected: limit of {ConcurrentChildLimit} concurrent connections reached.");
                c.Dispose();
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            try
            {
                await ChildConnectionDictionary.AddOrUpdate(
                    username,
                    new Lazy<Task<IMessageConnection>>(() => GetConnection()),
                    (key, cachedConnectionRecord) => new Lazy<Task<IMessageConnection>>(() => GetConnection(cachedConnectionRecord))).Value.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to establish an inbound child connection to {username} ({c.IPEndPoint}): {ex.Message}";
                Diagnostic.Debug($"{msg} (type: {c.Type}, id: {c.Id})");
                Diagnostic.Debug($"Purging child connection cache of failed connection to {username} ({c.IPEndPoint}).");
                ChildConnectionDictionary.TryRemove(username, out _);
                throw new ConnectionException(msg, ex);
            }

            async Task<IMessageConnection> GetConnection(Lazy<Task<IMessageConnection>> cachedConnectionRecord = null)
            {
                Diagnostic.Debug($"Inbound child connection to {username} ({c.IPEndPoint}) accepted. (type: {c.Type}, id: {c.Id}");

                var superseded = false;

                var connection = ConnectionFactory.GetMessageConnection(
                    username,
                    c.IPEndPoint,
                    SoulseekClient.Options.DistributedConnectionOptions,
                    c.HandoffTcpClient());

                connection.Type = ConnectionTypes.Inbound | ConnectionTypes.Direct;
                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessageRead;

                Diagnostic.Debug($"Inbound child connection to {username} ({connection.IPEndPoint}) handed off. (old: {c.Id}, new: {connection.Id})");

                if (cachedConnectionRecord != null)
                {
                    var cachedConnection = await cachedConnectionRecord.Value.ConfigureAwait(false);
                    cachedConnection.Disconnected -= ChildConnection_Disconnected;
                    Diagnostic.Debug($"Superceding cached child connection to {username} ({cachedConnection.IPEndPoint}) (old: {c.Id}, new: {connection.Id}");
                    cachedConnection.Disconnect("Superseded.");
                    cachedConnection.Dispose();
                    superseded = true;
                }

                try
                {
                    connection.StartReadingContinuously();

                    await connection.WriteAsync(GetBranchInformation<MessageCode.Peer>()).ConfigureAwait(false);
                }
                catch
                {
                    connection.Dispose();
                    throw;
                }

                Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
                Diagnostic.Info($"{(superseded ? "Updated" : "Added")} child connection to {connection.Username} ({connection.IPEndPoint})");

                return connection;
            }
        }

        /// <summary>
        ///     Asynchronously connects to one of the specified <paramref name="parentCandidates"/>.
        /// </summary>
        /// <remarks>
        ///     This method is invoked upon receipt of a list of new parent candidates via a <see cref="NetInfoNotification"/>, or
        ///     when a previous parent is disconnected. In the event of a disconnection, a connection will be attempted using the
        ///     existing list of parent connections, if there is one.
        /// </remarks>
        /// <param name="parentCandidates">The list of parent connection candidates provided by the server.</param>
        /// <returns>The operation context.</returns>
        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPEndPoint IPEndPoint)> parentCandidates)
        {
            ParentCandidateList = parentCandidates.ToList();

            if (HasParent || ParentCandidateList.Count == 0)
            {
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            Diagnostic.Info($"Attempting to establish a new parent connection from {ParentCandidateList.Count} candidates");
            Diagnostic.Debug($"Parent candidates: {string.Join(", ", ParentCandidateList.Select(p => p.Username))}");

            using (var cts = new CancellationTokenSource())
            {
                var tasks = ParentCandidateList.Select(p => GetParentCandidateConnectionAsync(p.Username, p.IPEndPoint, cts.Token)).ToList();

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // noop
                }

                var successfulConnections = tasks
                    .Where(t => t.Status == TaskStatus.RanToCompletion)
                    .Select(async t => await t.ConfigureAwait(false))
                    .Select(t => t.Result)
                    .OrderBy(c => c.BranchLevel)
                    .ToList();

                try
                {
                    if (successfulConnections.Count > 0)
                    {
                        Diagnostic.Debug($"Successfully established {successfulConnections.Count} connections.");

                        (ParentConnection, BranchLevel, BranchRoot) = successfulConnections.First();
                        Diagnostic.Debug($"Selected {ParentConnection.Username} as the best connection; branch root: {BranchRoot}, branch level: {BranchLevel}");

                        ParentConnection.Disconnected += ParentConnection_Disconnected;
                        ParentConnection.Disconnected -= ParentCandidateConnection_Disconnected;
                        ParentConnection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessageRead;

                        Diagnostic.Debug($"Parent connection to {ParentConnection.Username} ({ParentConnection.IPEndPoint}) established. (type: {ParentConnection.Id}, id: {ParentConnection.Id})");
                        Diagnostic.Info($"Adopted parent connection to {ParentConnection.Username} ({ParentConnection.IPEndPoint})");

                        successfulConnections.Remove((ParentConnection, BranchLevel, BranchRoot));
                        ParentCandidateList = successfulConnections.Select(c => (c.Connection.Username, c.Connection.IPEndPoint)).ToList();

                        Diagnostic.Debug($"Connected parent candidates: {string.Join(", ", ParentCandidateList.Select(p => p.Username))}");

                        foreach (var connection in successfulConnections)
                        {
                            var c = connection.Connection;
                            Diagnostic.Debug($"Disconnecting parent candidate connection to {c.Username} ({c.IPEndPoint})");
                            c.Disconnect("Not selected.");
                            c.Dispose();
                        }
                    }
                    else
                    {
                        Diagnostic.Warning("Failed to connect to any of the available parent candidates");
                    }
                }
                finally
                {
                    await UpdateStatusAsync().ConfigureAwait(false);
                }
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
        ///     Asynchronously forwards received search requests to each of the connected child connections.
        /// </summary>
        /// <param name="distributedSearchRequest">The distributed search request to forward.</param>
        /// <returns>The operation context.</returns>
        public Task ForwardSearchRequest(DistributedSearchRequest distributedSearchRequest)
        {
            ParentWatchdogTimer?.Reset();
            LastSearchRequest = DateTime.Now;
            return BroadcastMessageAsync(distributedSearchRequest.ToByteArray());
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
            UpdateStatusAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Sets the distributed <paramref name="branchRoot"/>.
        /// </summary>
        /// <param name="branchRoot">The distributed branch root.</param>
        public void SetBranchRoot(string branchRoot)
        {
            BranchRoot = branchRoot;
            UpdateStatusAsync().ConfigureAwait(false);
        }

        private void ChildConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;
            ChildConnectionDictionary.TryRemove(connection.Username, out _);
            Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");
            Diagnostic.Info($"Child connection to {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? "." : $": {e.Message}")}.");
            connection.Dispose();

            UpdateStatusAsync().ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
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

        private async Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)> GetParentCandidateConnectionAsync(string username, IPEndPoint ipEndPoint, CancellationToken cancellationToken)
        {
            using (var directCts = new CancellationTokenSource())
            using (var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token))
            using (var indirectCts = new CancellationTokenSource())
            using (var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token))
            {
                Diagnostic.Debug($"Attempting simultaneous direct and indirect parent candidate connections to {username} ({ipEndPoint})");

                var direct = GetParentCandidateConnectionDirectAsync(username, ipEndPoint, directLinkedCts.Token);
                var indirect = GetParentCandidateConnectionIndirectAsync(username, indirectLinkedCts.Token);

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
                    var msg = $"Failed to establish a direct or indirect parent candidate connection to {username} ({ipEndPoint})";
                    Diagnostic.Debug(msg);
                    throw new ConnectionException(msg);
                }

                var connection = await task.ConfigureAwait(false);
                var isDirect = task == direct;

                Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} parent candidate connection to {username} ({ipEndPoint}) established first, attempting to cancel {(isDirect ? "indirect" : "direct")} connection.");
                (isDirect ? indirectCts : directCts).Cancel();

                int branchLevel;
                string branchRoot;

                try
                {
                    var initWait = WaitForParentCandidateConnectionInitializationAsync(connection, cancellationToken);

                    if (isDirect)
                    {
                        var request = new PeerInit(SoulseekClient.Username, Constants.ConnectionType.Distributed, SoulseekClient.GetNextToken()).ToByteArray();
                        await connection.WriteAsync(request, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        connection.StartReadingContinuously();
                    }

                    Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} parent candidate connection to {username} ({ipEndPoint}) initialized.  Waiting for branch information and first search request. (id: {connection.Id})");
                    (branchLevel, branchRoot) = await initWait.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to negotiate parent candidate connection to {username} ({ipEndPoint}): {ex.Message}";
                    Diagnostic.Debug($"{msg} (type: {connection.Type}, id: {connection.Id})");
                    connection.Dispose();
                    throw new ConnectionException(msg, ex);
                }

                Diagnostic.Debug($"Parent candidate connection to {username} ({ipEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
                return (connection, branchLevel, branchRoot);
            }
        }

        private async Task<IMessageConnection> GetParentCandidateConnectionDirectAsync(string username, IPEndPoint ipEndPoint, CancellationToken cancellationToken)
        {
            Diagnostic.Debug($"Attempting direct parent candidate connection to {username} ({ipEndPoint})");

            var connection = ConnectionFactory.GetMessageConnection(
                username,
                ipEndPoint,
                SoulseekClient.Options.DistributedConnectionOptions);

            connection.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct;
            connection.Disconnected += ParentCandidateConnection_Disconnected;

            try
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to establish a direct parent candidate connection to {username} ({ipEndPoint}): {ex.Message}");
                connection.Dispose();
                throw;
            }

            Diagnostic.Debug($"Direct parent candidate connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
            return connection;
        }

        private async Task<IMessageConnection> GetParentCandidateConnectionIndirectAsync(string username, CancellationToken cancellationToken)
        {
            var solicitationToken = SoulseekClient.GetNextToken();

            Diagnostic.Debug($"Soliciting indirect parent candidate connection to {username} with token {solicitationToken}");

            try
            {
                PendingSolicitationDictionary.TryAdd(solicitationToken, username);

                await SoulseekClient.ServerConnection
                    .WriteAsync(new ConnectToPeerRequest(solicitationToken, username, Constants.ConnectionType.Distributed).ToByteArray(), cancellationToken)
                    .ConfigureAwait(false);

                using (var incomingConnection = await SoulseekClient.Waiter
                    .Wait<IConnection>(new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, username, solicitationToken), SoulseekClient.Options.DistributedConnectionOptions.ConnectTimeout, cancellationToken)
                    .ConfigureAwait(false))
                {
                    var connection = ConnectionFactory.GetMessageConnection(
                        username,
                        incomingConnection.IPEndPoint,
                        SoulseekClient.Options.DistributedConnectionOptions,
                        incomingConnection.HandoffTcpClient());

                    Diagnostic.Debug($"Indirect parent candidate connection to {username} ({incomingConnection.IPEndPoint}) handed off. (old: {incomingConnection.Id}, new: {connection.Id})");

                    connection.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect;
                    connection.Disconnected += ParentCandidateConnection_Disconnected;

                    Diagnostic.Debug($"Indirect parent candidate connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
                    return connection;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to establish an indirect parent candidate connection to {username} with token {solicitationToken}: {ex.Message}");
                throw;
            }
            finally
            {
                PendingSolicitationDictionary.TryRemove(solicitationToken, out var _);
            }
        }

        private void ParentCandidateConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;

            Diagnostic.Debug($"Parent candidate connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");

            connection.Dispose();
        }

        private async void ParentConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;

            Diagnostic.Debug($"Parent connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");
            Diagnostic.Info($"Parent connection to {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? "." : $": {e.Message}")}.");

            ParentConnection = null;
            BranchLevel = 0;
            BranchRoot = string.Empty;

            connection.Dispose();

            try
            {
                await AddParentConnectionAsync(ParentCandidateList).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // noop
            }
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
            var parentsIp = HasParent ? ParentConnection?.IPEndPoint?.Address ?? IPAddress.Any : IPAddress.Any;
            var branchLevel = HasParent ? BranchLevel : 0;
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

                Diagnostic.Info(sb.ToString());
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

        private void WaitForParentCandidateConnection_MessageRead(object sender, MessageReadEventArgs e)
        {
            var conn = (IMessageConnection)sender;

            try
            {
                var code = new MessageReader<MessageCode.Distributed>(e.Message).ReadCode();

                switch (code)
                {
                    case MessageCode.Distributed.ServerSearchRequest:
                    case MessageCode.Distributed.SearchRequest:
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Id));
                        break;

                    case MessageCode.Distributed.BranchLevel:
                        var branchLevel = DistributedBranchLevel.FromByteArray(e.Message);
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Id), branchLevel.Level);
                        break;

                    case MessageCode.Distributed.BranchRoot:
                        var branchRoot = DistributedBranchRoot.FromByteArray(e.Message);
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Id), branchRoot.Username);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to handle message from parent candidate: {ex.Message}", ex);
                conn.Disconnect(ex.Message);
                conn.Dispose();
            }
        }

        private async Task<(int BranchLevel, string BranchRoot)> WaitForParentCandidateConnectionInitializationAsync(IMessageConnection connection, CancellationToken cancellationToken)
        {
            connection.MessageRead += WaitForParentCandidateConnection_MessageRead;

            var branchLevelWait = SoulseekClient.Waiter.Wait<int>(new WaitKey(Constants.WaitKey.BranchLevelMessage, connection.Id), cancellationToken: cancellationToken);
            var branchRootWait = SoulseekClient.Waiter.Wait<string>(new WaitKey(Constants.WaitKey.BranchRootMessage, connection.Id), cancellationToken: cancellationToken);
            var searchWait = SoulseekClient.Waiter.Wait(new WaitKey(Constants.WaitKey.SearchRequestMessage, connection.Id), cancellationToken: cancellationToken);

            // wait for the branch level and first search request. branch roots will not send the root.
            var waits = new[] { branchLevelWait, searchWait }.ToList();
            var waitsTask = Task.WhenAll(waits);

            try
            {
                int branchLevel;
                string branchRoot;

                await waitsTask.ConfigureAwait(false);

                branchLevel = await branchLevelWait.ConfigureAwait(false);

                // if we didn't connect to a root, ensure we get the name of the root.
                if (branchLevel > 0)
                {
                    branchRoot = await branchRootWait.ConfigureAwait(false);
                }
                else
                {
                    Diagnostic.Debug($"Received branch level 0 from parent candidate {connection.Username}; this user is a branch root.");
                    branchRoot = connection.Username;
                }

                await searchWait.ConfigureAwait(false);

                return (branchLevel, branchRoot);
            }
            catch (Exception)
            {
                connection.Disconnect($"One or more required messages was not received.");
                throw new ConnectionException($"Failed to retrieve branch info from parent candidate connection to {connection.Username} ({connection.IPEndPoint}); one or more required messages was not received. (id: {connection.Id})");
            }
            finally
            {
                connection.MessageRead -= WaitForParentCandidateConnection_MessageRead;
            }
        }
    }
}