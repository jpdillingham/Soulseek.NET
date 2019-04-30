// <copyright file="ConnectionManagerTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class ConnectionManagerTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            ConnectionManager c = null;

            var ex = Record.Exception(() => c = new ConnectionManager(1000));

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(1000, c.ConcurrentPeerConnections);
            Assert.Equal(0, c.ActivePeerConnections);
            Assert.Equal(0, c.WaitingPeerConnections);
            Assert.Equal(0, c.ActiveTransferConnections);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException when ConcurrentPeerConnections is less than 1.")]
        public void Throws_ArgumentOutOfRangeException_When_ConcurrentPeerConnections_Is_Less_than_1()
        {
            ConnectionManager c = null;

            var ex = Record.Exception(() => c = new ConnectionManager(0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var c = new ConnectionManager(1);

            var ex = Record.Exception(() => c.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "RemoveAndDisposeAll")]
        [Theory(DisplayName = "RemoveAndDisposeAll removes and disposes all"), AutoData]
        public void RemoveAndDisposeAll_Removes_And_Disposes_All(IPAddress ip, int port)
        {
            var c = new ConnectionManager(1);

            var peer = new ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();
            peer.GetOrAdd(new ConnectionKey(ip, port), (new SemaphoreSlim(1), new Mock<IMessageConnection>().Object));

            c.SetProperty("PeerConnections", peer);

            var transfer = new ConcurrentDictionary<(ConnectionKey, int), IConnection>();
            transfer.GetOrAdd((new ConnectionKey(ip, port), port), new Mock<IConnection>().Object);

            c.SetProperty("TransferConnections", transfer);

            var activePeerBefore = c.ActivePeerConnections;
            var activeTransferBefore = c.ActiveTransferConnections;

            c.RemoveAndDisposeAll();

            Assert.Equal(1, activePeerBefore);
            Assert.Equal(1, activeTransferBefore);

            Assert.Empty(c.GetProperty<ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>>("PeerConnections"));
            Assert.Empty(c.GetProperty<ConcurrentDictionary<(ConnectionKey, int), IConnection>>("TransferConnections"));
        }

        [Trait("Category", "AddUnsolicitedTransferConnectionAsync")]
        [Theory(DisplayName = "AddUnsolicitedTransferConnectionAsync connects and sends PeerInit"), AutoData]
        internal async Task AddUnsolicitedTransferConnectionAsync_Connects_And_Sends_PeerInit(string username, IPAddress ipAddress, int port, int token, ConnectionOptions options)
        {
            var key = new ConnectionKey(ipAddress, port);
            var expectedBytes = new PeerInitRequest(username, "F", token).ToMessage().ToByteArray();
            byte[] actualBytes = Array.Empty<byte>();

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            conn.Setup(m => m.Port)
                .Returns(port);
            conn.Setup(m => m.ConnectAsync())
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<byte[], CancellationToken>((b, c) => actualBytes = b);

            var connFactory = new Mock<IConnectionFactory>();
            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            IConnection newConn = null;

            using (var c = new ConnectionManager(1, connFactory.Object))
            {
                newConn = await c.AddUnsolicitedTransferConnectionAsync(key, token, username, options, CancellationToken.None);
            }

            Assert.Equal(ipAddress, newConn.IPAddress);
            Assert.Equal(port, newConn.Port);

            Assert.Equal(expectedBytes, actualBytes);

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "AddSolicitedTransferConnectionAsync")]
        [Theory(DisplayName = "AddSolicitedTransferConnectionAsync connects and pierces firewall"), AutoData]
        internal async Task AddSolicitedTransferConnectionAsync_Connects_And_Pierces_Firewall(string username, IPAddress ipAddress, int port, int token, ConnectionOptions options)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", ipAddress, port, token);
            var expectedBytes = new PierceFirewallRequest(token).ToMessage().ToByteArray();
            byte[] actualBytes = Array.Empty<byte>();

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            conn.Setup(m => m.Port)
                .Returns(port);
            conn.Setup(m => m.ConnectAsync())
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<byte[], CancellationToken>((b, c) => actualBytes = b);

            var connFactory = new Mock<IConnectionFactory>();
            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            IConnection newConn = null;

            using (var c = new ConnectionManager(1, connFactory.Object))
            {
                newConn = await c.AddSolicitedTransferConnectionAsync(ctpr, options, CancellationToken.None);
            }

            Assert.Equal(ipAddress, newConn.IPAddress);
            Assert.Equal(port, newConn.Port);

            Assert.Equal(expectedBytes, actualBytes);

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
