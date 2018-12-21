// <copyright file="ConnectionTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Tcp
{
    using Moq;
    using Soulseek.NET.Tcp;
    using System.Net;
    using Xunit;

    public class ConnectionTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            Connection c = null;

            var ip = new IPAddress(0x0);
            var port = 1;

            var ex = Record.Exception(() => c = new Connection(ip, port));

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(ip, c.IPAddress);
            Assert.Equal(port, c.Port);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given options")]
        public void Instantiates_With_Given_Options()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var options = new ConnectionOptions(1, 1, 1);

            var c = new Connection(ip, port, options);

            Assert.Equal(options, c.Options);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given TcpClient")]
        public void Instantiates_With_Given_TcpClient()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();

            var c = new Connection(ip, port, tcpClient: t.Object);

            var ct = c.GetProperty<ITcpClient>("TcpClient");

            Assert.Equal(t.Object, ct);
        }
    }
}
