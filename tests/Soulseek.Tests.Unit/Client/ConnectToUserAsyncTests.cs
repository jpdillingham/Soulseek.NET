// <copyright file="ConnectToUserAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Network;
    using Xunit;

    public class ConnectToUserAsyncTests
    {
        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "ConnectToUserAsync Throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task Throws_ArgumentException_On_Bad_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectToUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "ConnectToUserAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task ConnectToUserAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.ConnectToUserAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Throws UserOfflineException when user is offline")]
        public async Task Throws_UserOfflineException_When_User_Is_Offline()
        {
            var (client, mocks) = GetFixture();

            mocks.Client.Setup(m => m.GetUserEndPointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new UserOfflineException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Throws TimeoutException when connection times out")]
        public async Task Throws_TimeoutException_When_Connection_Times_Out()
        {
            var (client, mocks) = GetFixture();

            mocks.Client.Setup(m => m.GetUserEndPointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Throws OperationCanceledException when canceled")]
        public async Task Throws_OperationCanceledException_When_Canceled()
        {
            var (client, mocks) = GetFixture();

            mocks.Client.Setup(m => m.GetUserEndPointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Uses given CancellationToken"), AutoData]
        public async Task Uses_Given_CancellationToken(string user)
        {
            var cancellationToken = new CancellationToken();

            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectToUserAsync(user, cancellationToken: cancellationToken);
            }

            mocks.Client.Verify(m => m.GetUserEndPointAsync(user, cancellationToken), Times.AtLeastOnce);
        }

        private (SoulseekClient client, Mocks Mocks) GetFixture()
        {
            var mocks = new Mocks();
            var client = mocks.Client.Object;

            return (client, mocks);
        }

        private class Mocks
        {
            public Mocks()
            {
                Client = new Mock<SoulseekClient>()
                {
                    CallBase = true,
                };

                Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                Client.Setup(m => m.GetUserEndPointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555)));
                Client.Setup(m => m.PeerConnectionManager).Returns(PeerConnectionManager.Object);

                PeerConnectionManager.Setup(m => m.TryInvalidateMessageConnectionCache(It.IsAny<string>()))
                    .Returns(false);
                PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<string>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new Mock<IMessageConnection>().Object));
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
        }
    }
}
