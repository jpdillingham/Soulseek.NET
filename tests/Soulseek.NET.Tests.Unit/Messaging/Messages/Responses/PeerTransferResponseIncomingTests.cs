// <copyright file="PeerTransferResponseIncomingTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Messaging.Messages
{
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using System;
    using Xunit;

    public class PeerTransferResponseIncomingTests
    {
        private Random Random { get; } = new Random();

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with the given data")]
        public void Instantiates_With_The_Given_Data()
        {
            var token = Random.Next();
            var allowed = Random.Next() % 2 == 0;
            var msg = Guid.NewGuid().ToString();
            var size = Random.Next();

            PeerTransferResponseIncoming response = null;

            var ex = Record.Exception(() => response = new PeerTransferResponseIncoming(token, allowed, size, msg));

            Assert.Null(ex);

            Assert.Equal(token, response.Token);
            Assert.Equal(allowed, response.Allowed);
            Assert.Equal(msg, response.Message);
            Assert.Equal(size, response.FileSize);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .Build();

            var ex = Record.Exception(() => PeerTransferResponseIncoming.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerTransferResponse)
                .Build();

            var ex = Record.Exception(() => PeerTransferResponseIncoming.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data when allowed")]
        public void Parse_Returns_Expected_Data_When_Allowed()
        {
            var token = Random.Next();
            var size = Random.Next();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerTransferResponse)
                .WriteInteger(token)
                .WriteByte(0x1)
                .WriteInteger(size)
                .Build();

            var response = PeerTransferResponseIncoming.Parse(msg);

            Assert.Equal(token, response.Token);
            Assert.True(response.Allowed);
            Assert.Equal(size, response.FileSize);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data when disallowed")]
        public void Parse_Returns_Expected_Data_When_Disallowed()
        {
            var token = Random.Next();
            var size = Random.Next();
            var message = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerTransferResponse)
                .WriteInteger(token)
                .WriteByte(0x0)
                .WriteString(message)
                .Build();

            var response = PeerTransferResponseIncoming.Parse(msg);

            Assert.Equal(token, response.Token);
            Assert.False(response.Allowed);
            Assert.Equal(message, response.Message);
        }
    }
}
