// <copyright file="GetSimilarUsersAsyncTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetSimilarUsersAsyncTests
    {
        [Trait("Category", "GetSimilarUsersAsync")]
        [Fact(DisplayName = "GetSimilarUsersAsync throws ArgumentException on undefined SimilarUserType")]
        public async Task GetSimilarUsersAsync_Throws_ArgumentException_On_Undefined_SimilarUserType()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync((SimilarUserType)(-1)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws ArgumentException when Interest type is given without an interest")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetSimilarUsersAsync_Throws_ArgumentException_When_Interest_Type_Given_Without_Interest(string interest)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Interest, interest));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws ArgumentException when Personal type is given with an interest"), AutoData]
        public async Task GetSimilarUsersAsync_Throws_ArgumentException_When_Personal_Type_Given_With_Interest(string interest)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Personal, interest));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetSimilarUsersAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Personal));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync returns expected info for Personal type"), AutoData]
        public async Task GetSimilarUsersAsync_Returns_Expected_Info_For_Personal_Type(List<(string Username, int Rating)> users)
        {
            var result = new PersonalSimilarUsersResponse(users);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PersonalSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var similarUsers = await s.GetSimilarUsersAsync(SimilarUserType.Personal);

                Assert.Equal(users.Select(u => (u.Username, (int?)u.Rating)), similarUsers);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync returns expected info for Interest type"), AutoData]
        public async Task GetSimilarUsersAsync_Returns_Expected_Info_For_Interest_Type(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var similarUsers = await s.GetSimilarUsersAsync(SimilarUserType.Interest, interest);

                Assert.Equal(usernames.Select(u => (u, (int?)null)), similarUsers);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync uses given CancellationToken"), AutoData]
        public async Task GetSimilarUsersAsync_Uses_Given_CancellationToken(List<(string Username, int Rating)> users)
        {
            var cancellationToken = new CancellationToken();
            var result = new PersonalSimilarUsersResponse(users);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PersonalSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GetSimilarUsersAsync(SimilarUserType.Personal, cancellationToken: cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken));
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws SoulseekClientException on throw for Personal type"), AutoData]
        public async Task GetSimilarUsersAsync_Throws_SoulseekClientException_On_Throw_For_Personal_Type(List<(string Username, int Rating)> users)
        {
            var result = new PersonalSimilarUsersResponse(users);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PersonalSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Personal));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws SoulseekClientException on throw for Interest type"), AutoData]
        public async Task GetSimilarUsersAsync_Throws_SoulseekClientException_On_Throw_For_Interest_Type(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Interest, interest));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws TimeoutException on timeout for Personal type"), AutoData]
        public async Task GetSimilarUsersAsync_Throws_TimeoutException_On_Timeout_For_Personal_Type(List<(string Username, int Rating)> users)
        {
            var result = new PersonalSimilarUsersResponse(users);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PersonalSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Personal));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws TimeoutException on timeout for Interest type"), AutoData]
        public async Task GetSimilarUsersAsync_Throws_TimeoutException_On_Timeout_For_Interest_Type(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Interest, interest));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws OperationCanceledException on cancellation for Personal type"), AutoData]
        public async Task GetSimilarUsersAsync_Throws_OperationCanceledException_On_Cancellation_For_Personal_Type(List<(string Username, int Rating)> users)
        {
            var result = new PersonalSimilarUsersResponse(users);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PersonalSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Personal));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "GetSimilarUsersAsync")]
        [Theory(DisplayName = "GetSimilarUsersAsync throws OperationCanceledException on cancellation for Interest type"), AutoData]
        public async Task GetSimilarUsersAsync_Throws_OperationCanceledException_On_Cancellation_For_Interest_Type(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetSimilarUsersAsync(SimilarUserType.Interest, interest));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
