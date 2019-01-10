// <copyright file="PeerTransferRequestIncomingTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Messaging.Responses
{
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Responses;
    using System;
    using Xunit;

    public class PeerTransferRequestIncomingTests
    {
        private Random Random { get; } = new Random();

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with the given data")]
        public void Instantiates_With_The_Given_Data()
        {
            var dir = (TransferDirection)Random.Next(2);
            var token = Random.Next();
            var file = Guid.NewGuid().ToString();
            var size = Random.Next();

            PeerTransferRequestIncoming response = null;

            var ex = Record.Exception(() => response = new PeerTransferRequestIncoming(dir, token, file, size));

            Assert.Null(ex);

            Assert.Equal(dir, response.Direction);
            Assert.Equal(token, response.Token);
            Assert.Equal(file, response.Filename);
            Assert.Equal(size, response.FileSize);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .Build();

            var ex = Record.Exception(() => PeerTransferRequestIncoming.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerTransferRequest)
                .Build();

            var ex = Record.Exception(() => PeerTransferRequestIncoming.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data")]
        public void Parse_Returns_Expected_Data()
        {
            var dir = Random.Next(2);
            var token = Random.Next();
            var file = Guid.NewGuid().ToString();
            var size = Random.Next();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerTransferRequest)
                .WriteInteger(dir)
                .WriteInteger(token)
                .WriteString(file)
                .WriteInteger(size)
                .Build();

            var response = PeerTransferRequestIncoming.Parse(msg);

            Assert.Equal(dir, (int)response.Direction);
            Assert.Equal(token, response.Token);
            Assert.Equal(file, response.Filename);
            Assert.Equal(size, response.FileSize);
        }
    }
}
