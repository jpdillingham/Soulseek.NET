// <copyright file="AddUserAsyncTests.cs" company="JP Dillingham">
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

    public class AddUserAsyncTests
    {
        [Trait("Category", "AddUserAsync")]
        [Theory(DisplayName = "AddUserAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task AddUserAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.AddUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "AddUserAsync")]
        [Theory(DisplayName = "AddUserAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task AddUserAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(async () => await s.AddUserAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "AddUserAsync")]
        [Theory(DisplayName = "AddUserAsync returns expected info"), AutoData]
        public async Task AddUserAsync_Returns_Expected_Info(string username, bool exists, UserStatus status, int averageSpeed, int downloadCount, int fileCount, int directoryCount, string countryCode)
        {
            var result = new AddUserResponse(username, exists, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<AddUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var add = await s.AddUserAsync(username);

                Assert.Equal(result.Username, add.Username);
                Assert.Equal(result.Exists, add.Exists);
                Assert.Equal(result.Status, add.Status);
                Assert.Equal(result.AverageSpeed, add.AverageSpeed);
                Assert.Equal(result.DownloadCount, add.DownloadCount);
                Assert.Equal(result.FileCount, add.FileCount);
                Assert.Equal(result.DirectoryCount, add.DirectoryCount);
                Assert.Equal(result.CountryCode, add.CountryCode);
            }
        }

        [Trait("Category", "AddUserAsyncAsync")]
        [Theory(DisplayName = "AddUserAsyncAsync throws UserStatusException on throw"), AutoData]
        public async Task AddUserAsyncAsync_Throws_UserStatusException_On_Throw(string username, bool exists, UserStatus status, int averageSpeed, int downloadCount, int fileCount, int directoryCount, string countryCode)
        {
            var result = new AddUserResponse(username, exists, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<AddUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                AddUserResponse r = null;
                var ex = await Record.ExceptionAsync(async () => r = await s.AddUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<AddUserException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "AddUserAsyncAsync")]
        [Theory(DisplayName = "AddUserAsync throws TimeoutException on timeout"), AutoData]
        public async Task AddUserAsyncAsync_Throws_TimeoutException_On_Timeout(string username, bool exists, UserStatus status, int averageSpeed, int downloadCount, int fileCount, int directoryCount, string countryCode)
        {
            var result = new AddUserResponse(username, exists, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<AddUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                AddUserResponse r = null;
                var ex = await Record.ExceptionAsync(async () => r = await s.AddUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "AddUserAsync")]
        [Theory(DisplayName = "AddUserAsync throws OperationCanceledException on cancel"), AutoData]
        public async Task AddUserAsync_Throws_OperationCanceledException_On_Cancel(string username, bool exists, UserStatus status, int averageSpeed, int downloadCount, int fileCount, int directoryCount, string countryCode)
        {
            var result = new AddUserResponse(username, exists, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<AddUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                AddUserResponse r = null;
                var ex = await Record.ExceptionAsync(async () => r = await s.AddUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
