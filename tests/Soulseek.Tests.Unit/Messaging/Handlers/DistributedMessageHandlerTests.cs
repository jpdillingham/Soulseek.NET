// <copyright file="DistributedMessageHandlerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Handlers
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

    public class DistributedMessageHandlerTests
    {
        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates diagnostic on message")]
        public void Creates_Diagnostic_On_Message()
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(1)
                .Build();

            handler.HandleMessage(conn.Object, message);

            mocks.Diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates unhandled diagnostic on unhandled message")]
        public void Creates_Unhandled_Diagnostic_On_Unhandled_Message()
        {
            string msg = null;
            var (handler, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(m => msg = m);

            var message = new MessageBuilder().WriteCode(MessageCode.Distributed.Unknown).Build();

            handler.HandleMessage(new Mock<IMessageConnection>().Object, message);

            mocks.Diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Exactly(2));

            Assert.Contains("Unhandled", msg, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Raises DiagnosticGenerated on Exception")]
        public void Raises_DiagnosticGenerated_On_Exception()
        {
            var mocks = new Mocks();
            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .Build();

            var diagnostics = new List<DiagnosticGeneratedEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);
            handler.HandleMessage(null, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Warning)
                .Where(d => d.Message.IndexOf("Error handling server message", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles BranchLevel"), AutoData]
        public void Handles_BranchLevel(IPAddress ip, int port, int level)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Context).Returns(null);
            conn.Setup(m => m.IPAddress).Returns(ip);
            conn.Setup(m => m.Port).Returns(port);
            conn.Setup(m => m.Username).Returns("foo");
            conn.Setup(m => m.Key).Returns(new ConnectionKey(ip, port));

            var key = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Context, conn.Object.Key);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(level)
                .Build();

            handler.HandleMessage(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete<int>(key, level), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sets BranchLevel on message from parent"), AutoData]
        public void Sets_BranchLevel_On_Message_From_Parent(string parent, IPAddress ip, int port, int level)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Context).Returns(null);
            conn.Setup(m => m.IPAddress).Returns(ip);
            conn.Setup(m => m.Port).Returns(port);
            conn.Setup(m => m.Username).Returns(parent);
            conn.Setup(m => m.Key).Returns(new ConnectionKey(ip, port));

            var key = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Context, conn.Object.Key);

            mocks.DistributedConnectionManager.Setup(m => m.Parent).Returns((parent, ip, port));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(level)
                .Build();

            handler.HandleMessage(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete<int>(key, level), Times.Once);
            mocks.DistributedConnectionManager.Verify(m => m.SetBranchLevel(level), Times.Once);
        }

        private (DistributedMessageHandler Handler, Mocks Mocks) GetFixture(ClientOptions clientOptions = null)
        {
            var mocks = new Mocks(clientOptions);

            var handler = new DistributedMessageHandler(
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

                Client.Setup(m => m.ServerConnection).Returns(ServerConnection.Object);
                Client.Setup(m => m.PeerConnectionManager).Returns(PeerConnectionManager.Object);
                Client.Setup(m => m.DistributedConnectionManager).Returns(DistributedConnectionManager.Object);
                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.Downloads).Returns(Downloads);
                Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                Client.Setup(m => m.Options).Returns(clientOptions ?? new ClientOptions());
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
            public Mock<IDistributedConnectionManager> DistributedConnectionManager { get; } = new Mock<IDistributedConnectionManager>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public ConcurrentDictionary<int, Transfer> Downloads { get; } = new ConcurrentDictionary<int, Transfer>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
        }
    }
}
