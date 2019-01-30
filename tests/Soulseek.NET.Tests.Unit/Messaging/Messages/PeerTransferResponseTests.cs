// <copyright file="PeerTransferResponseTests.cs" company="JP Dillingham">
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
    using System;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Xunit;

    public class PeerTransferResponseTests
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

            PeerTransferResponse response = null;

            var ex = Record.Exception(() => response = new PeerTransferResponse(token, allowed, size, msg));

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

            var ex = Record.Exception(() => PeerTransferResponse.Parse(msg));

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

            var ex = Record.Exception(() => PeerTransferResponse.Parse(msg));

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

            var response = PeerTransferResponse.Parse(msg);

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

            var response = PeerTransferResponse.Parse(msg);

            Assert.Equal(token, response.Token);
            Assert.False(response.Allowed);
            Assert.Equal(message, response.Message);
        }

        [Trait("Category", "ToMessage")]
        [Fact(DisplayName = "ToMessage constructs the correct Message")]
        public void ToMessage_Constructs_The_Correct_Message()
        {
            var rnd = new Random();

            var token = rnd.Next();
            var size = rnd.Next();
            var message = Guid.NewGuid().ToString();
            var a = new PeerTransferResponse(token, true, size, message);
            var msg = a.ToMessage();

            Assert.Equal(MessageCode.PeerTransferResponse, msg.Code);
            Assert.Equal(4 + 4 + 1 + 4 + 4 + message.Length, msg.Length);

            var reader = new MessageReader(msg);

            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(1, reader.ReadByte());
            Assert.Equal(size, reader.ReadInteger());
            Assert.Equal(message, reader.ReadString());
        }
    }
}
