// <copyright file="GetConnectionAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class GetConnectionAsyncTests
    {
        [Trait("Category", "GetPeerConnectionKeyAsync")]
        [Theory(DisplayName = "GetPeerConnectionKeyAsync returns expected ConnectionKey"), AutoData]
        public async Task GetPeerConnectionKeyAsync_Returns_Expected_ConnectionKey(string username, IPAddress ip, int port)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, messageWaiter: waiter.Object, serverConnection: conn.Object);

            ConnectionKey result = null;
            var ex = await Record.ExceptionAsync(async () => result = await s.InvokeMethod<Task<ConnectionKey>>("GetPeerConnectionKeyAsync", username));

            Assert.Null(ex);
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
            Assert.Equal(ip, result.IPAddress);
            Assert.Equal(port, result.Port);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync returns expected IConnection"), AutoData]
        internal async Task GetTransferConnectionAsync_Returns_Expected_IConnection(string username, string type, IPAddress ipAddress, int port, int token, ConnectionOptions options)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            conn.Setup(m => m.Port)
                .Returns(port);

            var response = new ConnectToPeerResponse(username, type, ipAddress, port, token);

            var s = new SoulseekClient();

            IConnection result = null;

            var ex = await Record.ExceptionAsync(async () => result = await s.InvokeMethod<Task<IConnection>>("GetTransferConnectionAsync", response, options, conn.Object));

            Assert.Null(ex);
            Assert.Equal(response.IPAddress, result.IPAddress);
            Assert.Equal(response.Port, result.Port);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync connects and pierces firewall"), AutoData]
        internal async Task GetTransferConnectionAsync_Connects_And_Pierces_Firewall(string username, string type, IPAddress ipAddress, int port, int token, ConnectionOptions options)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            conn.Setup(m => m.Port)
                .Returns(port);
            conn.Setup(m => m.ConnectAsync())
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            var response = new ConnectToPeerResponse(username, type, ipAddress, port, token);

            var s = new SoulseekClient();

            await s.InvokeMethod<Task<IConnection>>("GetTransferConnectionAsync", response, options, conn.Object);

            conn.Verify(m => m.ConnectAsync(), Times.Once);
            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>()), Times.Once);
        }
    }
}
