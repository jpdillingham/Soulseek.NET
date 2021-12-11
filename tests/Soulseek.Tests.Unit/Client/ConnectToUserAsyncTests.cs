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
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
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

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Gets a connection"), AutoData]
        public async Task Gets_A_Connection(string username)
        {
            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectToUserAsync(username);
            }

            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Invalidates cache if invalidateCache is true"), AutoData]
        public async Task Invalidates_Cache_If_InvalidateCache_Is_True(string username)
        {
            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectToUserAsync(username, invalidateCache: true);
            }

            mocks.PeerConnectionManager.Verify(m => m.TryInvalidateMessageConnectionCache(username), Times.Once);
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Creates a diagnostic message if the cache was invalidated"), AutoData]
        public async Task Creates_A_Diagnostic_Message_If_The_Cache_Was_Invalidated(string username)
        {
            var (_, mocks) = GetFixture();

            mocks.PeerConnectionManager.Setup(m => m.TryInvalidateMessageConnectionCache(It.IsAny<string>()))
                .Returns(true);

            mocks.Waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            string diagnostic = null;

            using (var client = new SoulseekClient(
                peerConnectionManager: mocks.PeerConnectionManager.Object,
                waiter: mocks.Waiter.Object,
                options: new SoulseekClientOptions(minimumDiagnosticLevel: DiagnosticLevel.Debug)))
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                client.DiagnosticGenerated += (_, e) => diagnostic = e.Message;

                await client.ConnectToUserAsync(username, invalidateCache: true);

                Assert.Equal($"Invalidated message connection cache for {username}", diagnostic);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
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

        [Trait("Category", "ConnectToUserAsync")]
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

        [Trait("Category", "ConnectToUserAsync")]
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

        [Trait("Category", "ConnectToUserAsync")]
        [Fact(DisplayName = "Throws SoulseekClientException on exception")]
        public async Task Throws_SoulseekClientException_On_Exception()
        {
            var (client, mocks) = GetFixture();

            mocks.Client.Setup(m => m.GetUserEndPointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("foo"));

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("foo", ex.InnerException.Message);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
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
                Client.Setup(m => m.Waiter).Returns(Waiter.Object);

                PeerConnectionManager.Setup(m => m.TryInvalidateMessageConnectionCache(It.IsAny<string>()))
                    .Returns(false);
                PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<string>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new Mock<IMessageConnection>().Object));
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
        }
    }
}
