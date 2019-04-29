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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Messaging;
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

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync connects and pierces firewall"), AutoData]
        internal async Task AddTransferConnectionAsync_Connects_And_Pierces_Firewall(string username, string type, IPAddress ipAddress, int port, int token, ConnectionOptions options)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            conn.Setup(m => m.Port)
                .Returns(port);
            conn.Setup(m => m.ConnectAsync())
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            var connFactory = new Mock<IConnectionFactory>();
            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var key = new ConnectionKey(ipAddress, port);

            IConnection newConn = null;

            using (var c = new ConnectionManager(1, connFactory.Object))
            {
                newConn = await c.AddUnsolicitedTransferConnectionAsync(key, token, username, options, CancellationToken.None);
            }

            Assert.Equal(ipAddress, newConn.IPAddress);
            Assert.Equal(port, newConn.Port);

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        //[Trait("Category", "GetSolicitedPeerConnectionAsync")]
        //[Theory(DisplayName = "GetSolicitedPeerConnectionAsync returns expected IMessageConnection instance"), AutoData]
        //public async Task GetSolicitedPeerConnectionAsync_Returns_IMessageConnection_Instance(string username, IPAddress ipAddress, int port, int token)
        //{
        //    var ctpr = new ConnectToPeerResponse(username, "P", ipAddress, port, token);
        //    var options = new ConnectionOptions();

        //    var s = new SoulseekClient();

        //    IMessageConnection conn = null;

        //    var ex = await Record.ExceptionAsync(async () => conn = await s.InvokeMethod<Task<IMessageConnection>>("GetSolicitedPeerConnectionAsync", ctpr, options, CancellationToken.None));

        //    Assert.Null(ex);
        //    Assert.NotNull(conn);

        //    Assert.Equal(username, conn.Username);
        //    Assert.Equal(ipAddress, conn.IPAddress);
        //    Assert.Equal(port, conn.Port);
        //    Assert.Equal(ctpr, conn.Context);
        //}

        //[Trait("Category", "GetSolicitedPeerConnectionAsync")]
        //[Theory(DisplayName = "GetSolicitedPeerConnectionAsync adds instance to PeerConnectionManager"), AutoData]
        //public async Task GetSolicitedPeerConnectionAsync_Adds_Instance_To_PeerConnectionManager(string username, IPAddress ipAddress, int port, int token)
        //{
        //    var ctpr = new ConnectToPeerResponse(username, "P", ipAddress, port, token);
        //    var options = new ConnectionOptions();

        //    var pcm = new Mock<IConnectionManager>();
        //    pcm.Setup(m => m.AddAsync(It.IsAny<IMessageConnection>()))
        //        .Returns(Task.CompletedTask);

        //    var s = new SoulseekClient("127.0.0.1", 1, peerConnectionManager: pcm.Object);

        //    await s.InvokeMethod<Task<IMessageConnection>>("GetSolicitedPeerConnectionAsync", ctpr, options, CancellationToken.None);

        //    pcm.Verify(m => m.AddAsync(It.IsAny<IMessageConnection>()), Times.Once);
        //}

        //[Trait("Category", "GetUnsolicitedPeerConnectionAsync")]
        //[Theory(DisplayName = "GetUnsolicitedPeerConnectionAsync returns new connection if not existing"), AutoData]
        //public async Task GetUnsolicitedPeerConnectionAsync_Returns_New_Connection_If_Not_Existing(string name, IPAddress ipAddress, int port)
        //{
        //    var options = new ConnectionOptions();

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(name, ipAddress, port)));

        //    var serverConn = new Mock<IMessageConnection>();
        //    serverConn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var s = new SoulseekClient("127.0.0.1", 1, serverConnection: serverConn.Object, waiter: waiter.Object);

        //    var conn = await s.InvokeMethod<Task<IMessageConnection>>("GetUnsolicitedPeerConnectionAsync", name, options, CancellationToken.None);

        //    Assert.NotNull(conn);
        //    Assert.Equal(name, conn.Username);
        //    Assert.Equal(ipAddress, conn.IPAddress);
        //    Assert.Equal(port, conn.Port);
        //    Assert.Equal(options, conn.Options);
        //}

        //[Trait("Category", "GetUnsolicitedPeerConnectionAsync")]
        //[Theory(DisplayName = "GetUnsolicitedPeerConnectionAsync returns existing connection if existing and not disconnected"), AutoData]
        //public async Task GetUnsolicitedPeerConnectionAsync_Returns_Existing_Connection_If_Existing_And_Not_Disconnected(string username, IPAddress ipAddress, int port)
        //{
        //    var options = new ConnectionOptions();
        //    var existingConn = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port, options);

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ipAddress, port)));

        //    var pcm = new Mock<IConnectionManager>();
        //    pcm.Setup(m => m.Get(It.IsAny<ConnectionKey>()))
        //        .Returns(existingConn);

        //    var s = new SoulseekClient("127.0.0.1", 1, peerConnectionManager: pcm.Object, waiter: waiter.Object);

        //    var conn = await s.InvokeMethod<Task<IMessageConnection>>("GetUnsolicitedPeerConnectionAsync", username, options, CancellationToken.None);

        //    Assert.NotNull(conn);
        //    Assert.Equal(username, conn.Username);
        //    Assert.Equal(ipAddress, conn.IPAddress);
        //    Assert.Equal(port, conn.Port);
        //    Assert.Equal(options, conn.Options);
        //}

        //[Trait("Category", "GetUnsolicitedPeerConnectionAsync")]
        //[Theory(DisplayName = "GetUnsolicitedPeerConnectionAsync removes disconnected connection"), AutoData]
        //public async Task GetUnsolicitedPeerConnectionAsync_Removes_Disconnected_Connection(string username, IPAddress ipAddress, int port)
        //{
        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ipAddress, port)));

        //    var existingConn = new Mock<IMessageConnection>();
        //    existingConn.Setup(m => m.State)
        //        .Returns(ConnectionState.Disconnected);

        //    var pcm = new Mock<IConnectionManager>();
        //    pcm.Setup(m => m.Get(It.IsAny<ConnectionKey>()))
        //        .Returns(existingConn.Object);
        //    pcm.Setup(m => m.RemoveAsync(It.IsAny<IMessageConnection>()))
        //        .Returns(Task.CompletedTask);
        //    pcm.Setup(m => m.GetMessageConnection(MessageConnectionType.Peer, username, ipAddress, port, It.IsAny<ConnectionOptions>()))
        //        .Returns(new Mock<IMessageConnection>().Object);

        //    var serverConn = new Mock<IMessageConnection>();
        //    serverConn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
        //        .Returns(Task.CompletedTask);

        //    var s = new SoulseekClient("127.0.0.1", 1, serverConnection: serverConn.Object, peerConnectionManager: pcm.Object, waiter: waiter.Object);

        //    await s.InvokeMethod<Task<IMessageConnection>>("GetUnsolicitedPeerConnectionAsync", username, new ConnectionOptions(), CancellationToken.None);

        //    pcm.Verify(m => m.RemoveAsync(It.IsAny<IMessageConnection>()), Times.Once);
        //}
    }
}
