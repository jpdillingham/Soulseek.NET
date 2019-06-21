// <copyright file="PeerInitResponseTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class PeerInitResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string username, string transferType, int token)
        {
            var r = new PeerInitResponse(username, transferType, token);

            Assert.Equal(username, r.Username);
            Assert.Equal(transferType, r.TransferType);
            Assert.Equal(token, r.Token);
        }

        [Trait("Category", "TryParse")]
        [Fact(DisplayName = "TryParse returns false on code mismatch")]
        public void TryParse_Returns_False_On_Code_Mismatch()
        {
            var msg = new List<byte>();

            msg.AddRange(BitConverter.GetBytes(0)); // overall length, ignored for this test.
            msg.Add((byte)InitializationCode.PierceFirewall);

            var r = PeerInitResponse.TryParse(msg.ToArray(), out var result);

            Assert.False(r);
            Assert.Null(result);
        }

        [Trait("Category", "TryParse")]
        [Theory(DisplayName = "TryParse returns false on missing data"), AutoData]
        public void TryParse_Returns_False_On_Missing_Data(string username, char type)
        {
            var msg = new List<byte>();

            msg.AddRange(BitConverter.GetBytes(0)); // overall length, ignored for this test.
            msg.Add((byte)InitializationCode.PeerInit);

            msg.AddRange(BitConverter.GetBytes(3)); // name len
            msg.AddRange(Encoding.ASCII.GetBytes(username)); // name
            msg.AddRange(BitConverter.GetBytes(1)); // type len
            msg.AddRange(Encoding.ASCII.GetBytes(type.ToString(CultureInfo.InvariantCulture))); // type

            // omit token
            var r = PeerInitResponse.TryParse(msg.ToArray(), out var result);

            Assert.False(r);
            Assert.Null(result);
        }

        [Trait("Category", "TryParse")]
        [Theory(DisplayName = "TryParse returns expected data"), AutoData]
        public void TryParse_Returns_Expected_Data(string username, char type, int token)
        {
            var msg = new List<byte>();

            msg.AddRange(BitConverter.GetBytes(0)); // overall length, ignored for this test.
            msg.Add((byte)InitializationCode.PeerInit);

            msg.AddRange(BitConverter.GetBytes(username.Length)); // name len
            msg.AddRange(Encoding.ASCII.GetBytes(username)); // name
            msg.AddRange(BitConverter.GetBytes(1)); // type len
            msg.AddRange(Encoding.ASCII.GetBytes(type.ToString(CultureInfo.InvariantCulture))); // type
            msg.AddRange(BitConverter.GetBytes(token));

            // omit token
            var r = PeerInitResponse.TryParse(msg.ToArray(), out var result);

            Assert.True(r);
            Assert.NotNull(result);

            Assert.Equal(username, result.Username);
            Assert.Equal(type.ToString(), result.TransferType);
            Assert.Equal(token, result.Token);
        }
    }
}
