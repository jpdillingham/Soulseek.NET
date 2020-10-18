// <copyright file="SetUserStatusAsyncTests.cs" company="JP Dillingham">
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
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class SetUserStatusAsyncTests
    {
        [Trait("Category", "SetUserStatusAsync")]
        [Fact(DisplayName = "SetUserStatusAsync throws InvalidOperationException when not connected")]
        public async Task SetUserStatusAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.SetStatusAsync(UserPresence.Online));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "SetUserStatusAsync")]
        [Fact(DisplayName = "SetUserStatusAsync throws InvalidOperationException when not logged in")]
        public async Task SetUserStatusAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.SetStatusAsync(UserPresence.Online));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "SetUserStatusAsync")]
        [Fact(DisplayName = "SetUserStatusAsync does not throw when write does not throw")]
        public async Task SetUserStatusAsync_Does_Not_Throw_When_Write_Does_Not_Throw()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetStatusAsync(UserPresence.Online));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "SetUserStatusAsync")]
        [Fact(DisplayName = "SetUserStatusAsync throws UserStatusException when write throws")]
        public async Task SetUserStatusAsync_Throws_Exception_When_Write_Throws()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetStatusAsync(UserPresence.Online, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<UserStatusException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "SetUserStatusAsync")]
        [Fact(DisplayName = "SetUserStatusAsync throws TimeoutException when write times out")]
        public async Task SetUserStatusAsync_Throws_TimeoutException_When_Write_Times_Out()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetStatusAsync(UserPresence.Online, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "SetUserStatusAsync")]
        [Fact(DisplayName = "SetUserStatusAsync throws OperationCanceledException when write is canceled")]
        public async Task SetUserStatusAsync_Throws_OperationCanceledException_When_Write_Is_Canceled()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetStatusAsync(UserPresence.Online, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
