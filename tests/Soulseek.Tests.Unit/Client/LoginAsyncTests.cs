// <copyright file="LoginAsyncTests.cs" company="JP Dillingham">
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
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class LoginAsyncTests
    {
        private static readonly Random RNG = new Random();

        private static int GetPort()
        {
            return 50000 + RNG.Next(1, 9999);
        }

        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "LoginAsync throws ArgumentException on null username")]
        public async Task LoginAsync_Throws_ArgumentException_On_Null_Username()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync(null, Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync throws ArgumentException on bad input")]
        [InlineData(null, "a")]
        [InlineData("", "a")]
        [InlineData("a", null)]
        [InlineData("a", "")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public async Task LoginAsync_Throws_ArgumentException_On_Bad_Input(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "LoginAsync throws InvalidOperationException if logged in")]
        public async Task LoginAsync_Throws_InvalidOperationException_If_Logged_In()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync("a", "b"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "LoginAsync throws InvalidOperationException if not connected")]
        public async Task LoginAsync_Throws_InvalidOperationException_If_Not_Connected()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Disconnected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync("a", "b"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync changes state to Connected and LoggedIn on success"), AutoData]
        public async Task LoginAsync_Changes_State_To_Connected_And_LoggedIn_On_Success(string user, string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                await s.LoginAsync(user, password);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, s.State);
                Assert.Equal(user, s.Username);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync sets listen port on success if set"), AutoData]
        public async Task LoginAsync_Sets_Listen_Port_On_Success_If_Set(string user, string password)
        {
            var port = GetPort();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object, options: new SoulseekClientOptions(listen: true, listenPort: port)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                await s.LoginAsync(user, password);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, s.State);
                Assert.Equal(user, s.Username);
            }

            var expectedBytes = new SetListenPortCommand(port).ToByteArray();
            conn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(expectedBytes)), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync uses given CancellationToken"), AutoData]
        public async Task LoginAsync_Uses_Given_CancellationToken(string user, string password)
        {
            var cancellationToken = new CancellationToken();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                await s.LoginAsync(user, password, cancellationToken);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, s.State);
                Assert.Equal(user, s.Username);
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.AtLeastOnce);
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync writes HaveNoParent on success if enabled"), AutoData]
        public async Task LoginAsync_Writes_HaveNoParent_On_Success_If_Enabled(string user, string password)
        {
            var port = GetPort();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object, options: new SoulseekClientOptions(listenPort: port)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                await s.LoginAsync(user, password);
            }

            var expectedBytes = new HaveNoParentsCommand(true).ToByteArray();
            conn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(expectedBytes)), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync does not write HaveNoParent on success if disabled"), AutoData]
        public async Task LoginAsync_Does_Not_Write_HaveNoParent_On_Success_If_Disabled(string user, string password)
        {
            var port = GetPort();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object, options: new SoulseekClientOptions(enableDistributedNetwork: false, listenPort: port)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                await s.LoginAsync(user, password);
            }

            var expectedBytes = new HaveNoParentsCommand(true).ToByteArray();
            conn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(expectedBytes)), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync raises ServerInfoReceived on login"), AutoData]
        public async Task LoginAsync_Raises_ServerInfoReceived_On_Login(string user, string password, int parentMinSpeed, int parentSpeedRatio, int wishlistInterval)
        {
            var port = GetPort();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));
            waiter.Setup(m => m.Wait<int>(It.Is<WaitKey>(w => w == new WaitKey(MessageCode.Server.ParentMinSpeed)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(parentMinSpeed));
            waiter.Setup(m => m.Wait<int>(It.Is<WaitKey>(w => w == new WaitKey(MessageCode.Server.ParentSpeedRatio)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(parentSpeedRatio));
            waiter.Setup(m => m.Wait<int>(It.Is<WaitKey>(w => w == new WaitKey(MessageCode.Server.WishlistInterval)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(wishlistInterval));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object, options: new SoulseekClientOptions(listenPort: port)))
            {
                ServerInfo args = null;

                s.SetProperty("State", SoulseekClientStates.Connected);
                s.ServerInfoReceived += (sender, e) => args = e;

                await s.LoginAsync(user, password);

                Assert.NotNull(args);
                Assert.Equal(parentMinSpeed, args.ParentMinSpeed);
                Assert.Equal(parentSpeedRatio, args.ParentSpeedRatio);
                Assert.Equal(wishlistInterval * 1000, args.WishlistInterval);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync sets ServerInfo on login"), AutoData]
        public async Task LoginAsync_Sets_ServerInfo_On_Login(string user, string password, int parentMinSpeed, int parentSpeedRatio, int wishlistInterval)
        {
            var port = GetPort();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));
            waiter.Setup(m => m.Wait<int>(It.Is<WaitKey>(w => w == new WaitKey(MessageCode.Server.ParentMinSpeed)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(parentMinSpeed));
            waiter.Setup(m => m.Wait<int>(It.Is<WaitKey>(w => w == new WaitKey(MessageCode.Server.ParentSpeedRatio)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(parentSpeedRatio));
            waiter.Setup(m => m.Wait<int>(It.Is<WaitKey>(w => w == new WaitKey(MessageCode.Server.WishlistInterval)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(wishlistInterval));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object, options: new SoulseekClientOptions(listenPort: port)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                await s.LoginAsync(user, password);

                Assert.Equal(parentMinSpeed, s.ServerInfo.ParentMinSpeed);
                Assert.Equal(parentSpeedRatio, s.ServerInfo.ParentSpeedRatio);
                Assert.Equal(wishlistInterval * 1000, s.ServerInfo.WishlistInterval);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync throws SoulseekClientException if expected login messages are not sent"), AutoData]
        public async Task LoginAsync_Throws_SoulseekClientException_If_Expected_Login_Messages_Are_Not_Sent(string user, string password)
        {
            var port = GetPort();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));
            waiter.Setup(m => m.Wait<int>(It.Is<WaitKey>(w => w == new WaitKey(MessageCode.Server.WishlistInterval)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<int>(new TimeoutException("timed out")));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object, options: new SoulseekClientOptions(listenPort: port)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync(user, password));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.True(ex.Message.ContainsInsensitive("did not receive one or more expected server messages"));
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.IsType<TimeoutException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync disconnects and throws LoginRejectedException on failure"), AutoData]
        public async Task LoginAsync_Disconnects_And_Throws_LoginException_On_Failure(string user, string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(false, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync(user, password));

                Assert.NotNull(ex);
                Assert.IsType<LoginRejectedException>(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
                Assert.Null(s.Username);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync throws TimeoutException on timeout"), AutoData]
        public async Task LoginAsync_Throws_TimeoutException_On_Timeout(string user, string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<LoginResponse>(new TimeoutException()));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync(user, password));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task LoginAsync_Throws_OperationCanceledException_On_Cancellation(string user, string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<LoginResponse>(new OperationCanceledException()));

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync(user, password));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync throws SoulseekClientException on message write exception"), AutoData]
        public async Task LoginAsync_Throws_SoulseekClientException_On_Message_Write_Exception(string user, string password)
        {
            var waiter = new Mock<IWaiter>();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Exception>(new ConnectionWriteException()));

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LoginAsync(user, password));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }
    }
}
