// <copyright file="ConnectionFactoryTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System.Net;
    using AutoFixture.Xunit2;
    using Soulseek.Network;
    using Xunit;

    public class ConnectionFactoryTests
    {
        [Trait("Category", "GetConnection")]
        [Theory(DisplayName = "GetConnection returns the expected connection"), AutoData]
        internal void GetConneciton_Returns_The_Expected_Connection(IPEndPoint endpoint, ConnectionOptions options)
        {
            var c = new ConnectionFactory().GetTransferConnection(endpoint, options);

            Assert.Equal(endpoint.Address, c.IPEndPoint.Address);
            Assert.Equal(endpoint.Port, c.IPEndPoint.Port);
            Assert.Equal(options, c.Options);
        }

        [Trait("Category", "GetMessageConnection")]
        [Theory(DisplayName = "GetMessageConnection returns the expected connection"), AutoData]
        internal void GetMessageConneciton_Returns_The_Expected_Connection(string username, IPEndPoint endpoint, ConnectionOptions options)
        {
            var c = new ConnectionFactory().GetMessageConnection(username, endpoint, options);

            Assert.Equal(endpoint.Address, c.IPEndPoint.Address);
            Assert.Equal(endpoint.Port, c.IPEndPoint.Port);
            Assert.Equal(options, c.Options);
        }
    }
}
