// <copyright file="SoulseekClientTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit
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

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ServerInfo is not null, but contains nulls initially")]
        public void ServerInfo_Is_Not_Null_But_Contains_Nulls_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.NotNull(s.ServerInfo);
                Assert.Null(s.ServerInfo.ParentMinSpeed);
                Assert.Null(s.ServerInfo.ParentSpeedRatio);
                Assert.Null(s.ServerInfo.WishlistInterval);
            }
        }

        [Trait("Category", "Port")]
        [Theory(DisplayName = "Port returns IPEndPoint port if not null"), AutoData]
        public void Port_Returns_IPEndPoint_Port_If_Not_Null(IPAddress ip, int port)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("IPEndPoint", new IPEndPoint(ip, port));

                Assert.Equal(port, s.Port);
            }
        }

        [Trait("Category", "IPAddress")]
        [Theory(DisplayName = "IPAddress returns IPEndPoint address if not null"), AutoData]
        public void IPAddress_Returns_IPEndPoint_Address_If_Not_Null(IPAddress ip, int port)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("IPEndPoint", new IPEndPoint(ip, port));

                Assert.Equal(ip, s.IPAddress);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect fails if connected")]
        public async Task Connect_Fails_If_Connected()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync());

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

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port));

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

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));

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

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

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
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync());

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
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
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync());

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
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync());

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect uses given CancellationToken"), AutoData]
        public async Task Connect_Uses_Given_CancellationToken(CancellationToken cancellationToken)
        {
            var c = new Mock<IMessageConnection>();

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                await s.ConnectAsync(cancellationToken);
            }

            c.Verify(m => m.ConnectAsync(cancellationToken), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect succeeds when TcpConnection succeeds")]
        public async Task Connect_Succeeds_When_TcpConnection_Succeeds()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync());

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
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect address uses given CancellationToken"), AutoData]
        public async Task Connect_Address_Uses_Given_CancellationToken(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            var c = new Mock<IMessageConnection>();

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object))
            {
                await s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, cancellationToken);
            }

            c.Verify(m => m.ConnectAsync(cancellationToken), Times.Once);
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
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            var key = new WaitKey(MessageCode.Server.Login);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(succeeded: true, string.Empty)));

            using (var s = new SoulseekClient(connectionFactory: factory.Object, waiter: waiter.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

                Assert.Null(ex);
            }

            waiter.Verify(m => m.Wait<LoginResponse>(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect address login uses given CancellationToken"), AutoData]
        public async Task Connect_Address_Login_Uses_Given_CancellationToken(IPEndPoint endpoint, string username, string password, CancellationToken cancellationToken)
        {
            var c = new Mock<IMessageConnection>();

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(m => m.GetServerConnection(
                It.IsAny<IPEndPoint>(),
                It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            var key = new WaitKey(MessageCode.Server.Login);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(key, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(succeeded: true, string.Empty)));

            using (var s = new SoulseekClient(connectionFactory: factory.Object, waiter: waiter.Object))
            {
                await s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password, cancellationToken);
            }

            c.Verify(m => m.ConnectAsync(cancellationToken), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect d StateChanged event")]
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
        [Theory(DisplayName = "Connect throws AddressException on bad address"), AutoData]
        public async Task Connect_Throws_ArgumentException_On_Bad_Address(string address)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, 1));

                Assert.NotNull(ex);
                Assert.IsType<AddressException>(ex);
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
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
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

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect login uses given CancellationToken"), AutoData]
        public async Task Connect_Login_Uses_Given_CancellationToken(string username, string password, CancellationToken cancellationToken)
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
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<ConnectionOptions>(),
                It.IsAny<ITcpClient>()))
                .Returns(c.Object);

            using (var s = new SoulseekClient(connectionFactory: factory.Object, waiter: w.Object))
            {
                await s.ConnectAsync(username, password, cancellationToken);
            }

            c.Verify(m => m.ConnectAsync(cancellationToken), Times.Once);
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
        [Fact(DisplayName = "Disconnect uses default message if none is given")]
        public void Disconnect_Uses_Default_Message_If_None_Is_Given()
        {
            string message = default;

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => message = e.Message;

                s.SetProperty("State", ConnectionState.Connected);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.Equal("Client disconnected", message);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnect uses given message"), AutoData]
        public void Disconnect_Uses_Given_Message(string msg)
        {
            string message = default;

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => message = e.Message;

                s.SetProperty("State", ConnectionState.Connected);

                var ex = Record.Exception(() => s.Disconnect(msg));

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.Equal(msg, message);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnect uses Exception message if no message is supplied"), AutoData]
        public void Disconnect_Uses_Exception_Message_If_No_Message_Is_Supplied(string msg)
        {
            string message = default;
            var exception = new Exception(msg);

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => message = e.Message;

                s.SetProperty("State", ConnectionState.Connected);

                var ex = Record.Exception(() => s.InvokeMethod("Disconnect", null, exception));

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.Equal(msg, message);
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
        [Theory(DisplayName = "ChangeState produces diagnostic"), AutoData]
        public void ChangeState_Produces_Diagnostic(string message, Exception exception)
        {
            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(diagnosticFactory: diagnostic.Object))
            {
                s.InvokeMethod("ChangeState", SoulseekClientStates.Disconnected, message, exception);
            }

            diagnostic.Verify(m => m.Debug(It.Is<string>(s => s == $"Client state changed from Disconnected to Disconnected; message: {message}")), Times.Once);
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState produces diagnostic and omits message if none is provided")]
        public void ChangeState_Produces_Diagnostic_And_Omits_Message_If_None_Is_Provided()
        {
            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(diagnosticFactory: diagnostic.Object))
            {
                s.InvokeMethod("ChangeState", SoulseekClientStates.Disconnected, null, null);
            }

            diagnostic.Verify(m => m.Debug(It.Is<string>(s => s == $"Client state changed from Disconnected to Disconnected")), Times.Once);
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
                string args = default;
                s.GlobalMessageReceived += (sender, e) => args = e;

                handlerMock.Raise(m => m.GlobalMessageReceived += null, this, msg);

                Assert.NotNull(args);
                Assert.Equal(msg, args);
            }
        }

        [Trait("Category", "GlobalMessageRecieved")]
        [Theory(DisplayName = "Does not throw when GlobalMessageRecieved and no handler bound"), AutoData]
        public void Does_Not_Throw_When_GlobalMessageReceived_And_No_Handler_Bound(string msg)
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                var ex = Record.Exception(() => handlerMock.Raise(m => m.GlobalMessageReceived += null, this, msg));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "MessageRead")]
        [Fact(DisplayName = "MessageRead invokes HandleMessageRead")]
        public void MessageRead_Invokes_HandleMessageRead()
        {
            var handlerMock = new Mock<IServerMessageHandler>();
            var args = new MessageEventArgs(new byte[4]);

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                s.InvokeMethod("ServerConnection_MessageRead", this, args);
            }

            handlerMock.Verify(m => m.HandleMessageRead(It.IsAny<object>(), args), Times.Once);
        }

        [Trait("Category", "MessageWritten")]
        [Fact(DisplayName = "MessageWritten invokes HandleMessageWritten")]
        public void MessageWritten_Invokes_HandleMessageWritten()
        {
            var handlerMock = new Mock<IServerMessageHandler>();
            var args = new MessageEventArgs(new byte[4]);

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                s.InvokeMethod("ServerConnection_MessageWritten", this, args);
            }

            handlerMock.Verify(m => m.HandleMessageWritten(It.IsAny<object>(), args), Times.Once);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when ListenerHandler raises")]
        public void Raises_DiagnosticGenerated_When_ListenerHandler_Raises()
        {
            var mock = new Mock<IListenerHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(listenerHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when ListenerHandler raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_ListenerHandler_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IListenerHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(listenerHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when PeerMessageHandler raises")]
        public void Raises_DiagnosticGenerated_When_PeerMessageHandler_Raises()
        {
            var mock = new Mock<IPeerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(peerMessageHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when PeerMessageHandler raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_PeerMessageHandler_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IPeerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(peerMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when DistributedMessageHandler raises")]
        public void Raises_DiagnosticGenerated_When_DistributedMessageHandler_Raises()
        {
            var mock = new Mock<IDistributedMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(distributedMessageHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when DistributedMessageHandler raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_DistributedMessageHandler_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IDistributedMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(distributedMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when PeerConnectionManager raises")]
        public void Raises_DiagnosticGenerated_When_PeerConnectionManager_Raises()
        {
            var mock = new Mock<IPeerConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(peerConnectionManager: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when PeerConnectionManager raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_PeerConnectionManager_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IPeerConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(peerConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when DistributedConnectionManager raises")]
        public void Raises_DiagnosticGenerated_When_DistributedConnectionManager_Raises()
        {
            var mock = new Mock<IDistributedConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when DistributedConnectionManager raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_DistributedConnectionManager_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IDistributedConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserStatusChanged fires when handler raises"), AutoData]
        public void UserStatusChanged_Fires_When_Handler_Raises(string username, UserPresence presense, bool privileged)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserStatusChangedEventArgs(username, presense, privileged);
            UserStatusChangedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.UserStatusChanged += (sender, args) => actualArgs = args;
                mock.Raise(m => m.UserStatusChanged += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserStatusChanged does not throw if event not bound"), AutoData]
        public void UserStatusChanged_Does_Not_Throw_If_Event_Not_Bound(string username, UserPresence presense, bool privileged)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserStatusChangedEventArgs(username, presense, privileged);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.UserStatusChanged += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateMessageReceived fires when handler raises"), AutoData]
        public void PrivateMessageReceived_Fires_When_Handler_Raises(int id, DateTime timestamp, string username, string message, bool isAdmin)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivateMessageReceivedEventArgs(id, timestamp, username, message, isAdmin);
            PrivateMessageReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateMessageReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateMessageReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateMessageReceived does not throw if event not bound"), AutoData]
        public void PrivateMessageReceived_Does_Not_Throw_If_Event_Not_Bound(int id, DateTime timestamp, string username, string message, bool isAdmin)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivateMessageReceivedEventArgs(id, timestamp, username, message, isAdmin);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateMessageReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegedUserListReceived fires when handler raises"), AutoData]
        public void PrivilegedUserListReceived_Fires_When_Handler_Raises(string[] usernames)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = usernames.ToList().AsReadOnly();
            IReadOnlyCollection<string> actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivilegedUserListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivilegedUserListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegedUserListReceived does not throw if event not bound"), AutoData]
        public void PrivilegedUserListReceived_Does_Not_Throw_If_Event_Not_Bound(string[] usernames)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = usernames.ToList().AsReadOnly();

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivilegedUserListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegeNotificationReceived fires when handler raises"), AutoData]
        public void PrivilegeNotificationReceived_Fires_When_Handler_Raises(string username, int id)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivilegeNotificationReceivedEventArgs(username, id);
            PrivilegeNotificationReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivilegeNotificationReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivilegeNotificationReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegeNotificationReceived does not throw if event not bound"), AutoData]
        public void PrivilegeNotificationReceived_Does_Not_Throw_If_Event_Not_Bound(string username, int id)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivilegeNotificationReceivedEventArgs(username, id);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivilegeNotificationReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserCannotConnect fires when handler raises"), AutoData]
        public void UserCannotConnect_Fires_When_Handler_Raises(int token, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserCannotConnectEventArgs(token, username);
            UserCannotConnectEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.UserCannotConnect += (sender, args) => actualArgs = args;
                mock.Raise(m => m.UserCannotConnect += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserCannotConnect does not throw if event not bound"), AutoData]
        public void UserCannotConnect_Does_Not_Throw_If_Event_Not_Bound(int token, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserCannotConnectEventArgs(token, username);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.UserCannotConnect += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomMessageReceived fires when handler raises"), AutoData]
        public void RoomMessageReceived_Fires_When_Handler_Raises(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomMessageReceivedEventArgs(roomName, username, message);
            RoomMessageReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomMessageReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomMessageReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomMessageReceived does not throw if event not bound"), AutoData]
        public void RoomMessageReceived_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomMessageReceivedEventArgs(roomName, username, message);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomMessageReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerAdded fires when handler raises"), AutoData]
        public void RoomTickerAdded_Fires_When_Handler_Raises(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerAddedEventArgs(roomName, new RoomTicker(username, message));
            RoomTickerAddedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomTickerAdded += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomTickerAdded += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerAdded does not throw if event not bound"), AutoData]
        public void RoomTickerAdded_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerAddedEventArgs(roomName, new RoomTicker(username, message));

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomTickerAdded += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerRemoved fires when handler raises"), AutoData]
        public void RoomTickerRemoved_Fires_When_Handler_Raises(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerRemovedEventArgs(roomName, username);
            RoomTickerRemovedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomTickerRemoved += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomTickerRemoved += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerRemoved does not throw if event not bound"), AutoData]
        public void RoomTickerRemoved_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerRemovedEventArgs(roomName, username);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomTickerRemoved += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerListReceived fires when handler raises"), AutoData]
        public void RoomTickerListReceived_Fires_When_Handler_Raises(string roomName, List<RoomTicker> tickers)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerListReceivedEventArgs(roomName, tickers);
            RoomTickerListReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomTickerListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomTickerListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerListReceived does not throw if event not bound"), AutoData]
        public void RoomTickerListReceived_Does_Not_Throw_If_Event_Not_Bound(string roomName, List<RoomTicker> tickers)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerListReceivedEventArgs(roomName, tickers);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomTickerListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PublicChatMessageReceived fires when handler raises"), AutoData]
        public void PublicChatMessageReceived_Fires_When_Handler_Raises(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PublicChatMessageReceivedEventArgs(roomName, username, message);
            PublicChatMessageReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PublicChatMessageReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PublicChatMessageReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PublicChatMessageReceived does not throw if event not bound"), AutoData]
        public void PublicChatMessageReceived_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PublicChatMessageReceivedEventArgs(roomName, username, message);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PublicChatMessageReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomJoined fires when handler raises"), AutoData]
        public void RoomJoined_Fires_When_Handler_Raises(string roomName, string username, UserData userData)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomJoinedEventArgs(roomName, username, userData);
            RoomJoinedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomJoined += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomJoined += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomJoined does not throw if event not bound"), AutoData]
        public void RoomJoined_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, UserData userData)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomJoinedEventArgs(roomName, username, userData);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomJoined += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomLeft fires when handler raises"), AutoData]
        public void RoomLeft_Fires_When_Handler_Raises(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomLeftEventArgs(roomName, username);
            RoomLeftEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomLeft += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomLeft += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomLeft does not throw if event not bound"), AutoData]
        public void RoomLeft_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomLeftEventArgs(roomName, username);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomLeft += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomListReceived fires when handler raises"), AutoData]
        public void RoomListReceived_Fires_When_Handler_Raises(RoomList rooms)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = rooms;
            RoomList actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomListReceived does not throw if event not bound"), AutoData]
        public void RoomListReceived_Does_Not_Throw_If_Event_Not_Bound(RoomList rooms)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = rooms;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipAdded fires when handler raises"), AutoData]
        public void PrivateRoomMembershipAdded_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomMembershipAdded += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomMembershipAdded += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipAdded does not throw if event not bound"), AutoData]
        public void PrivateRoomMembershipAdded_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomMembershipAdded += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipRemoved fires when handler raises"), AutoData]
        public void PrivateRoomMembershipRemoved_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomMembershipRemoved += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomMembershipRemoved += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipRemoved does not throw if event not bound"), AutoData]
        public void PrivateRoomMembershipRemoved_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomMembershipRemoved += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationAdded fires when handler raises"), AutoData]
        public void PrivateRoomModerationAdded_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomModerationAdded += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomModerationAdded += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationAdded does not throw if event not bound"), AutoData]
        public void PrivateRoomModerationAdded_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomModerationAdded += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationRemoved fires when handler raises"), AutoData]
        public void PrivateRoomModerationRemoved_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomModerationRemoved += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomModerationRemoved += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationRemoved does not throw if event not bound"), AutoData]
        public void PrivateRoomModerationRemoved_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomModerationRemoved += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomUserListReceived fires when handler raises"), AutoData]
        public void PrivateRoomUserListReceived_Fires_When_Handler_Raises(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;
            RoomInfo actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomUserListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomUserListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomUserListReceived does not throw if event not bound"), AutoData]
        public void PrivateRoomUserListReceived_Does_Not_Throw_If_Event_Not_Bound(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomUserListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModeratedUserListReceived fires when handler raises"), AutoData]
        public void PrivateRoomModeratedUserListReceived_Fires_When_Handler_Raises(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;
            RoomInfo actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomModeratedUserListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomModeratedUserListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModeratedUserListReceived does not throw if event not bound"), AutoData]
        public void PrivateRoomModeratedUserListReceived_Does_Not_Throw_If_Event_Not_Bound(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomModeratedUserListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DiagnosticGenerated fires when handler raises"), AutoData]
        public void DiagnosticGenerated_Fires_When_Handler_Raises(DiagnosticLevel level, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(level, message);
            DiagnosticEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) => actualArgs = args;
                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "ServerMessageHandler DiagnosticGenerated does not throw if event not bound"), AutoData]
        public void ServerMessageHandler_DiagnosticGenerated_Does_Not_Throw_If_Event_Not_Bound(DiagnosticLevel level, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(level, message);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Diagnostic does not throw if event not bound"), AutoData]
        public void Diagnostic_Does_Not_Throw_If_Event_Not_Bound(string message)
        {
            using (var s = new SoulseekClient())
            {
                DiagnosticFactory d = s.GetProperty<DiagnosticFactory>("Diagnostic");

                var ex = Record.Exception(() => d.Info(message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Diagnostic raises DiagnosticGenerated"), AutoData]
        public void Diagnostic_Raises_DiagnosticGenerated(string message)
        {
            string actualMessage = null;

            using (var s = new SoulseekClient())
            {
                s.DiagnosticGenerated += (sender, m) => actualMessage = m.Message;
                DiagnosticFactory d = s.GetProperty<DiagnosticFactory>("Diagnostic");

                d.Info(message);

                Assert.Equal(message, actualMessage);
            }
        }
    }
}
