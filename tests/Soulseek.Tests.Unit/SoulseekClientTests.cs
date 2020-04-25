// <copyright file="SoulseekClientTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class SoulseekClientTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with with given options")]
        public void Instantiates_With_Given_Options()
        {
            var options = new SoulseekClientOptions();

            using (var s = new SoulseekClient(options))
            {
                Assert.Equal(options, s.Options);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates without exception")]
        public void Instantiates_Without_Exception()
        {
            SoulseekClient s = null;

            var ex = Record.Exception(() => s = new SoulseekClient());

            Assert.Null(ex);
            Assert.NotNull(s);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "State is Disconnected initially")]
        public void State_Is_Disconnected_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Username is null initially")]
        public void Username_Is_Null_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.Null(s.Username);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "IPEndPoint is null initially")]
        public void IPEndPoint_Is_Null_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.Null(s.IPEndPoint);
                Assert.Null(s.IPAddress);
                Assert.Null(s.Port);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect fails if connected")]
        public async Task Connect_Fails_If_Connected()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect fails if connected"), AutoData]
        public async Task Connect_Address_Fails_If_Connected(IPEndPoint endpoint)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect fails if connected"), AutoData]
        public async Task Connect_Credentials_Fails_If_Connected(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect fails if connected"), AutoData]
        public async Task Connect_Address_Credentials_Fails_If_Connected(IPEndPoint endpoint, string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws when TcpConnection throws")]
        public async Task Connect_Throws_When_TcpConnection_Throws()
        {
            var c = new Mock<IMessageConnection>();
            c.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>())).Throws(new ConnectionException());

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageReadEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws TimeoutException when connection times out")]
        public async Task Connect_Throws_TimeoutException_When_Connection_Times_Out()
        {
            var c = new Mock<IMessageConnection>();
            c.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>())).Throws(new TimeoutException());

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageReadEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws OperationCanceledException when canceled")]
        public async Task Connect_Throws_OperationCanceledException_When_Canceled()
        {
            var c = new Mock<IMessageConnection>();
            c.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageReadEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect succeeds when TcpConnection succeeds")]
        public async Task Connect_Succeeds_When_TcpConnection_Succeeds()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect address succeeds when TcpConnection succeeds"), AutoData]
        public async Task Connect_Address_Succeeds_When_TcpConnection_Succeeds(IPEndPoint endpoint)
        {
            var c = new Mock<IMessageConnection>();

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageReadEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect address credentials succeeds when TcpConnection succeeds"), AutoData]
        public async Task Connect_Address_Credentials_Succeeds_When_TcpConnection_Succeeds(IPEndPoint endpoint, string username, string password)
        {
            var c = new Mock<IMessageConnection>();

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageReadEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            var key = new WaitKey(MessageCode.Server.Login);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(succeeded: true, string.Empty)));

            using (var s = new SoulseekClient(connectionFactory: factory.Object, waiter: waiter.Object))
            {
                var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

                Assert.Null(ex);
            }

            waiter.Verify(m => m.Wait<LoginResponse>(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect raises StateChanged event")]
        public async Task Connect_Raises_StateChanged_Event()
        {
            var fired = false;

            using (var s = new SoulseekClient())
            {
                s.StateChanged += (sender, e) => fired = true;

                var task = s.ConnectAsync();

                var c = s.GetProperty<Connection>("ServerConnection");
                c.RaiseEvent(typeof(Connection), "Connected", EventArgs.Empty);

                await task;

                Assert.True(fired);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws ArgumentException on bad input")]
        [InlineData(null, "a")]
        [InlineData("", "a")]
        [InlineData("a", null)]
        [InlineData("a", "")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public async Task Connect_Throws_ArgumentException_On_Bad_Input(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws ArgumentException on bad input"), AutoData]
        public async Task Connect_Throws_ArgumentException_On_Bad_Address(string address)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, 1));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.IsType<AddressException>(ex.InnerException);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws ArgumentException on bad input")]
        [InlineData("127.0.0.1", 1, null, "a")]
        [InlineData("127.0.0.1", 1, "", "a")]
        [InlineData("127.0.0.1", 1, "a", null)]
        [InlineData("127.0.0.1", 1, "a", "")]
        [InlineData("127.0.0.1", 1, "", "")]
        [InlineData("127.0.0.1", 1, null, null)]
        [InlineData(null, 1, "user", "pass")]
        [InlineData("", 1, "user", "pass")]
        [InlineData(" ", 1, "user", "pass")]
        public async Task Connect_Address_Credentials_Throws_ArgumentException_On_Bad_Input(string address, int port, string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws ArgumentException on bad input")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Connect_Address_Throws_ArgumentException_On_Bad_Input(string address)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, 1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws ArgumentOutOfRangeException on bad port")]
        [InlineData(-1)]
        [InlineData(65536)]
        public async Task Connect_Address_Throws_ArgumentException_On_Bad_Port(int port)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("127.0.0.01", port));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws ArgumentOutOfRangeException on bad port")]
        [InlineData(-1)]
        [InlineData(65536)]
        public async Task Connect_Address_Credentials_Throws_ArgumentException_On_Bad_Port(int port)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("127.0.0.01", port, "user", "pass"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws InvalidOperationException_When_Already_Connected"), AutoData]
        public async Task Connect_Throws_InvalidOperationException_When_Already_Connected(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect connects and logs in"), AutoData]
        public async Task Connect_Connects_And_Logs_In(string username, string password)
        {
            var c = new Mock<IMessageConnection>();

            var w = new Mock<IWaiter>();
            w.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageReadEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object, waiter: w.Object))
            {
                await s.ConnectAsync(username, password);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, s.State);
            }

            c.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect handler disconnects")]
        public async Task Disconnect_Handler_Disconnects()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                await s.ConnectAsync();

                s.InvokeMethod("ServerConnection_Disconnected", null, new ConnectionDisconnectedEventArgs(string.Empty));

                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect disconnects")]
        public async Task Disconnect_Disconnects()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                await s.ConnectAsync();

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect raises StateChanged event")]
        public void Disconnect_Raises_StateChanged_Event()
        {
            var fired = false;

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => fired = true;

                s.SetProperty("State", ConnectionState.Connected);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.True(fired);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect clears searches")]
        public void Disconnect_Clears_Searches()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                using (var search1 = new SearchInternal(string.Empty, 0, new SearchOptions()))
                using (var search2 = new SearchInternal(string.Empty, 1, new SearchOptions()))
                {
                    var searches = new ConcurrentDictionary<int, SearchInternal>();
                    searches.TryAdd(0, search1);
                    searches.TryAdd(1, search2);

                    s.SetProperty("Searches", searches);

                    var ex = Record.Exception(() => s.Disconnect());

                    Assert.Null(ex);
                    Assert.Equal(SoulseekClientStates.Disconnected, s.State);
                    Assert.Empty(searches);
                }
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect does not clear downloads")]
        public void Disconnect_Clears_Downloads()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(0, new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0));
                downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 1));

                s.SetProperty("Downloads", downloads);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
                Assert.NotEmpty(downloads);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect does not clear peer queue")]
        public void Disconnect_Clears_Peer_Queue()
        {
            var c = new Mock<IMessageConnection>();

            var p = new Mock<IPeerConnectionManager>();

            using (var s = new SoulseekClient(serverConnection: c.Object, peerConnectionManager: p.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                p.Verify(m => m.RemoveAndDisposeAll(), Times.Never);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect cancels searches")]
        public async Task Disconnect_Cancels_Searches()
        {
            var c = new Mock<IMessageConnection>();

            var p = new Mock<IPeerConnectionManager>();

            using (var search = new SearchInternal("foo", 1))
            {
                var searches = new ConcurrentDictionary<int, SearchInternal>();
                searches.TryAdd(1, search);

                using (var s = new SoulseekClient(serverConnection: c.Object, peerConnectionManager: p.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected);
                    s.SetProperty("Searches", searches);

                    var searchWait = search.WaitForCompletion(CancellationToken.None);

                    s.Disconnect();

                    var ex = await Record.ExceptionAsync(() => searchWait);

                    Assert.NotNull(ex);
                    Assert.IsType<OperationCanceledException>(ex);
                }
            }
        }

        [Trait("Category", "Dispose/Finalize")]
        [Fact(DisplayName = "Disposes without exception")]
        public void Disposes_Without_Exception()
        {
            using (var s = new SoulseekClient())
            {
                var ex = Record.Exception(() => s.Dispose());

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Dispose/Finalize")]
        [Fact(DisplayName = "Finalizes without exception")]
        public void Finalizes_Without_Exception()
        {
            using (var s = new SoulseekClient())
            {
                var ex = Record.Exception(() => s.InvokeMethod("Finalize"));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState does not throw if StateChange is unsubscribed")]
        public void ChangeState_Does_Not_Throw_If_StateChange_Is_Unsubscribed()
        {
            using (var s = new SoulseekClient())
            {
                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Connected, string.Empty, null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ChangeState")]
        [Theory(DisplayName = "ChangeState fires Disconnected event when transitioning to Disconnected"), AutoData]
        public void ChangeState_Fires_Disconnected_Event_When_Transitioning_To_Disconnected(string message, Exception exception)
        {
            using (var s = new SoulseekClient())
            {
                SoulseekClientDisconnectedEventArgs args = null;
                s.Disconnected += (sender, e) => args = e;

                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Disconnected, message, exception));

                Assert.Null(ex);
                Assert.NotNull(args);
                Assert.Equal(message, args.Message);
                Assert.Equal(exception, args.Exception);
            }
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState fires Connected event when transitioning to Connected")]
        public void ChangeState_Fires_Connected_Event_When_Transitioning_To_Connected()
        {
            using (var s = new SoulseekClient())
            {
                bool fired = false;
                s.Connected += (sender, e) => fired = true;

                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Connected, string.Empty, null));

                Assert.Null(ex);
                Assert.True(fired);
            }
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState fires LoggedIn event when transitioning to LoggedIn")]
        public void ChangeState_Fires_LoggedIn_Event_When_Transitioning_To_LoggedIn()
        {
            using (var s = new SoulseekClient())
            {
                bool fired = false;
                s.LoggedIn += (sender, e) => fired = true;

                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, string.Empty, null));

                Assert.Null(ex);
                Assert.True(fired);
            }
        }

        [Trait("Category", "GetNextToken")]
        [Theory(DisplayName = "GetNextToken invokes TokenFactory"), AutoData]
        public void GetNextToken_Invokes_TokenFactory(int token)
        {
            var f = new Mock<ITokenFactory>();
            f.Setup(m => m.NextToken())
                .Returns(token);

            using (var s = new SoulseekClient(tokenFactory: f.Object))
            {
                var t = s.GetNextToken();

                Assert.Equal(token, t);

                f.Verify(m => m.NextToken(), Times.Once);
            }
        }

        [Trait("Category", "KickedFromServer")]
        [Fact(DisplayName = "Raises KickedFromServer when kicked from server")]
        public void Raises_KickedFromServer_When_Kicked_From_Server()
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                bool fired = false;
                s.KickedFromServer += (sender, args) => fired = true;

                handlerMock.Raise(m => m.KickedFromServer += null, EventArgs.Empty);

                Assert.True(fired);
            }
        }

        [Trait("Category", "KickedFromServer")]
        [Fact(DisplayName = "Disconnects when kicked from server")]
        public void Disconnects_When_Kicked_From_Server()
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                SoulseekClientDisconnectedEventArgs e = null;
                s.Disconnected += (sender, args) => e = args;

                handlerMock.Raise(m => m.KickedFromServer += null, EventArgs.Empty);

                Assert.True(e.Exception is KickedFromServerException);
            }
        }

        [Trait("Category", "GlobalMessageRecieved")]
        [Theory(DisplayName = "Raises GlobalMessageRecieved on receipt"), AutoData]
        public void Raises_GlobalMessageReceived_On_Receipt(string msg)
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                GlobalMessageReceivedEventArgs args = default;
                s.GlobalMessageReceived += (sender, e) => args = e;

                handlerMock.Raise(m => m.GlobalMessageReceived += null, new GlobalMessageReceivedEventArgs(msg));

                Assert.NotNull(args);
                Assert.Equal(msg, args.Message);
            }
        }

        [Trait("Category", "MessageRead")]
        [Fact(DisplayName = "MessageRead invokes HandleMessageRead")]
        public void MessageRead_Invokes_HandleMessageRead()
        {
            var handlerMock = new Mock<IServerMessageHandler>();
            var args = new MessageReadEventArgs(Array.Empty<byte>());

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                s.InvokeMethod("ServerConnection_MessageRead", this, args);
            }

            handlerMock.Verify(m => m.HandleMessageRead(It.IsAny<object>(), args), Times.Once);
        }
    }
}
