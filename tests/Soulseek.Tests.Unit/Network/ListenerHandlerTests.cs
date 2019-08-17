// <copyright file="ListenerHandlerTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;
    public class ListenerHandlerTests
    {
        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on connection"), AutoData]
        public void Creates_Diagnostic_On_Connection(IPAddress ip)
        {
            var (handler, mocks) = GetFixture(ip);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            handler.HandleConnection(null, mocks.Connection.Object);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("Accepted incoming connection", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on unknown connection"), AutoData]
        public void Creates_Diagnostic_On_Unknown_Connection(IPAddress ip)
        {
            var (handler, mocks) = GetFixture(ip);

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(5)));

            mocks.Connection.Setup(m => m.ReadAsync(1, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(1)));

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            handler.HandleConnection(null, mocks.Connection.Object);

            var compare = StringComparison.InvariantCultureIgnoreCase;
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("failed to initialize", compare) && s.Contains("Unrecognized initialization message", compare))), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic if connection read throws"), AutoData]
        public void Creates_Diagnostic_If_Connection_Read_Throws(IPAddress ip)
        {
            var (handler, mocks) = GetFixture(ip);

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            handler.HandleConnection(null, mocks.Connection.Object);

            var compare = StringComparison.InvariantCultureIgnoreCase;
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("failed to initialize", compare))), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on PeerInit"), AutoData]
        public void Creates_Diagnostic_On_PeerInit(IPAddress ip, string username, int token)
        {
            var (handler, mocks) = GetFixture(ip);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var message = new PeerInit(username, Constants.ConnectionType.Peer, token);
            var messageBytes = message.ToByteArray().AsSpan().Slice(4).ToArray();

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(messageBytes.Length)));

            mocks.Connection.Setup(m => m.ReadAsync(messageBytes.Length, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(messageBytes));

            handler.HandleConnection(null, mocks.Connection.Object);

            var compare = StringComparison.InvariantCultureIgnoreCase;
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("PeerInit for connection type", compare))), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on unknown PierceFirewall"), AutoData]
        public void Creates_Diagnostic_On_PierceFirewall(IPAddress ip, int token)
        {
            var (handler, mocks) = GetFixture(ip);

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var message = new PierceFirewall(token);
            var messageBytes = message.ToByteArray().AsSpan().Slice(4).ToArray();

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(messageBytes.Length)));

            mocks.Connection.Setup(m => m.ReadAsync(messageBytes.Length, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(messageBytes));

            handler.HandleConnection(null, mocks.Connection.Object);

            var compare = StringComparison.InvariantCultureIgnoreCase;
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("Unknown PierceFirewall", compare))), Times.Once);
        }

        [Trait("Category", "PeerInit")]
        [Theory(DisplayName = "Adds peer connection on peer PeerInit"), AutoData]
        public void Adds_Peer_Connection_On_Peer_PeerInit(IPAddress ip, string username, int token)
        {
            var (handler, mocks) = GetFixture(ip);

            var message = new PeerInit(username, Constants.ConnectionType.Peer, token);
            var messageBytes = message.ToByteArray().AsSpan().Slice(4).ToArray();

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(messageBytes.Length)));

            mocks.Connection.Setup(m => m.ReadAsync(messageBytes.Length, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(messageBytes));

            handler.HandleConnection(null, mocks.Connection.Object);

            mocks.PeerConnectionManager.Verify(m => m.AddMessageConnectionAsync(username, It.IsAny<ITcpClient>()));
        }

        [Trait("Category", "PeerInit")]
        [Theory(DisplayName = "Adds transfer connection on transfer PeerInit"), AutoData]
        public void Adds_Transfer_Connection_On_Transfer_PeerInit(IPAddress ip, string username, int token)
        {
            var (handler, mocks) = GetFixture(ip);

            var message = new PeerInit(username, Constants.ConnectionType.Transfer, token);
            var messageBytes = message.ToByteArray().AsSpan().Slice(4).ToArray();

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(messageBytes.Length)));

            mocks.Connection.Setup(m => m.ReadAsync(messageBytes.Length, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(messageBytes));

            handler.HandleConnection(null, mocks.Connection.Object);

            mocks.PeerConnectionManager.Verify(m => m.AddTransferConnectionAsync(username, token, It.IsAny<ITcpClient>()));
        }

        [Trait("Category", "PeerInit")]
        [Theory(DisplayName = "Adds distributed connection on distributed PeerInit"), AutoData]
        public void Adds_Distributed_Connection_On_Distributed_PeerInit(IPAddress ip, string username, int token)
        {
            var (handler, mocks) = GetFixture(ip);

            var message = new PeerInit(username, Constants.ConnectionType.Distributed, token);
            var messageBytes = message.ToByteArray().AsSpan().Slice(4).ToArray();

            mocks.Connection.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(messageBytes.Length)));

            mocks.Connection.Setup(m => m.ReadAsync(messageBytes.Length, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(messageBytes));

            handler.HandleConnection(null, mocks.Connection.Object);

            mocks.DistributedConnectionManager.Verify(m => m.AddChildConnectionAsync(username, It.IsAny<ITcpClient>()));
        }

        private (ListenerHandler Handler, Mocks Mocks) GetFixture(IPAddress ip, ClientOptions clientOptions = null)
        {
            var mocks = new Mocks(clientOptions);

            mocks.Connection.Setup(m => m.IPAddress).Returns(ip);

            var handler = new ListenerHandler(
                mocks.Client.Object,
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

                Listener.Setup(m => m.Port).Returns(clientOptions?.ListenPort ?? new ClientOptions().ListenPort ?? 0);
                PeerConnectionManager.Setup(m => m.PendingSolicitations)
                    .Returns(new Dictionary<int, string>());
                DistributedConnectionManager.Setup(m => m.PendingSolicitations)
                    .Returns(new Dictionary<int, string>());

                Client.Setup(m => m.PeerConnectionManager).Returns(PeerConnectionManager.Object);
                Client.Setup(m => m.DistributedConnectionManager).Returns(DistributedConnectionManager.Object);
                Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                Client.Setup(m => m.Options).Returns(clientOptions ?? new ClientOptions());
                Client.Setup(m => m.Listener).Returns(Listener.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
            public Mock<IDistributedConnectionManager> DistributedConnectionManager { get; } = new Mock<IDistributedConnectionManager>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<IConnection> Connection { get; } = new Mock<IConnection>();
            public Mock<IListener> Listener { get; } = new Mock<IListener>();
        }
    }
}
