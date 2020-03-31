// <copyright file="GetDirectoryContentsAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetDirectoryContentsAsyncTests
    {
        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetDirectoryContentsAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.GetDirectoryContentsAsync(username, "foo"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetDirectoryContentsAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(async () => await s.GetDirectoryContentsAsync("a", "b"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_TimeoutException_On_Timeout(string username, string directory)
        {
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_OperationCanceledException_On_Cancellation(string username, string directory)
        {
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws UserOfflineException on user offline"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_UserOfflineException_On_User_Offline(string username, string directory)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(new UserOfflineException()));

            var serverConn = new Mock<IMessageConnection>();
            var connManager = new Mock<IPeerConnectionManager>();

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                Directory dir = null;
                var ex = await Record.ExceptionAsync(async () => dir = await s.GetDirectoryContentsAsync(username, directory));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws DirectoryContentsException on throw"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_DirectoryContentsException_On_Throw(string username, string directory)
        {
        }
    }
}
