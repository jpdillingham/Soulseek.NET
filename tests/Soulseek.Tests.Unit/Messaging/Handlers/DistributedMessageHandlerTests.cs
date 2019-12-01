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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
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
            var handler = new DistributedMessageHandler(
                mocks.Client.Object);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .Build();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);
            handler.HandleMessage(conn.Object, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Warning)
                .Where(d => d.Message.IndexOf("Error handling distributed message", StringComparison.InvariantCultureIgnoreCase) > -1)
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

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles BranchRoot"), AutoData]
        public void Handles_BranchRoot(IPAddress ip, int port, string root)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Context).Returns(null);
            conn.Setup(m => m.IPAddress).Returns(ip);
            conn.Setup(m => m.Port).Returns(port);
            conn.Setup(m => m.Username).Returns("foo");
            conn.Setup(m => m.Key).Returns(new ConnectionKey(ip, port));

            var key = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Context, conn.Object.Key);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchRoot)
                .WriteString(root)
                .Build();

            handler.HandleMessage(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete<string>(key, root), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sets BranchRoot on message from parent"), AutoData]
        public void Sets_BranchRoot_On_Message_From_Parent(string parent, IPAddress ip, int port, string root)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Context).Returns(null);
            conn.Setup(m => m.IPAddress).Returns(ip);
            conn.Setup(m => m.Port).Returns(port);
            conn.Setup(m => m.Username).Returns(parent);
            conn.Setup(m => m.Key).Returns(new ConnectionKey(ip, port));

            var key = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Context, conn.Object.Key);

            mocks.DistributedConnectionManager.Setup(m => m.Parent).Returns((parent, ip, port));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchRoot)
                .WriteString(root)
                .Build();

            handler.HandleMessage(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete<string>(key, root), Times.Once);
            mocks.DistributedConnectionManager.Verify(m => m.SetBranchRoot(root), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ChildDepth"), AutoData]
        public void Handles_ChildDepth(IPAddress ip, int port, int depth)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Context).Returns(null);
            conn.Setup(m => m.IPAddress).Returns(ip);
            conn.Setup(m => m.Port).Returns(port);
            conn.Setup(m => m.Username).Returns("foo");
            conn.Setup(m => m.Key).Returns(new ConnectionKey(ip, port));

            var key = new WaitKey(Constants.WaitKey.ChildDepthMessage, conn.Object.Key);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(depth)
                .Build();

            handler.HandleMessage(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete<int>(key, depth), Times.Once);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Handles Ping")]
        public void Handles_Ping()
        {
            var (handler, mocks) = GetFixture();

            byte[] msg = null;

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Callback<byte[], CancellationToken?>((b, c) => msg = b);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .Build();

            handler.HandleMessage(conn.Object, message);

            var reader = new MessageReader<MessageCode.Distributed>(msg);

            Assert.Equal(MessageCode.Distributed.Ping, reader.ReadCode());
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes SearchRequest wait"), AutoData]
        public void Completes_SearchRequest_Wait(string username, int token, string query, IPAddress ip, int port)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Context).Returns(null);
            conn.Setup(m => m.Key).Returns(new ConnectionKey(ip, port));

            var key = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Context, conn.Object.Key);

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessage(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete(key), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Broadcasts SearchRequest"), AutoData]
        public void Broadcasts_SearchRequest(string username, int token, string query)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessage(conn.Object, message);

            mocks.DistributedConnectionManager.Verify(m => m.BroadcastMessageAsync(message, It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on SearchResponseResolver Exception"), AutoData]
        public void Raises_DiagnosticGenerated_On_SearchResponseResolver_Exception(string username, int token, string query)
        {
            var mocks = new Mocks();
            var handler = new DistributedMessageHandler(
                mocks.Client.Object);

            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(searchResponseResolver: (a, b, c) => throw new Exception()));

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);
            handler.HandleMessage(conn.Object, message);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Warning)
                .Where(d => d.Message.IndexOf("Error resolving search response", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Responds to SearchRequest"), AutoData]
        public void Responds_To_SearchRequest(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, 1, 1, 1, 1, new List<File>() { new File(1, "1", 1, "1", 0) });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserAddressAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((IPAddress.None, 0)));

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, IPAddress.None, 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessage(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserAddressAsync(username, It.IsAny<CancellationToken?>()), Times.Once);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, IPAddress.None, 0, It.IsAny<CancellationToken>()), Times.Once);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest if result is null"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_If_Result_Is_Null(string username, int token, string query)
        {
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult<SearchResponse>(null));
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserAddressAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((IPAddress.None, 0)));

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, IPAddress.None, 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessage(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserAddressAsync(username, It.IsAny<CancellationToken?>()), Times.Never);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, IPAddress.None, 0, It.IsAny<CancellationToken>()), Times.Never);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), null), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest if result contains no files"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_If_Result_Contains_No_Files(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, 0, 1, 1, 1, new List<File>());
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserAddressAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((IPAddress.None, 0)));

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, IPAddress.None, 0, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessage(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserAddressAsync(username, It.IsAny<CancellationToken?>()), Times.Never);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, IPAddress.None, 0, It.IsAny<CancellationToken>()), Times.Never);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), null), Times.Never);
        }

        private (DistributedMessageHandler Handler, Mocks Mocks) GetFixture(SoulseekClientOptions clientOptions = null)
        {
            var mocks = new Mocks(clientOptions);

            var handler = new DistributedMessageHandler(
                mocks.Client.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
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
                Client.Setup(m => m.PeerConnectionManager).Returns(PeerConnectionManager.Object);
                Client.Setup(m => m.DistributedConnectionManager).Returns(DistributedConnectionManager.Object);
                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.Downloads).Returns(Downloads);
                Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                Client.Setup(m => m.Options).Returns(clientOptions ?? new SoulseekClientOptions());
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
            public Mock<IDistributedConnectionManager> DistributedConnectionManager { get; } = new Mock<IDistributedConnectionManager>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public ConcurrentDictionary<int, TransferInternal> Downloads { get; } = new ConcurrentDictionary<int, TransferInternal>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
        }
    }
}
