// <copyright file="MessageConnectionFactoryTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Messaging
{
    using System.Net;
    using AutoFixture.Xunit2;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class MessageConnectionFactoryTests
    {
        [Trait("Category", "GetConnection")]
        [Theory(DisplayName = "GetConnection returns expected IConnection"), AutoData]
        public void GetConnection_Returns_Expected_IConnection(MessageConnectionType type, string username, IPAddress ip, int port, ConnectionOptions options)
        {
            var f = new MessageConnectionFactory();

            var c = f.GetMessageConnection(type, username, ip, port, options);

            Assert.Equal(type, c.Type);
            Assert.Equal(username, c.Username);
            Assert.Equal(ip, c.IPAddress);
            Assert.Equal(port, c.Port);
            Assert.Equal(options, c.Options);
        }
    }
}
