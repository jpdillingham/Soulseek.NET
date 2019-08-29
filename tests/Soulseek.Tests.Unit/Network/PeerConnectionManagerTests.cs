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
    using System.Net.Sockets;
    using System.Text;
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
        [Fact(DisplayName = "RemoveAndDisposeAll removes and disposes all")]
        public void RemoveAndDisposeAll_Removes_And_Disposes_All()
        {
            var (manager, _) = GetFixture();

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
        internal async Task AddTransferConnectionAsync_Reads_Token_And_Completes_Wait(string username, IPAddress ipAddress, int port, int token)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.TcpClient.Setup(m => m.RemoteEndPoint)
                .Returns(new IPEndPoint(ipAddress, port));

            using (manager)
            {
                await manager.AddTransferConnectionAsync(username, token, mocks.TcpClient.Object);
            }

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(Constants.WaitKey.DirectTransfer, username, token), conn.Object));
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync disposes connection on exception"), AutoData]
        internal async Task AddTransferConnectionAsync_Disposes_Connection_On_Exception(string username, IPAddress ipAddress, int port, int token)
        {
            var expectedEx = new Exception("foo");

            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.TcpClient.Setup(m => m.RemoteEndPoint)
                .Returns(new IPEndPoint(ipAddress, port));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.AddTransferConnectionAsync(username, token, mocks.TcpClient.Object));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync starts reading"), AutoData]
        internal async Task AddMessageConnectionAsync_Starts_Reading(string username, IPAddress ipAddress, int port, int token)
        {
            var conn = GetMessageConnectionMock(username, ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.TcpClient.Setup(m => m.RemoteEndPoint)
                .Returns(new IPEndPoint(ipAddress, port));

            using (manager)
            {
                await manager.AddMessageConnectionAsync(username, mocks.TcpClient.Object);
            }

            conn.Verify(m => m.StartReadingContinuously());
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync adds connection"), AutoData]
        internal async Task AddMessageConnectionAsync_Adds_Connection(string username, IPAddress ipAddress, int port, int token)
        {
            var conn = GetMessageConnectionMock(username, ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.TcpClient.Setup(m => m.RemoteEndPoint)
                .Returns(new IPEndPoint(ipAddress, port));

            using (manager)
            {
                await manager.AddMessageConnectionAsync(username, mocks.TcpClient.Object);

                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPAddress == ipAddress && c.Port == port);
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync replaces duplicate connection and disposes old"), AutoData]
        internal async Task AddMessageConnectionAsync_Replaces_Duplicate_Connection_And_Disposes_Old(string username, IPAddress ipAddress, int port, int token)
        {
            var conn1 = GetMessageConnectionMock(username, ipAddress, port);
            conn1.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn1.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var conn2 = GetMessageConnectionMock(username, ipAddress, port);
            conn2.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn2.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            mocks.TcpClient.Setup(m => m.RemoteEndPoint)
                .Returns(new IPEndPoint(ipAddress, port));

            using (manager)
            {
                await manager.AddMessageConnectionAsync(username, mocks.TcpClient.Object);

                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPAddress == ipAddress && c.Port == port);

                // swap in the second connection
                mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                    .Returns(conn2.Object);

                // call this again to force the first connection out and second in its place
                await manager.AddMessageConnectionAsync(username, mocks.TcpClient.Object);

                // make sure we still have just the one
                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPAddress == ipAddress && c.Port == port);

                // verify that the first connection was disposed
                conn1.Verify(m => m.Dispose());
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync connects and pierces firewall"), AutoData]
        internal async Task GetTransferConnectionAsync_Connects_And_Pierces_Firewall(string username, IPAddress ipAddress, int port, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", ipAddress, port, token);
            var expectedBytes = new PierceFirewall(token).ToByteArray();
            byte[] actualBytes = Array.Empty<byte>();

            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask)
                .Callback<byte[], CancellationToken>((b, c) => actualBytes = b);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            (IConnection Connection, int RemoteToken) newConn = default;

            using (manager)
            {
                newConn = await manager.GetTransferConnectionAsync(ctpr);
            }

            Assert.Equal(ipAddress, newConn.Connection.IPAddress);
            Assert.Equal(port, newConn.Connection.Port);
            Assert.Equal(token, newConn.RemoteToken);

            Assert.Equal(expectedBytes, actualBytes);

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken?>()), Times.Once);
            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync disposes connection if connect fails"), AutoData]
        internal async Task GetTransferConnectionAsync_Disposes_Connection_If_Connect_Fails(string username, IPAddress ipAddress, int port, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", ipAddress, port, token);
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), null))
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
        internal async Task GetTransferConnectionOutboundDirectAsync_Disposes_Connection_If_Connect_Fails(IPAddress ipAddress, int port, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", ipAddress, port, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync returns connection if connect succeeds"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Returns_Connection_If_Connect_Succeeds(IPAddress ipAddress, int port, int token)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", ipAddress, port, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync sets connection context to Direct"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Sets_Connection_Context_To_Direct(IPAddress ipAddress, int port, int token)
        {
            object context = null;

            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.SetupSet(m => m.Context = It.IsAny<string>())
                .Callback<object>(o => context = o);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", ipAddress, port, token, CancellationToken.None))
            {
                Assert.Equal(Constants.ConnectionMethod.Direct, context);
            }

            conn.VerifySet(m => m.Context = Constants.ConnectionMethod.Direct);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync sends ConnectToPeerRequest"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Sends_ConnectToPeerRequest(IPAddress ipAddress, int port, string username, int token)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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
        internal async Task GetTransferConnectionOutboundIndirectAsync_Throws_If_Wait_Throws(IPAddress ipAddress, int port, string username, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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
        internal async Task GetTransferConnectionOutboundIndirectAsync_Hands_Off_ITcpConnection(IPAddress ipAddress, int port, string username, int token)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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
        internal async Task GetTransferConnectionOutboundIndirectAsync_Sets_Connection_Context_To_Indirect(IPAddress ipAddress, int port, string username, int token)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            conn.VerifySet(m => m.Context = Constants.ConnectionMethod.Indirect);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync adds and removes from PendingSolicitationDictionary"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Adds_And_Removes_From_PendingSolicitationDictionary(IPAddress ipAddress, int port, string username, int token)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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

            conn.VerifySet(m => m.Context = Constants.ConnectionMethod.Indirect);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync returns direct connection when direct connects first"), AutoData]
        internal async Task GetTransferConnectionAsync_Returns_Direct_Connection_When_Direct_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var direct = GetConnectionMock(ipAddress, directPort);
            direct.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Direct);

            var indirect = GetConnectionMock(ipAddress, indirectPort);
            indirect.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), directPort, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), indirectPort, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, ipAddress, directPort, token, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(Constants.ConnectionMethod.Direct, newConn.Context);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync returns indirect connection when indirect connects first"), AutoData]
        internal async Task GetTransferConnectionAsync_Returns_Indirect_Connection_When_Indirect_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var direct = GetConnectionMock(ipAddress, directPort);
            direct.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var indirect = GetConnectionMock(ipAddress, indirectPort);
            indirect.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), directPort, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), indirectPort, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(indirect.Object));

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, ipAddress, directPort, token, CancellationToken.None))
            {
                Assert.Equal(indirect.Object, newConn);
                Assert.Equal(Constants.ConnectionMethod.Indirect, newConn.Context);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync throws ConnectionException when direct and indirect connections fail"), AutoData]
        internal async Task GetTransferConnectionAsync_Throws_ConnectionException_When_Direct_And_Indirect_Connections_Fail(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var direct = GetConnectionMock(ipAddress, directPort);
            direct.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var indirect = GetConnectionMock(ipAddress, indirectPort);
            indirect.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), directPort, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), indirectPort, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.GetTransferConnectionAsync(username, ipAddress, directPort, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync generates expected diagnostics on successful connection"), AutoData]
        internal async Task GetTransferConnectionAsync_Generates_Expected_Diagnostics(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Transfer, token).ToByteArray();

            var direct = GetConnectionMock(ipAddress, directPort);
            direct.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Direct);

            var indirect = GetConnectionMock(ipAddress, indirectPort);
            indirect.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), directPort, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), indirectPort, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, ipAddress, directPort, token, CancellationToken.None))
            {
                Assert.Contains(diagnostics, s => s.Contains("Attempting direct and indirect transfer connections", StringComparison.InvariantCultureIgnoreCase));
                Assert.Contains(diagnostics, s => s.Contains($"established; cancelling", StringComparison.InvariantCultureIgnoreCase));
                Assert.Contains(
                    diagnostics,
                    s => s.Contains("transfer connection to", StringComparison.InvariantCultureIgnoreCase) && s.Contains("established.", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync sends PeerInit on direct connection established"), AutoData]
        internal async Task GetTransferConnectionAsync_Sends_PeerInit_On_Direct_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Transfer, token).ToByteArray();

            var direct = GetConnectionMock(ipAddress, directPort);
            direct.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Direct);

            var indirect = GetConnectionMock(ipAddress, indirectPort);
            indirect.Setup(m => m.Context)
                .Returns(Constants.ConnectionMethod.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), directPort, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), indirectPort, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, ipAddress, directPort, token, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(Constants.ConnectionMethod.Direct, newConn.Context);

                direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(peerInit)), It.IsAny<CancellationToken?>()), Times.Once);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync writes token on connection established"), AutoData]
        internal async Task GetTransferConnectionAsync_Writes_Token_On_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            {
                var direct = GetConnectionMock(ipAddress, directPort);
                direct.Setup(m => m.Context)
                    .Returns(Constants.ConnectionMethod.Direct);

                var indirect = GetConnectionMock(ipAddress, indirectPort);
                indirect.Setup(m => m.Context)
                    .Returns(Constants.ConnectionMethod.Indirect);

                var (manager, mocks) = GetFixture();

                mocks.Client.Setup(m => m.Username)
                    .Returns(localUsername);

                mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), directPort, It.IsAny<ConnectionOptions>(), null))
                    .Returns(direct.Object);
                mocks.ConnectionFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), indirectPort, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                    .Returns(indirect.Object);

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                    .Throws(new Exception());

                using (manager)
                using (var newConn = await manager.GetTransferConnectionAsync(username, ipAddress, directPort, token, CancellationToken.None))
                {
                    Assert.Equal(direct.Object, newConn);
                    Assert.Equal(Constants.ConnectionMethod.Direct, newConn.Context);

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
                .Returns(new ConnectionKey(username, IPAddress.None, 0));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

            using (var semaphore = new SemaphoreSlim(1))
            {
                dict.TryAdd(username, (semaphore, conn.Object));

                var (manager, mocks) = GetFixture();

                manager.SetProperty("MessageConnectionDictionary", dict);

                using (manager)
                {
                    var ms = manager.GetProperty<SemaphoreSlim>("MessageSemaphore");
                    ms.WaitAsync();

                    manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, message);

                    Assert.Empty(dict);
                }
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected generates diagnostic on removal"), AutoData]
        internal void MessageConnection_Disconnected_Generates_Diagnostic_On_Removal(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, IPAddress.None, 0));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

            using (var semaphore = new SemaphoreSlim(1))
            {
                dict.TryAdd(username, (semaphore, conn.Object));

                var (manager, mocks) = GetFixture();

                List<string> diagnostics = new List<string>();

                mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                    .Callback<string>(s => diagnostics.Add(s));

                manager.SetProperty("MessageConnectionDictionary", dict);

                using (manager)
                {
                    var ms = manager.GetProperty<SemaphoreSlim>("MessageSemaphore");
                    ms.WaitAsync();

                    manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, message);

                    Assert.Single(diagnostics);
                    Assert.Contains(diagnostics, m => m.Contains($"Removing message connection to {username}", StringComparison.InvariantCultureIgnoreCase));
                }
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected does not throw if connection isn't tracked"), AutoData]
        internal void MessageConnection_Disconnected_Does_Not_Throw_If_Connection_Isnt_Tracked(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, IPAddress.None, 0));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

            var (manager, mocks) = GetFixture();

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                var ms = manager.GetProperty<SemaphoreSlim>("MessageSemaphore");
                ms.WaitAsync();

                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, message);

                Assert.Empty(dict);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected does not generate diagnostic if connection isn't tracked"), AutoData]
        internal void MessageConnection_Disconnected_Does_Generate_Diagnostic_If_Connection_Isnt_Tracked(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, IPAddress.None, 0));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

            var (manager, mocks) = GetFixture();

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                var ms = manager.GetProperty<SemaphoreSlim>("MessageSemaphore");
                ms.WaitAsync();

                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, message);

                Assert.Empty(diagnostics);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync connects and returns connection if connect succeeds"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Connects_And_Returns_Connection_If_Connect_Succeeds(string username, IPAddress ipAddress, int port)
        {
            var conn = GetMessageConnectionMock(username, ipAddress, port);
            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, ipAddress, port, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actualConn);
            }

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync disposes connection if connect fails"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Disposes_Connection_If_Connect_Fails(string username, IPAddress ipAddress, int port)
        {
            var expectedEx = new Exception("foo");

            var conn = GetMessageConnectionMock(username, ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(async () => await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, ipAddress, port, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync sets context to Direct"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Sets_Context_To_Direct(string username, IPAddress ipAddress, int port)
        {
            var conn = GetMessageConnectionMock(username, ipAddress, port);
            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, ipAddress, port, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actualConn);
            }

            conn.VerifySet(m => m.Context = Constants.ConnectionMethod.Direct);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync sends ConnectToPeerRequest"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Sends_ConnectToPeerRequest(IPAddress ipAddress, int port, string username)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, ipAddress, port);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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
        internal async Task GetMessageConnectionOutboundIndirectAsync_Throws_If_Wait_Throws(IPAddress ipAddress, int port, string username)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, ipAddress, port);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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
        internal async Task GetMessageConnectionOutboundIndirectAsync_Hands_Off_ITcpConnection(IPAddress ipAddress, int port, string username)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, ipAddress, port);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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
        internal async Task GetMessageConnectionOutboundIndirectAsync_Sets_Connection_Context_To_Indirect(IPAddress ipAddress, int port, string username)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, ipAddress, port);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, newConn);
            }

            msgConn.VerifySet(m => m.Context = Constants.ConnectionMethod.Indirect);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync adds and removes from PendingSolicitationDictionary"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Adds_And_Removes_From_PendingSolicitationDictionary(IPAddress ipAddress, int port, string username)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, ipAddress, port);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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

            msgConn.VerifySet(m => m.Context = Constants.ConnectionMethod.Indirect);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync calls StartReadingContinuously"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Calls_StartReadingContinuously(IPAddress ipAddress, int port, string username)
        {
            var conn = GetConnectionMock(ipAddress, port);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, ipAddress, port);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, ipAddress, port, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
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

            msgConn.Verify(m => m.StartReadingContinuously(), Times.Once);
        }

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

        private Mock<IMessageConnection> GetMessageConnectionMock(string username, IPAddress ip, int port)
        {
            var mock = new Mock<IMessageConnection>();
            mock.Setup(m => m.Username).Returns(username);
            mock.Setup(m => m.IPAddress).Returns(ip);
            mock.Setup(m => m.Port).Returns(port);

            return mock;
        }

        private Mock<IConnection> GetConnectionMock(IPAddress ip, int port)
        {
            var mock = new Mock<IConnection>();
            mock.Setup(m => m.IPAddress)
                .Returns(ip);
            mock.Setup(m => m.Port)
                .Returns(port);

            return mock;
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
