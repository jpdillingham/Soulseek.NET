//// <copyright file="ConnectionManagerTests.cs" company="JP Dillingham">
////     Copyright (c) JP Dillingham. All rights reserved.
////
////     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
////     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
////
////     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
////     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
////
////     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
//// </copyright>

//namespace Soulseek.Tests.Unit.Network
//{
//    using System;
//    using System.Collections.Concurrent;
//    using System.Linq;
//    using System.Net;
//    using System.Threading;
//    using System.Threading.Tasks;
//    using AutoFixture.Xunit2;
//    using Moq;
//    using Soulseek.Messaging;
//    using Soulseek.Messaging.Messages;
//    using Soulseek.Messaging.Tcp;
//    using Soulseek.Network;
//    using Xunit;

//    public class ConnectionManagerTests
//    {
//        [Trait("Category", "Instantiation")]
//        [Fact(DisplayName = "Instantiates properly")]
//        public void Instantiates_Properly()
//        {
//            ConnectionManager c = null;

//            var ex = Record.Exception(() => c = new ConnectionManager(1000));

//            Assert.Null(ex);
//            Assert.NotNull(c);

//            Assert.Equal(1000, c.ConcurrentPeerConnections);
//            Assert.Equal(0, c.ActivePeerConnections);
//            Assert.Equal(0, c.WaitingPeerConnections);
//            Assert.Equal(0, c.ActiveTransferConnections);
//        }

//        [Trait("Category", "Instantiation")]
//        [Fact(DisplayName = "Throws ArgumentOutOfRangeException when ConcurrentPeerConnections is less than 1.")]
//        public void Throws_ArgumentOutOfRangeException_When_ConcurrentPeerConnections_Is_Less_than_1()
//        {
//            ConnectionManager c = null;

//            var ex = Record.Exception(() => c = new ConnectionManager(0));

//            Assert.NotNull(ex);
//            Assert.IsType<ArgumentOutOfRangeException>(ex);
//        }

//        [Trait("Category", "Dispose")]
//        [Fact(DisplayName = "Disposes without throwing")]
//        public void Disposes_Without_Throwing()
//        {
//            var c = new ConnectionManager(1);

//            var ex = Record.Exception(() => c.Dispose());

//            Assert.Null(ex);
//        }

//        [Trait("Category", "RemoveAndDisposeAll")]
//        [Theory(DisplayName = "RemoveAndDisposeAll removes and disposes all"), AutoData]
//        public void RemoveAndDisposeAll_Removes_And_Disposes_All(IPAddress ip, int port)
//        {
//            var c = new ConnectionManager(1);

//            var peer = new ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();
//            peer.GetOrAdd(new ConnectionKey(ip, port), (new SemaphoreSlim(1), new Mock<IMessageConnection>().Object));

//            c.SetProperty("PeerConnections", peer);

//            var transfer = new ConcurrentDictionary<(ConnectionKey, int), IConnection>();
//            transfer.GetOrAdd((new ConnectionKey(ip, port), port), new Mock<IConnection>().Object);

//            c.SetProperty("TransferConnections", transfer);

//            var activePeerBefore = c.ActivePeerConnections;
//            var activeTransferBefore = c.ActiveTransferConnections;

//            c.RemoveAndDisposeAll();

//            Assert.Equal(1, activePeerBefore);
//            Assert.Equal(1, activeTransferBefore);

//            Assert.Empty(c.GetProperty<ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>>("PeerConnections"));
//            Assert.Empty(c.GetProperty<ConcurrentDictionary<(ConnectionKey, int), IConnection>>("TransferConnections"));
//        }

//        [Trait("Category", "AddUnsolicitedTransferConnectionAsync")]
//        [Theory(DisplayName = "AddUnsolicitedTransferConnectionAsync connects and sends PeerInit"), AutoData]
//        internal async Task AddUnsolicitedTransferConnectionAsync_Connects_And_Sends_PeerInit(string username, IPAddress ipAddress, int port, int token, ConnectionOptions options)
//        {
//            var key = new ConnectionKey(ipAddress, port);
//            var expectedBytes = new PeerInitRequest(username, "F", token).ToByteArray().ToByteArray();
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
//                newConn = await c.AddUnsolicitedTransferConnectionAsync(key, token, username, options, CancellationToken.None);
//            }

//            Assert.Equal(ipAddress, newConn.IPAddress);
//            Assert.Equal(port, newConn.Port);

//            Assert.Equal(expectedBytes, actualBytes);

//            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
//            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
//        }

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
//    }
//}
