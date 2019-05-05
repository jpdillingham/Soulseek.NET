// <copyright file="GetPeerConnectionKeyAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Messaging.Tcp;
    using Soulseek.Tcp;
    using Xunit;

    public class GetPeerConnectionKeyAsyncTests
    {
        [Trait("Category", "GetPeerConnectionKeyAsync")]
        [Theory(DisplayName = "GetPeerConnectionKeyAsync returns expected ConnectionKey"), AutoData]
        public async Task GetPeerConnectionKeyAsync_Returns_Expected_ConnectionKey(string username, IPAddress ip, int port)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: conn.Object);

            ConnectionKey result = null;
            var ex = await Record.ExceptionAsync(async () => result = await s.InvokeMethod<Task<ConnectionKey>>("GetPeerConnectionKeyAsync", username, CancellationToken.None));

            Assert.Null(ex);
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
            Assert.Equal(ip, result.IPAddress);
            Assert.Equal(port, result.Port);
        }
    }
}
