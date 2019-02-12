// <copyright file="GetSolicitedPeerConnectionAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class GetSolicitedPeerConnectionAsyncTests
    {
        [Trait("Category", "GetSolicitedPeerConnectionAsync")]
        [Theory(DisplayName = "Returns expected IMessageConnection instance"), AutoData]
        public async Task Returns_IMessageConnection_Instance(string username, IPAddress ipAddress, int port, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "P", ipAddress, port, token);
            var options = new ConnectionOptions();

            var s = new SoulseekClient();

            IMessageConnection conn = null;

            var ex = await Record.ExceptionAsync(async () => conn = await s.InvokeMethod<Task<IMessageConnection>>("GetSolicitedPeerConnectionAsync", ctpr, options));

            Assert.Null(ex);
            Assert.NotNull(conn);

            Assert.Equal(username, conn.Username);
            Assert.Equal(ipAddress, conn.IPAddress);
            Assert.Equal(port, conn.Port);
            Assert.Equal(ctpr, conn.Context);
        }

        [Trait("Category", "GetSolicitedPeerConnectionAsync")]
        [Theory(DisplayName = "Adds instance to PeerConnectionManager"), AutoData]
        public async Task Adds_Instance_To_PeerConnectionManager(string username, IPAddress ipAddress, int port, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "P", ipAddress, port, token);
            var options = new ConnectionOptions();

            var pcm = new Mock<IConnectionManager<IMessageConnection>>();
            pcm.Setup(m => m.AddAsync(It.IsAny<IMessageConnection>()))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, peerConnectionManager: pcm.Object);

            await s.InvokeMethod<Task<IMessageConnection>>("GetSolicitedPeerConnectionAsync", ctpr, options);

            pcm.Verify(m => m.AddAsync(It.IsAny<IMessageConnection>()), Times.Once);
        }
    }
}
