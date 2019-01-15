// <copyright file="MessageConnectionTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Messaging.Tcp
{
    using AutoFixture.Xunit2;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using System.Net;
    using Xunit;

    public class MessageConnectionTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates peer connection with given username and IP"), AutoData]
        public void Instantiates_Peer_Connection_With_Given_Username_And_IP(string username, IPAddress ipAddress, int port, ConnectionOptions options)
        {
            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port, options);

            Assert.Equal(MessageConnectionType.Peer, c.Type);
            Assert.Equal(username, c.Username);
            Assert.Equal(ipAddress, c.IPAddress);
            Assert.Equal(port, c.Port);
            Assert.Equal(options, c.Options);

            Assert.Equal(new ConnectionKey(username, ipAddress, port, MessageConnectionType.Peer), c.Key);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates server connection with given IP"), AutoData]
        public void Instantiates_Peer_Connection_With_Given_IP(IPAddress ipAddress, int port, ConnectionOptions options)
        {
            var c = new MessageConnection(MessageConnectionType.Server, ipAddress, port, options);

            Assert.Equal(MessageConnectionType.Server, c.Type);
            Assert.Equal(ipAddress, c.IPAddress);
            Assert.Equal(port, c.Port);
            Assert.Equal(options, c.Options);

            Assert.Equal(new ConnectionKey(string.Empty, ipAddress, port, MessageConnectionType.Server), c.Key);
        }
    }
}
