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
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;
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
        [Fact(DisplayName = "RemoveAndDisposeAll removes and disposes all")]
        public void RemoveAndDisposeAll_Removes_And_Disposes_All()
        {
            var (manager, _) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            using (manager)
            using (var semaphore = new SemaphoreSlim(1))
            {
                var peer = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
                peer.GetOrAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

                manager.SetProperty("MessageConnectionDictionary", peer);

                var solicitations = new ConcurrentDictionary<int, string>();
                solicitations.TryAdd(1, "bar");

                manager.SetProperty("PendingSolicitationDictionary", solicitations);

                manager.RemoveAndDisposeAll();

                Assert.Empty(manager.PendingSolicitations);
                Assert.Empty(manager.MessageConnections);
            }
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync reads token and completes wait"), AutoData]
        internal async Task AddTransferConnectionAsync_Reads_Token_And_Completes_Wait(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddTransferConnectionAsync(username, token, incomingConn.Object);
            }

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(Constants.WaitKey.DirectTransfer, username, token), conn.Object));
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync disposes connection on exception"), AutoData]
        internal async Task AddTransferConnectionAsync_Disposes_Connection_On_Exception(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.AddTransferConnectionAsync(username, token, incomingConn.Object));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync produces diagnostic on disconnect"), AutoData]
        internal async Task AddTransferConnectionAsync_Produces_Diagnostic_On_Disconnect(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Callback<long, CancellationToken?>((i, t) => conn.Raise(mock => mock.Disconnected += null, null, new ConnectionDisconnectedEventArgs("foo")))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.AddTransferConnectionAsync(username, token, incomingConn.Object));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("disconnected", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync starts reading"), AutoData]
        internal async Task AddMessageConnectionAsync_Starts_Reading(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddMessageConnectionAsync(username, incomingConn.Object);
            }

            conn.Verify(m => m.StartReadingContinuously());
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync adds connection"), AutoData]
        internal async Task AddMessageConnectionAsync_Adds_Connection(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddMessageConnectionAsync(username, incomingConn.Object);

                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPEndPoint == endpoint);
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync replaces duplicate connection and does not dispose old"), AutoData]
        internal async Task AddMessageConnectionAsync_Replaces_Duplicate_Connection_And_Does_Not_Dispose_Old(string username, IPEndPoint endpoint, int token)
        {
            var conn1 = GetMessageConnectionMock(username, endpoint);
            conn1.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn1.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var conn2 = GetMessageConnectionMock(username, endpoint);
            conn2.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn2.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddMessageConnectionAsync(username, incomingConn.Object);

                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPEndPoint == endpoint);

                // swap in the second connection
                mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                    .Returns(conn2.Object);

                // call this again to force the first connection out and second in its place
                await manager.AddMessageConnectionAsync(username, incomingConn.Object);

                // make sure we still have just the one
                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPEndPoint == endpoint);

                // verify that the first connection was disposed
                conn1.Verify(m => m.Dispose(), Times.Never);
                conn1.Verify(m => m.Disconnect(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync connects and pierces firewall"), AutoData]
        internal async Task GetTransferConnectionAsync_Connects_And_Pierces_Firewall(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token);
            var expectedBytes = new PierceFirewall(token).ToByteArray();
            byte[] actualBytes = Array.Empty<byte>();

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask)
                .Callback<byte[], CancellationToken>((b, c) => actualBytes = b);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            (IConnection Connection, int RemoteToken) newConn = default;

            using (manager)
            {
                newConn = await manager.GetTransferConnectionAsync(ctpr);
            }

            Assert.Equal(endpoint.Address, newConn.Connection.IPEndPoint.Address);
            Assert.Equal(endpoint.Port, newConn.Connection.IPEndPoint.Port);
            Assert.Equal(token, newConn.RemoteToken);

            Assert.Equal(expectedBytes, actualBytes);

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken?>()), Times.Once);
            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync disposes connection if connect fails"), AutoData]
        internal async Task GetTransferConnectionAsync_Disposes_Connection_If_Connect_Fails(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token);
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.GetTransferConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync disposes connection if connect fails"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Disposes_Connection_If_Connect_Fails(IPEndPoint endpoint, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync returns connection if connect succeeds"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Returns_Connection_If_Connect_Succeeds(IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync sets connection type to Outbound Direct"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Sets_Connection_Type_To_Outbound_Direct(IPEndPoint endpoint, int token)
        {
            ConnectionTypes type = ConnectionTypes.None;

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.SetupSet(m => m.Type = It.IsAny<ConnectionTypes>())
                .Callback<ConnectionTypes>(o => type = o);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
            {
                Assert.Equal(ConnectionTypes.Outbound | ConnectionTypes.Direct, type);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync sends ConnectToPeerRequest"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Sends_ConnectToPeerRequest(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => true), CancellationToken.None));
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync throws if wait throws"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Throws_If_Wait_Throws(IPEndPoint endpoint, string username, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync hands off ITcpConnection"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Hands_Off_ITcpConnection(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            conn.Verify(m => m.HandoffTcpClient(), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync sets connection context to Indirect"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Sets_Connection_Context_To_Indirect(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync adds and removes from PendingSolicitationDictionary"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Adds_And_Removes_From_PendingSolicitationDictionary(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                List<KeyValuePair<int, string>> pending = new List<KeyValuePair<int, string>>();

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                    .Callback<WaitKey, int?, CancellationToken?>((w, i, c) => pending = manager.GetProperty<ConcurrentDictionary<int, string>>("PendingSolicitationDictionary").ToList())
                    .Returns(Task.FromResult(conn.Object));

                using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
                {
                    Assert.Equal(conn.Object, newConn);

                    Assert.Single(pending);
                    Assert.Equal(username, pending[0].Value);
                    Assert.Empty(manager.PendingSolicitations);
                }
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync returns direct connection when direct connects first"), AutoData]
        internal async Task GetTransferConnectionAsync_Returns_Direct_Connection_When_Direct_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync returns indirect connection when indirect connects first"), AutoData]
        internal async Task GetTransferConnectionAsync_Returns_Indirect_Connection_When_Indirect_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(indirect.Object));

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                Assert.Equal(indirect.Object, newConn);
                Assert.Equal(ConnectionTypes.Indirect, newConn.Type);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync throws ConnectionException when direct and indirect connections fail"), AutoData]
        internal async Task GetTransferConnectionAsync_Throws_ConnectionException_When_Direct_And_Indirect_Connections_Fail(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync generates expected diagnostics on successful connection"), AutoData]
        internal async Task GetTransferConnectionAsync_Generates_Expected_Diagnostics(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Attempting simultaneous direct and indirect transfer connections"))));
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"established first, attempting to cancel"))));
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("transfer connection to") && s.ContainsInsensitive("established."))));
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync sends PeerInit on direct connection established"), AutoData]
        internal async Task GetTransferConnectionAsync_Sends_PeerInit_On_Direct_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Transfer, token).ToByteArray();

            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);

                direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(peerInit)), It.IsAny<CancellationToken?>()), Times.Once);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync writes token on connection established"), AutoData]
        internal async Task GetTransferConnectionAsync_Writes_Token_On_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            {
                var dendpoint = new IPEndPoint(ipAddress, directPort);
                var direct = GetConnectionMock(dendpoint);
                direct.Setup(m => m.Type)
                    .Returns(ConnectionTypes.Direct);

                var iendpoint = new IPEndPoint(ipAddress, indirectPort);
                var indirect = GetConnectionMock(iendpoint);
                indirect.Setup(m => m.Type)
                    .Returns(ConnectionTypes.Indirect);

                var (manager, mocks) = GetFixture();

                mocks.Client.Setup(m => m.Username)
                    .Returns(localUsername);

                mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                    .Returns(direct.Object);
                mocks.ConnectionFactory.Setup(m => m.GetConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                    .Returns(indirect.Object);

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                    .Throws(new Exception());

                using (manager)
                using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
                {
                    Assert.Equal(direct.Object, newConn);
                    Assert.Equal(ConnectionTypes.Direct, newConn.Type);

                    direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(BitConverter.GetBytes(token))), It.IsAny<CancellationToken?>()), Times.Once);
                }
            }
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected removes and disposes connection"), AutoData]
        internal void MessageConnection_Disconnected_Removes_And_Disposes_Connection(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, mocks) = GetFixture();

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Empty(dict);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected generates diagnostic on removal"), AutoData]
        internal void MessageConnection_Disconnected_Generates_Diagnostic_On_Removal(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, mocks) = GetFixture();

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Contains(diagnostics, m => m.ContainsInsensitive($"Removed message connection record for {username}"));
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected does not throw if connection isn't tracked"), AutoData]
        internal void MessageConnection_Disconnected_Does_Not_Throw_If_Connection_Isnt_Tracked(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();

            var (manager, mocks) = GetFixture();

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Empty(dict);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected does not generate removed diagnostic if connection isn't tracked"), AutoData]
        internal void MessageConnection_Disconnected_Does_Generate_Diagnostic_If_Connection_Isnt_Tracked(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var (manager, mocks) = GetFixture();

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("removed"))), Times.Never);
            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync connects and returns connection if connect succeeds"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Connects_And_Returns_Connection_If_Connect_Succeeds(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, endpoint, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actualConn);
            }

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync disposes connection if connect fails"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Disposes_Connection_If_Connect_Fails(string username, IPEndPoint endpoint)
        {
            var expectedEx = new Exception("foo");

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, endpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync sets context to Direct"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Sets_Context_To_Direct(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, endpoint, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actualConn);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync sends ConnectToPeerRequest"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Sends_ConnectToPeerRequest(IPEndPoint endpoint, string username)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, newConn);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => true), CancellationToken.None));
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync throws if wait throws"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Throws_If_Wait_Throws(IPEndPoint endpoint, string username)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync hands off ITcpConnection"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Hands_Off_ITcpConnection(IPEndPoint endpoint, string username)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, newConn);
            }

            conn.Verify(m => m.HandoffTcpClient(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync sets connection context to Indirect"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Sets_Connection_Context_To_Indirect(IPEndPoint endpoint, string username)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, newConn);
            }

            msgConn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync adds and removes from PendingSolicitationDictionary"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Adds_And_Removes_From_PendingSolicitationDictionary(IPEndPoint endpoint, string username)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            using (manager)
            {
                List<KeyValuePair<int, string>> pending = new List<KeyValuePair<int, string>>();

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                    .Callback<WaitKey, int?, CancellationToken?>((w, i, c) => pending = manager.GetProperty<ConcurrentDictionary<int, string>>("PendingSolicitationDictionary").ToList())
                    .Returns(Task.FromResult(conn.Object));

                using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, CancellationToken.None))
                {
                    Assert.Equal(msgConn.Object, newConn);

                    Assert.Single(pending);
                    Assert.Equal(username, pending[0].Value);
                    Assert.Empty(manager.PendingSolicitations);
                }
            }

            msgConn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync calls StartReadingContinuously"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Calls_StartReadingContinuously(IPEndPoint endpoint, string username)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            using (manager)
            {
                List<KeyValuePair<int, string>> pending = new List<KeyValuePair<int, string>>();

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                    .Callback<WaitKey, int?, CancellationToken?>((w, i, c) => pending = manager.GetProperty<ConcurrentDictionary<int, string>>("PendingSolicitationDictionary").ToList())
                    .Returns(Task.FromResult(conn.Object));

                using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, CancellationToken.None))
                {
                    Assert.Equal(msgConn.Object, newConn);

                    Assert.Single(pending);
                    Assert.Equal(username, pending[0].Value);
                    Assert.Empty(manager.PendingSolicitations);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns existing connection if exists"), AutoData]
        internal async Task GetOrAddMessageConnectionAsyncCTPR_Returns_Existing_Connection_If_Exists(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token);

            var conn = GetMessageConnectionMock(username, endpoint);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, _) = GetFixture();

            using (manager)
            using (var sem = new SemaphoreSlim(1, 1))
            {
                manager.SetProperty("MessageConnectionDictionary", dict);

                using (var existingConn = await manager.GetOrAddMessageConnectionAsync(ctpr))
                {
                    Assert.Equal(conn.Object, existingConn);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync connects and returns new if not existing"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Connects_And_Returns_New_If_Not_Existing(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                using (var newConn = await manager.GetOrAddMessageConnectionAsync(ctpr))
                {
                    Assert.Equal(conn.Object, newConn);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync disposes connection and throws on connect failure"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Disposes_Connection_And_Throws_On_Connect_Failure(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception();
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.GetOrAddMessageConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose());
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync disposes connection and throws on write failure"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Disposes_Connection_And_Throws_On_Write_Failure(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception();
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.GetOrAddMessageConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose());
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync pierces firewall with correct token"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Pierces_Firewall_With_Correct_Token(string username, IPEndPoint endpoint, int token)
        {
            var expectedMessage = new PierceFirewall(token).ToByteArray();
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                (await manager.GetOrAddMessageConnectionAsync(ctpr)).Dispose();
            }

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(expectedMessage)), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR generates expected diagnostic on successful connection"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Generates_Expected_Diagnostic_On_Successful_Connection(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                (await manager.GetOrAddMessageConnectionAsync(ctpr)).Dispose();
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Attempting indirect message connection"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Message connection to {username}") && s.ContainsInsensitive("established"))), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns existing connection if exists"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Returns_Existing_Connection_If_Exists(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, _) = GetFixture();

            using (manager)
            using (var sem = new SemaphoreSlim(1, 1))
            {
                manager.SetProperty("MessageConnectionDictionary", dict);

                using (var existingConn = await manager.GetOrAddMessageConnectionAsync(username, endpoint, CancellationToken.None))
                {
                    Assert.Equal(conn.Object, existingConn);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns direct connection when direct connects first"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Returns_Direct_Connection_When_Direct_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IMessageConnection>(It.Is<WaitKey>(k => k.TokenParts.Contains(Constants.WaitKey.SolicitedPeerConnection)), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns indirect connection when indirect connects first"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Returns_Indirect_Connection_When_Indirect_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var incomingIndirect = GetConnectionMock(iendpoint);
            incomingIndirect.Setup(m => m.IPEndPoint)
                .Returns(iendpoint);
            incomingIndirect.Setup(m => m.HandoffTcpClient())
                .Returns(new Mock<ITcpClient>().Object);

            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(incomingIndirect.Object));

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Equal(indirect.Object, newConn);
                Assert.Equal(ConnectionTypes.Indirect, newConn.Type);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync throws ConnectionException when direct and indirect connections fail"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Throws_ConnectionException_When_Direct_And_Indirect_Connections_Fail(string localUsername, string username, IPAddress ipAddress, int directPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync generates expected diagnostics on successful connection"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Generates_Expected_Diagnostics(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IMessageConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Contains(diagnostics, s => s.ContainsInsensitive("Attempting simultaneous direct and indirect message connections"));
                Assert.Contains(diagnostics, s => s.ContainsInsensitive($"established first, attempting to cancel"));
                Assert.Contains(
                    diagnostics,
                    s => s.ContainsInsensitive("message connection to") && s.ContainsInsensitive("established."));
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync sends PeerInit on direct connection established"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Sends_PeerInit_On_Direct_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Peer, token).ToByteArray();

            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(token);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IMessageConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);

                direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(peerInit)), It.IsAny<CancellationToken?>()), Times.Once);
            }
        }

        private (PeerConnectionManager Manager, Mocks Mocks) GetFixture(string username = null, IPEndPoint endpoint = null, SoulseekClientOptions options = null)
        {
            var mocks = new Mocks(options);

            mocks.ServerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.ServerConnection.Setup(m => m.IPEndPoint)
                .Returns(endpoint ?? new IPEndPoint(IPAddress.None, 0));

            var handler = new PeerConnectionManager(
                mocks.Client.Object,
                mocks.ConnectionFactory.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private Mock<IMessageConnection> GetMessageConnectionMock(string username, IPEndPoint endpoint)
        {
            var mock = new Mock<IMessageConnection>();
            mock.Setup(m => m.Username).Returns(username);
            mock.Setup(m => m.IPEndPoint).Returns(endpoint);

            return mock;
        }

        private Mock<IConnection> GetConnectionMock(IPEndPoint endpoint)
        {
            var mock = new Mock<IConnection>();
            mock.Setup(m => m.IPEndPoint)
                .Returns(endpoint);

            return mock;
        }

        private class Mocks
        {
            public Mocks(SoulseekClientOptions clientOptions = null)
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
