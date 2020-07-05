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
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws given null SoulseekClient")]
        public void Instantiation_Throws_Given_Null_SoulseekClient()
        {
            var ex = Record.Exception(() => new DistributedMessageHandler(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("soulseekClient", ((ArgumentNullException)ex).ParamName);
        }

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

            handler.HandleMessageRead(conn.Object, new MessageReadEventArgs(message));

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

            handler.HandleMessageRead(new Mock<IMessageConnection>().Object, message);

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
            handler.HandleMessageRead(conn.Object, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Warning)
                .Where(d => d.Message.IndexOf("Error handling distributed message", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on diagnostic"), AutoData]
        public void Raises_DiagnosticGenerated_On_Diagnostic(string message)
        {
            using (var client = new SoulseekClient(options: null))
            {
                DiagnosticEventArgs args = default;

                DistributedMessageHandler l = new DistributedMessageHandler(client);
                l.DiagnosticGenerated += (sender, e) => args = e;

                var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");
                diagnostic.Info(message);

                Assert.Equal(message, args.Message);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Does not throw raising DiagnosticGenerated if no handlers bound"), AutoData]
        public void Does_Not_Throw_Raising_DiagnosticGenerated_If_No_Handlers_Bound(string message)
        {
            using (var client = new SoulseekClient(options: null))
            {
                DistributedMessageHandler l = new DistributedMessageHandler(client);

                var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");

                var ex = Record.Exception(() => diagnostic.Info(message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sets BranchLevel on message from parent"), AutoData]
        public void Sets_BranchLevel_On_Message_From_Parent(string parent, IPEndPoint ipEndPoint, int level, Guid id)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Id).Returns(id);
            conn.Setup(m => m.IPEndPoint).Returns(ipEndPoint);
            conn.Setup(m => m.Username).Returns(parent);
            conn.Setup(m => m.Key).Returns(new ConnectionKey(ipEndPoint));

            mocks.DistributedConnectionManager.Setup(m => m.Parent).Returns((parent, ipEndPoint));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(level)
                .Build();

            handler.HandleMessageRead(conn.Object, message);

            mocks.DistributedConnectionManager.Verify(m => m.SetBranchLevel(level), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sets BranchRoot on message from parent"), AutoData]
        public void Sets_BranchRoot_On_Message_From_Parent(string parent, IPEndPoint endpoint, string root, Guid id)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Id).Returns(id);
            conn.Setup(m => m.IPEndPoint).Returns(endpoint);
            conn.Setup(m => m.Username).Returns(parent);
            conn.Setup(m => m.Key).Returns(new ConnectionKey(endpoint));

            mocks.DistributedConnectionManager.Setup(m => m.Parent).Returns((parent, endpoint));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchRoot)
                .WriteString(root)
                .Build();

            handler.HandleMessageRead(conn.Object, message);

            mocks.DistributedConnectionManager.Verify(m => m.SetBranchRoot(root), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ChildDepth"), AutoData]
        public void Handles_ChildDepth(IPEndPoint endpoint, int depth, Guid id)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Id).Returns(id);
            conn.Setup(m => m.IPEndPoint).Returns(endpoint);
            conn.Setup(m => m.Username).Returns("foo");
            conn.Setup(m => m.Key).Returns(new ConnectionKey(endpoint));

            var key = new WaitKey(Constants.WaitKey.ChildDepthMessage, conn.Object.Key);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(depth)
                .Build();

            handler.HandleMessageRead(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete<int>(key, depth), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles Ping"), AutoData]
        public void Handles_Ping(string username, int token)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .WriteInteger(token)
                .Build();

            handler.HandleMessageRead(conn.Object, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Distributed.Ping, username), It.Is<DistributedPingResponse>(r => r.Token == token)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Broadcasts SearchRequest"), AutoData]
        public void Broadcasts_SearchRequest(string username, int token, string query)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);

            mocks.DistributedConnectionManager.Verify(m => m.BroadcastMessageAsync(message, It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Broadcasts ServerSearchRequest as SearchRequest"), AutoData]
        public void Broadcasts_ServerSearchRequest_As_SearchRequest(string username, int token, string query)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ServerSearchRequest)
                .WriteBytes(new byte[8])
                .WriteString(username)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var forwardedMessage = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);

            mocks.DistributedConnectionManager
                .Verify(m => m.BroadcastMessageAsync(forwardedMessage, It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on SearchResponseResolver Exception"), AutoData]
        public void Raises_DiagnosticGenerated_On_SearchResponseResolver_Exception(string username, int token, string query)
        {
            var mocks = new Mocks();
            var handler = new DistributedMessageHandler(
                mocks.Client.Object);

            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(searchResponseResolver: (a, b, c) => { throw new Exception(); }));

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);
            handler.HandleMessageRead(conn.Object, message);

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

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(new IPEndPoint(IPAddress.None, 0)));

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()), Times.Once);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()), Times.Once);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Deduplicates SearchRequest when deduplicate option is set"), AutoData]
        public void Deduplicates_SearchRequest_When_Deduplicate_Option_Is_Set(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, 1, 1, 1, 1, new List<File>() { new File(1, "1", 1, "1", 0) });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response), deduplicateSearchRequests: true);
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(new IPEndPoint(IPAddress.None, 0)));

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);
            handler.HandleMessageRead(conn.Object, message);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not deduplicate SearchRequest when deduplicate option is unset"), AutoData]
        public void Does_Not_Deduplicate_SearchRequest_When_Deduplicate_Option_Is_Unset(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, 1, 1, 1, 1, new List<File>() { new File(1, "1", 1, "1", 0) });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response), deduplicateSearchRequests: false);
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(new IPEndPoint(IPAddress.None, 0)));

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);
            handler.HandleMessageRead(conn.Object, message);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(response.ToByteArray())), null), Times.Exactly(2));
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Generates diagnostic on failure to send search results"), AutoData]
        public void Generates_Diagnostic_On_Failure_To_Send_Search_Results(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, 1, 1, 1, 1, new List<File>() { new File(1, "1", 1, "1", 0) });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(new IPEndPoint(IPAddress.None, 0)));

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            var ex = new Exception("foo");

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IMessageConnection>(ex));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);

            mocks.Diagnostic.Verify(m => m.Debug($"Failed to send search response for {query} to {username}: {ex.Message}", ex), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Responds to ServerSearchRequest"), AutoData]
        public void Responds_To_ServerSearchRequest(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, 1, 1, 1, 1, new List<File>() { new File(1, "1", 1, "1", 0) });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(new IPEndPoint(IPAddress.None, 0)));

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ServerSearchRequest)
                .WriteBytes(new byte[8])
                .WriteString(username)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            handler.HandleMessageRead(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()), Times.Once);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()), Times.Once);

            peerConn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest if result is null"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_If_Result_Is_Null(string username, int token, string query)
        {
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult<SearchResponse>(null));
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(new IPEndPoint(IPAddress.None, 0)));

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()), Times.Never);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()), Times.Never);

            peerConn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), null), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest if result contains no files"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_If_Result_Contains_No_Files(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, 0, 1, 1, 1, new List<File>());
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedSearchRequest(username, token, query).ToByteArray();

            handler.HandleMessageRead(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()), Times.Never);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()), Times.Never);

            peerConn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), null), Times.Never);
        }

        [Trait("Category", "HandleChildMessageRead")]
        [Theory(DisplayName = "HandleChildMessageRead responts to ping"), AutoData]
        public void HandleChildMessageRead_Responds_To_Ping(int token)
        {
            var (handler, mocks) = GetFixture();

            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(token);

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedPingRequest().ToByteArray();

            handler.HandleChildMessageRead(conn.Object, message);

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(new DistributedPingResponse(token).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "HandleChildMessageRead")]
        [Theory(DisplayName = "HandleChildMessageRead responts to ping from EventArgs"), AutoData]
        public void HandleChildMessageRead_Responds_To_Ping_From_EventArgs(int token)
        {
            var (handler, mocks) = GetFixture();

            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(token);

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedPingRequest().ToByteArray();

            handler.HandleChildMessageRead(conn.Object, new MessageReadEventArgs(message));

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(new DistributedPingResponse(token).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "HandleChildMessageRead")]
        [Theory(DisplayName = "HandleChildMessageRead produces warning on Exception"), AutoData]
        public void HandleChildMessageRead_Produces_Warning_On_Exception(int token)
        {
            var (handler, mocks) = GetFixture();

            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(token);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(new Exception()));

            var message = new DistributedPingRequest().ToByteArray();

            handler.HandleChildMessageRead(conn.Object, message);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("error handling distributed child message")), It.IsAny<Exception>()), Times.Once);
        }

        [Trait("Category", "HandleChildMessageRead")]
        [Fact(DisplayName = "HandleChildMessageRead produces debug on unhandled message")]
        public void HandleChildMessageRead_Produces_Debug_On_Unhandled_Message()
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            var message = new DistributedBranchLevel(1).ToByteArray();

            handler.HandleChildMessageRead(conn.Object, message);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("unhandled distributed child message"))), Times.Once);
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
