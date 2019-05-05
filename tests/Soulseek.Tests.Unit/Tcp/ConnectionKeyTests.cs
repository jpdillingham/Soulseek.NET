// <copyright file="ConnectionKeyTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Tcp
{
    using System.Collections.Generic;
    using System.Net;
    using Soulseek.Messaging.Tcp;
    using Soulseek.Tcp;
    using Xunit;

    public class ConnectionKeyTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with ip and port")]
        public void Instantiates_With_IP_And_Port()
        {
            var ip = new IPAddress(0x0);

            ConnectionKey k = null;
            var ex = Record.Exception(() => k = new ConnectionKey(ip, 0));

            Assert.Null(ex);
            Assert.NotNull(k);

            Assert.Equal(ip, k.IPAddress);
            Assert.Equal(0, k.Port);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with username, ip, port and type")]
        public void Instantiates_With_Username_IP_Port_And_Type()
        {
            var ip = new IPAddress(0x0);

            ConnectionKey k = null;
            var ex = Record.Exception(() => k = new ConnectionKey("username", ip, 0, MessageConnectionType.Server));

            Assert.Null(ex);
            Assert.NotNull(k);

            Assert.Equal("username", k.Username);
            Assert.Equal(ip, k.IPAddress);
            Assert.Equal(0, k.Port);
            Assert.Equal(MessageConnectionType.Server, k.Type);
        }

        public static IEnumerable<object[]> GetHashCodeData => new List<object[]>
        {
            new object[] { null, null, 0, MessageConnectionType.Default },
            new object[] { null, new IPAddress(0x0), 0, MessageConnectionType.Default },
            new object[] { null, null, 1, MessageConnectionType.Default },
            new object[] { "a", new IPAddress(0x1), 0, MessageConnectionType.Peer },
        };

        [Trait("Category", "Hash Code")]
        [Theory(DisplayName = "GetHashCode matches")]
        [MemberData(nameof(GetHashCodeData))]
        public void GetHashCode_Matches(string username, IPAddress ipAddress, int port, MessageConnectionType type)
        {
            var a = new ConnectionKey(username, ipAddress, port, type);
            var b = new ConnectionKey(username, ipAddress, port, type);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Trait("Category", "Hash Code")]
        [Fact(DisplayName = "GetHashCode does not match if key differs")]
        public void GetHashCode_Does_Not_Match_If_Key_Differs()
        {
            var a = new ConnectionKey("a", new IPAddress(0x0), 1, MessageConnectionType.Default);
            var b = new ConnectionKey("b", new IPAddress(0x0), 1, MessageConnectionType.Default);

            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns true when equal")]
        public void Equals_Returns_True_When_Equal()
        {
            var a = new ConnectionKey("a", new IPAddress(0x0), 1, MessageConnectionType.Default);
            var b = new ConnectionKey("a", new IPAddress(0x0), 1, MessageConnectionType.Default);

            Assert.True(a.Equals(b));
            Assert.True(b.Equals(a));
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns false when not equal")]
        public void Equals_Returns_False_When_Not_Equal()
        {
            var a = new ConnectionKey("a", new IPAddress(0x0), 1, MessageConnectionType.Default);
            var b = new ConnectionKey("a", new IPAddress(0x1), 1, MessageConnectionType.Default);

            Assert.False(a.Equals(b));
            Assert.False(b.Equals(a));
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns false when different type")]
        public void Equals_Returns_False_When_Different_Type()
        {
            var a = new ConnectionKey("a", new IPAddress(0x0), 1, MessageConnectionType.Default);
            var b = "foo";

            Assert.False(a.Equals(b));
            Assert.False(b.Equals(a));
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals handles boxed instances")]
        public void Equals_Handles_Boxed_Instances()
        {
            var a = new ConnectionKey("a", new IPAddress(0x0), 1, MessageConnectionType.Default);
            var b = new ConnectionKey("a", new IPAddress(0x0), 1, MessageConnectionType.Default);

            Assert.True(a.Equals((object)b));
            Assert.True(b.Equals((object)a));
        }
    }
}
