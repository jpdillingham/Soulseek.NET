// <copyright file="LoginAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class LoginAsyncTests
    {
        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "LoginAsync throws ArgumentException on null username")]
        public async Task LoginAsync_Throws_ArgumentException_On_Null_Username()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(null, Guid.NewGuid().ToString()));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
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
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(username, password));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "LoginAsync throws InvalidOperationException if logged in")]
        public async Task LoginAsync_Throws_InvalidOperationException_If_Logged_In()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync("a", "b"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "LoginAsync throws InvalidOperationException if not connected")]
        public async Task LoginAsync_Throws_InvalidOperationException_If_Not_Connected()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Disconnected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync("a", "b"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync changes state to Connected and LoggedIn on success"), AutoData]
        public async Task LoginAsync_Changes_State_To_Connected_And_LoggedIn_On_Success(string user, string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object, waiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected);

            await s.LoginAsync(user, password);

            Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, s.State);
            Assert.Equal(user, s.Username);
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync disconnects and throws LoginException on failure"), AutoData]
        public async Task LoginAsync_Disconnects_And_Throws_LoginException_On_Failure(string user, string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(false, string.Empty)));

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object, waiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(user, password));

            Assert.NotNull(ex);
            Assert.IsType<LoginException>(ex);
            Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            Assert.Null(s.Username);
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync throws LoginException on wait timeout"), AutoData]
        public async Task LoginAsync_Throws_LoginException_On_Wait_Timeout(string user, string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<LoginResponse>(new TimeoutException()));

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object, waiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(user, password));

            Assert.NotNull(ex);
            Assert.IsType<LoginException>(ex);
            Assert.IsType<TimeoutException>(ex.InnerException);
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "LoginAsync throws LoginException on message write exception"), AutoData]
        public async Task LoginAsync_Throws_LoginException_On_Message_Write_Exception(string user, string password)
        {
            var waiter = new Mock<IWaiter>();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Exception>(new ConnectionWriteException()));

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object, waiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(user, password));

            Assert.NotNull(ex);
            Assert.IsType<LoginException>(ex);
            Assert.IsType<ConnectionWriteException>(ex.InnerException);
        }
    }
}
