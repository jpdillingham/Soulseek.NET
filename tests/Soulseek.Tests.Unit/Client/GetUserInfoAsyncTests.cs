// <copyright file="GetUserInfoAsyncTests.cs" company="JP Dillingham">
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

    public class GetUserInfoAsyncTests
    {
        [Trait("Category", "GetUserInfoAsync")]
        [Theory(DisplayName = "GetUserInfoAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetUserInfoAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.GetUserInfoAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetUserInfoAsync")]
        [Theory(DisplayName = "GetUserInfoAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetUserInfoAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(async () => await s.GetUserInfoAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetUserInfoAsync")]
        [Theory(DisplayName = "GetUserInfoAsync returns expected info"), AutoData]
        public async Task GetUserInfoAsync_Returns_Expected_Info(string username, string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var result = new UserInfoResponse(description, picture, uploadSlots, queueLength, hasFreeSlot);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInfoResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var info = await s.GetUserInfoAsync(username);

                Assert.Equal(result.Description, info.Description);
                Assert.Equal(result.HasPicture, info.HasPicture);
                Assert.Equal(result.Picture, info.Picture);
                Assert.Equal(result.UploadSlots, info.UploadSlots);
                Assert.Equal(result.QueueLength, info.QueueLength);
                Assert.Equal(result.HasFreeUploadSlot, info.HasFreeUploadSlot);
            }
        }

        [Trait("Category", "GetUserInfoAsync")]
        [Theory(DisplayName = "GetUserInfoAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetUserInfoAsync_Throws_TimeoutException_On_Timeout(string username, string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var result = new UserInfoResponse(description, picture, uploadSlots, queueLength, hasFreeSlot);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInfoResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new TimeoutException()));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                UserInfoResponse info = null;
                var ex = await Record.ExceptionAsync(async () => info = await s.GetUserInfoAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetUserInfoAsync")]
        [Theory(DisplayName = "GetUserInfoAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetUserInfoAsync_Throws_OperationCanceledException_On_Cancellation(string username, string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var result = new UserInfoResponse(description, picture, uploadSlots, queueLength, hasFreeSlot);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInfoResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new OperationCanceledException()));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                UserInfoResponse info = null;
                var ex = await Record.ExceptionAsync(async () => info = await s.GetUserInfoAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "GetUserInfoAsync")]
        [Theory(DisplayName = "GetUserInfoAsync throws UserInfoException on throw"), AutoData]
        public async Task GetUserInfoAsync_Throws_UserInfoException_On_Throw(string username, string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var result = new UserInfoResponse(description, picture, uploadSlots, queueLength, hasFreeSlot);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInfoResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new ConnectionException("foo")));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                UserInfoResponse info = null;
                var ex = await Record.ExceptionAsync(async () => info = await s.GetUserInfoAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserInfoException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }
    }
}
