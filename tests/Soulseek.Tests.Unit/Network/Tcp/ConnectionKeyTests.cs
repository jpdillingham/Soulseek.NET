﻿// <copyright file="ConnectionKeyTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Network.Tcp
{
    using System.Collections.Generic;
    using System.Net;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ConnectionKeyTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with ip endpoint")]
        public void Instantiates_With_IP_EndPoint()
        {
            var ip = new IPEndPoint(new IPAddress(0x0), 1);

            ConnectionKey k = null;
            var ex = Record.Exception(() => k = new ConnectionKey(ip));

            Assert.Null(ex);
            Assert.NotNull(k);

            Assert.Equal(ip.Address, k.IPEndPoint.Address);
            Assert.Equal(1, k.IPEndPoint.Port);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with username, ip endpoint and type")]
        public void Instantiates_With_Username_IP_EndPoint_And_Type()
        {
            var ip = new IPEndPoint(new IPAddress(0x0), 1);

            ConnectionKey k = null;
            var ex = Record.Exception(() => k = new ConnectionKey("username", ip));

            Assert.Null(ex);
            Assert.NotNull(k);

            Assert.Equal("username", k.Username);
            Assert.Equal(ip.Address, k.IPEndPoint.Address);
            Assert.Equal(1, k.IPEndPoint.Port);
        }

        public static IEnumerable<object[]> GetHashCodeData => new List<object[]>
        {
            new object[] { null, null },
            new object[] { null, new IPEndPoint(new IPAddress(0x0), 0) },
            new object[] { null, null },
            new object[] { "a", new IPEndPoint(new IPAddress(0x1), 0) },
        };

        [Trait("Category", "Hash Code")]
        [Theory(DisplayName = "GetHashCode matches")]
        [MemberData(nameof(GetHashCodeData))]
        public void GetHashCode_Matches(string username, IPEndPoint endpoint)
        {
            var a = new ConnectionKey(username, endpoint);
            var b = new ConnectionKey(username, endpoint);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Trait("Category", "Hash Code")]
        [Fact(DisplayName = "GetHashCode does not match if key differs")]
        public void GetHashCode_Does_Not_Match_If_Key_Differs()
        {
            var a = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x0), 1));
            var b = new ConnectionKey("b", new IPEndPoint(new IPAddress(0x0), 1));

            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns true when equal")]
        public void Equals_Returns_True_When_Equal()
        {
            var a = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x0), 1));
            var b = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x0), 1));

            Assert.True(a.Equals(b));
            Assert.True(b.Equals(a));
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns false when not equal")]
        public void Equals_Returns_False_When_Not_Equal()
        {
            var a = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x0), 1));
            var b = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x1), 1));

            Assert.False(a.Equals(b));
            Assert.False(b.Equals(a));
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns false when different type")]
        public void Equals_Returns_False_When_Different_Type()
        {
            var a = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x0), 1));
            var b = "foo";

            Assert.False(a.Equals(b));
            Assert.False(b.Equals(a));
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals handles boxed instances")]
        public void Equals_Handles_Boxed_Instances()
        {
            var a = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x0), 1));
            var b = new ConnectionKey("a", new IPEndPoint(new IPAddress(0x0), 1));

            Assert.True(a.Equals((object)b));
            Assert.True(b.Equals((object)a));
        }
    }
}
