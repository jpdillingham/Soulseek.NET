// <copyright file="ConnectAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ConnectAsyncTests
    {
        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws InvalidOperationException if connected"), AutoData]
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
        [Theory(DisplayName = "Connect throws InvalidOperationException if connected"), AutoData]
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
        [Theory(DisplayName = "Connect throws InvalidOperationException if connecting"), AutoData]
        public async Task Connect_Credentials_Fails_If_Connecting(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connecting);

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws InvalidOperationException if connecting"), AutoData]
        public async Task Connect_Address_Credentials_Fails_If_Connecting(IPEndPoint endpoint, string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connecting);

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws InvalidOperationException if logging in"), AutoData]
        public async Task Connect_Credentials_Fails_If_Logging_In(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.LoggingIn);

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws InvalidOperationException if logging in"), AutoData]
        public async Task Connect_Address_Credentials_Fails_If_Logging_In(IPEndPoint endpoint, string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.LoggingIn);

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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect sets state to Disconnected on failure")]
        public async Task Connect_Sets_State_To_Disconnected_On_Failure()
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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);

                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect sets state to Connected | LoggedIn on success")]
        public async Task Connect_Sets_State_To_Connected_LoggedIn_On_Success()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

                Assert.Null(ex);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, s.State);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect raises correct StateChanged sequence on success")]
        public async Task Connect_Raises_Correct_StateChanged_Sequence_On_Success()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                var events = new List<SoulseekClientStateChangedEventArgs>();

                s.StateChanged += (e, args) => events.Add(args);

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, s.State);

                Assert.Equal(SoulseekClientStates.Connecting, events[0].State);
                Assert.Equal(SoulseekClientStates.Connected, events[1].State);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggingIn, events[2].State);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, events[3].State);
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
        [Fact(DisplayName = "Connect raises StateChanged event")]
        public async Task Connect_Raises_StateChanged_Event()
        {
            var fired = false;

            using (var s = new SoulseekClient())
            {
                s.StateChanged += (sender, e) => fired = true;

                var task = s.ConnectAsync("u", "p");

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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, 1, "u", "p"));

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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, 1, "u", "p"));

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
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("127.0.0.01", port, "u", "p"));

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

        [Trait("Category", "ConnectInternal")]
        [Theory(DisplayName = "Exits gracefully if already connected and logged in"), AutoData]
        public async Task Exits_Gracefully_If_Already_Connected_And_Logged_In(IPEndPoint endpoint, string username, string password)
        {
            var fired = false;

            using (var s = new SoulseekClient())
            {
                s.StateChanged += (sender, e) => fired = true;
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("ConnectInternalAsync", endpoint.Address.ToString(), endpoint, username, password, null);

                await task;

                Assert.False(fired);
            }
        }
    }
}
