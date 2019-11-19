// <copyright file="GetUserStatusAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetUserStatusAsyncTests
    {
        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetUserStatusAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetUserStatusAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(async () => await s.GetUserStatusAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetPeerInfoAsync returns expected info"), AutoData]
        public async Task GetUserStatusAsync_Returns_Expected_Info(string username, UserStatus status, bool privileged)
        {
            var result = new UserStatusResponse(username, status, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatusResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var stat = await s.GetUserStatusAsync(username);

                Assert.Equal(result.Username, stat.Username);
                Assert.Equal(result.Status, stat.Status);
                Assert.Equal(result.IsPrivileged, stat.IsPrivileged);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws UserStatusException on throw"), AutoData]
        public async Task GetUserStatusAsync_Throws_UserStatusException_On_Throw(string username, UserStatus status, bool privileged)
        {
            var result = new UserStatusResponse(username, status, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatusResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                UserStatusResponse r = null;
                var ex = await Record.ExceptionAsync(async () => r = await s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserStatusException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetUserStatusAsync_Throws_TimeoutException_On_Timeout(string username, UserStatus status, bool privileged)
        {
            var result = new UserStatusResponse(username, status, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatusResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                UserStatusResponse r = null;
                var ex = await Record.ExceptionAsync(async () => r = await s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetUserStatusAsync_Throws_OperationCanceledException_On_Cancellation(string username, UserStatus status, bool privileged)
        {
            var result = new UserStatusResponse(username, status, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatusResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                UserStatusResponse r = null;
                var ex = await Record.ExceptionAsync(async () => r = await s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
