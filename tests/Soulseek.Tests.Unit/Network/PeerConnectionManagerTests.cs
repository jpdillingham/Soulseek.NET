// <copyright file="PeerConnectionManagerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class PeerConnectionManagerTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            PeerConnectionManager c = null;

            var ex = Record.Exception(() => (c, _) = GetFixture());

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(0, c.MessageConnections.Count);
            Assert.Equal(new ClientOptions().ConcurrentPeerMessageConnectionLimit, c.ConcurrentMessageConnectionLimit);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var (manager, mocks) = GetFixture();

            using (manager)
            using (var c = new PeerConnectionManager(mocks.Client.Object))
            {
                var ex = Record.Exception(() => c.Dispose());

                Assert.Null(ex);
            }
        }

        [Trait("Category", "RemoveAndDisposeAll")]
        [Theory(DisplayName = "RemoveAndDisposeAll removes and disposes all"), AutoData]
        public void RemoveAndDisposeAll_Removes_And_Disposes_All(IPAddress ip, int port)
        {
            var (manager, mocks) = GetFixture();

            using (manager)
            using (var semaphore = new SemaphoreSlim(1))
            {
                var peer = new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();
                peer.GetOrAdd("foo", (semaphore, new Mock<IMessageConnection>().Object));

                manager.SetProperty("MessageConnectionDictionary", peer);

                var solicitations = new ConcurrentDictionary<int, string>();
                solicitations.TryAdd(1, "bar");

                manager.SetProperty("PendingSolicitationDictionary", solicitations);
                manager.SetField("waitingMessageConnections", 1);

                manager.RemoveAndDisposeAll();

                Assert.Empty(manager.MessageConnections);
                Assert.Empty(manager.PendingSolicitations);
                Assert.Equal(0, manager.WaitingMessageConnections);
            }
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync reads token and completes wait"), AutoData]
        internal async Task AddTransferConnectionAsync_Reads_Token_And_Completes_Wait(string username, IPAddress ipAddress, int port, int token, ConnectionOptions options)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            conn.Setup(m => m.Port)
                .Returns(port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.TcpClient.Setup(m => m.RemoteEndPoint)
                .Returns(new IPEndPoint(IPAddress.None, 0));

            using (manager)
            {
                await manager.AddTransferConnectionAsync(username, token, mocks.TcpClient.Object);
            }

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(Constants.WaitKey.DirectTransfer, username, token), conn.Object));
        }

        //        [Trait("Category", "AddSolicitedTransferConnectionAsync")]
        //        [Theory(DisplayName = "AddSolicitedTransferConnectionAsync connects and pierces firewall"), AutoData]
        //        internal async Task AddSolicitedTransferConnectionAsync_Connects_And_Pierces_Firewall(string username, IPAddress ipAddress, int port, int token, ConnectionOptions options)
        //        {
        //            var ctpr = new ConnectToPeerResponse(username, "F", ipAddress, port, token);
        //            var expectedBytes = new PierceFirewallRequest(token).ToByteArray().ToByteArray();
        //            byte[] actualBytes = Array.Empty<byte>();

        //            var conn = new Mock<IConnection>();
        //            conn.Setup(m => m.IPAddress)
        //                .Returns(ipAddress);
        //            conn.Setup(m => m.Port)
        //                .Returns(port);
        //            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
        //                .Returns(Task.CompletedTask);
        //            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //                .Returns(Task.CompletedTask)
        //                .Callback<byte[], CancellationToken>((b, c) => actualBytes = b);

        //            var connFactory = new Mock<IConnectionFactory>();
        //            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
        //                .Returns(conn.Object);

        //            IConnection newConn = null;

        //            using (var c = new ConnectionManager(1, connectionFactory: connFactory.Object))
        //            {
        //                newConn = await c.AddSolicitedTransferConnectionAsync(ctpr, options, CancellationToken.None);
        //            }

        //            Assert.Equal(ipAddress, newConn.IPAddress);
        //            Assert.Equal(port, newConn.Port);

        //            Assert.Equal(expectedBytes, actualBytes);

        //            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        //            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        //        }

        //        [Trait("Category", "GetOrAddSolicitedConnectionAsync")]
        //        [Theory(DisplayName = "GetOrAddSolicitedConnectionAsync connects and pierces firewall"), AutoData]
        //        internal async Task GetOrAddSolicitedConnectionAsync_Connects_And_Pierces_Firewall(
        //            string username, IPAddress ipAddress, int port, EventHandler<Message> messageHandler, ConnectionOptions options, int token)
        //        {
        //            var ctpr = new ConnectToPeerResponse(username, "P", ipAddress, port, token);

        //            var expectedBytes = new PierceFirewallRequest(token).ToByteArray().ToByteArray();
        //            byte[] actualBytes = Array.Empty<byte>();

        //            var tokenFactory = new Mock<ITokenFactory>();
        //            tokenFactory.Setup(m => m.NextToken())
        //                .Returns(token);

        //            var conn = new Mock<IMessageConnection>();
        //            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
        //                .Returns(Task.CompletedTask);
        //            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //                .Returns(Task.CompletedTask)
        //                .Callback<byte[], CancellationToken>((b, ct) => actualBytes = b);

        //            var connFactory = new Mock<IConnectionFactory>();
        //            connFactory.Setup(m => m.GetMessageConnection(MessageConnectionType.Peer, username, ipAddress, port, options))
        //                .Returns(conn.Object);

        //            var c = new ConnectionManager(10, tokenFactory.Object, connFactory.Object);

        //            IMessageConnection connection = null;

        //            var ex = await Record.ExceptionAsync(async () => connection = await c.GetOrAddSolicitedPeerConnectionAsync(ctpr, messageHandler, options, CancellationToken.None));

        //            Assert.Null(ex);

        //            Assert.Equal(conn.Object, connection);

        //            Assert.Equal(expectedBytes, actualBytes);
        //        }

        //        [Trait("Category", "GetOrAddSolicitedConnectionAsync")]
        //        [Theory(DisplayName = "GetOrAddSolicitedConnectionAsync returns existing connection"), AutoData]
        //        internal async Task GetOrAddSolicitedConnectionAsync_Returns_Existing_Connection(
        //            string username, IPAddress ipAddress, int port, EventHandler<Message> messageHandler, ConnectionOptions options, int token)
        //        {
        //            var ctpr = new ConnectToPeerResponse(username, "P", ipAddress, port, token);

        //            var key = new ConnectionKey(username, ipAddress, port, MessageConnectionType.Peer);
        //            var conn = new Mock<IMessageConnection>();

        //            var peer = new ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();
        //            peer.GetOrAdd(key, (new SemaphoreSlim(1), conn.Object));

        //            var c = new ConnectionManager(10);
        //            c.SetProperty("PeerConnections", peer);

        //            IMessageConnection connection = null;

        //            var ex = await Record.ExceptionAsync(async () => connection = await c.GetOrAddSolicitedPeerConnectionAsync(ctpr, messageHandler, options, CancellationToken.None));

        //            Assert.Null(ex);

        //            Assert.Equal(conn.Object, connection);
        //        }

        //        [Trait("Category", "GetOrAddUnsolicitedConnectionAsync")]
        //        [Theory(DisplayName = "GetOrAddUnsolicitedConnectionAsync connects and sends PeerInit"), AutoData]
        //        internal async Task GetOrAddUnsolicitedConnectionAsync_Connects_And_Sends_PeerInit(
        //            string username, IPAddress ipAddress, int port, EventHandler<Message> messageHandler, ConnectionOptions options, int token)
        //        {
        //            var key = new ConnectionKey(username, ipAddress, port, MessageConnectionType.Peer);

        //            var expectedBytes = new PeerInitRequest(username, "P", token).ToByteArray().ToByteArray();
        //            byte[] actualBytes = Array.Empty<byte>();

        //            var tokenFactory = new Mock<ITokenFactory>();
        //            tokenFactory.Setup(m => m.NextToken())
        //                .Returns(token);

        //            var conn = new Mock<IMessageConnection>();
        //            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
        //                .Returns(Task.CompletedTask);
        //            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //                .Returns(Task.CompletedTask)
        //                .Callback<byte[], CancellationToken>((b, ct) => actualBytes = b);

        //            var connFactory = new Mock<IConnectionFactory>();
        //            connFactory.Setup(m => m.GetMessageConnection(MessageConnectionType.Peer, username, ipAddress, port, options))
        //                .Returns(conn.Object);

        //            var c = new ConnectionManager(10, tokenFactory.Object, connFactory.Object);

        //            IMessageConnection connection = null;

        //            var ex = await Record.ExceptionAsync(async () => connection = await c.GetOrAddUnsolicitedPeerConnectionAsync(key, username, messageHandler, options, CancellationToken.None));

        //            Assert.Null(ex);

        //            Assert.Equal(conn.Object, connection);

        //            Assert.Equal(expectedBytes, actualBytes);
        //        }

        //        [Trait("Category", "GetOrAddUnsolicitedConnectionAsync")]
        //        [Theory(DisplayName = "GetOrAddUnsolicitedConnectionAsync returns existing connection"), AutoData]
        //        internal async Task GetOrAddUnsolicitedConnectionAsync_Returns_Existing_Connection(
        //            string username, IPAddress ipAddress, int port, EventHandler<Message> messageHandler, ConnectionOptions options)
        //        {
        //            var key = new ConnectionKey(username, ipAddress, port, MessageConnectionType.Peer);
        //            var conn = new Mock<IMessageConnection>();

        //            var peer = new ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();
        //            peer.GetOrAdd(key, (new SemaphoreSlim(1), conn.Object));

        //            var c = new ConnectionManager(10);
        //            c.SetProperty("PeerConnections", peer);

        //            IMessageConnection connection = null;

        //            var ex = await Record.ExceptionAsync(async () => connection = await c.GetOrAddUnsolicitedPeerConnectionAsync(key, username, messageHandler, options, CancellationToken.None));

        //            Assert.Null(ex);

        //            Assert.Equal(conn.Object, connection);
        //        }

        //        [Trait("Category", "Semaphore")]
        //        [Theory(DisplayName = "GetOrAdd queues connections"), AutoData]
        //        internal void GetOrAdd_Queues_Connections(
        //            string username, IPAddress ipAddress, int port, string username2, IPAddress ipAddress2, int port2, EventHandler<Message> messageHandler, ConnectionOptions options, int token)
        //        {
        //            var key1 = new ConnectionKey(username, ipAddress, port, MessageConnectionType.Peer);
        //            var key2 = new ConnectionKey(username2, ipAddress2, port2, MessageConnectionType.Peer);

        //            var tokenFactory = new Mock<ITokenFactory>();
        //            tokenFactory.Setup(m => m.NextToken())
        //                .Returns(token);

        //            var conn = new Mock<IMessageConnection>();
        //            conn.Setup(m => m.Key)
        //                .Returns(key1);
        //            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //                .Returns(() => Task.CompletedTask);

        //            var conn2 = new Mock<IMessageConnection>();
        //            conn2.Setup(m => m.Key)
        //                .Returns(key2);
        //            conn2.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //                .Returns(() => Task.CompletedTask);

        //            var connFactory = new Mock<IConnectionFactory>();
        //            connFactory.Setup(m => m.GetMessageConnection(MessageConnectionType.Peer, username, ipAddress, port, options))
        //                .Returns(conn.Object);
        //            connFactory.Setup(m => m.GetMessageConnection(MessageConnectionType.Peer, username2, ipAddress2, port2, options))
        //                .Returns(conn2.Object);

        //            var c = new ConnectionManager(1, tokenFactory.Object, connFactory.Object);

        //#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        //            c.GetOrAddUnsolicitedPeerConnectionAsync(key1, username, messageHandler, options, CancellationToken.None);
        //            c.GetOrAddUnsolicitedPeerConnectionAsync(key2, username, messageHandler, options, CancellationToken.None);
        //#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        //            Assert.Equal(1, c.ActivePeerConnections);
        //            Assert.Equal(1, c.WaitingPeerConnections);

        //            var firstConn = c.GetProperty<ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>>("PeerConnections").First();
        //            c.InvokeMethod("RemoveMessageConnection", firstConn.Value.Connection);

        //            Thread.Sleep(500);

        //            Assert.Equal(1, c.ActivePeerConnections);
        //            Assert.Equal(0, c.WaitingPeerConnections);

        //            firstConn = c.GetProperty<ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>>("PeerConnections").First();
        //            c.InvokeMethod("RemoveMessageConnection", firstConn.Value.Connection);

        //            Thread.Sleep(500);

        //            Assert.Equal(0, c.ActivePeerConnections);
        //            Assert.Equal(0, c.WaitingPeerConnections);
        //        }

        private (PeerConnectionManager Manager, Mocks Mocks) GetFixture(string username = null, IPAddress ip = null, int port = 0, ClientOptions options = null)
        {
            var mocks = new Mocks(options);

            mocks.ServerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.ServerConnection.Setup(m => m.IPAddress)
                .Returns(ip ?? IPAddress.Parse("0.0.0.0"));
            mocks.ServerConnection.Setup(m => m.Port)
                .Returns(port);

            var handler = new PeerConnectionManager(
                mocks.Client.Object,
                mocks.ConnectionFactory.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private class Mocks
        {
            public Mocks(ClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(clientOptions)
                {
                    CallBase = true,
                };

                Client.Setup(m => m.ServerConnection).Returns(ServerConnection.Object);
                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.Listener).Returns(Listener.Object);
                Client.Setup(m => m.PeerMessageHandler).Returns(PeerMessageHandler.Object);
                Client.Setup(m => m.DistributedMessageHandler).Returns(DistributedMessageHandler.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public Mock<IListener> Listener { get; } = new Mock<IListener>();
            public Mock<IPeerMessageHandler> PeerMessageHandler { get; } = new Mock<IPeerMessageHandler>();
            public Mock<IDistributedMessageHandler> DistributedMessageHandler { get; } = new Mock<IDistributedMessageHandler>();
            public Mock<IConnectionFactory> ConnectionFactory { get; } = new Mock<IConnectionFactory>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<ITcpClient> TcpClient { get; } = new Mock<ITcpClient>();
        }
    }
}
