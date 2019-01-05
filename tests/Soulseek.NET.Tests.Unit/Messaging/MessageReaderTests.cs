// <copyright file="MessageReaderTests.cs" company="JP Dillingham">
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
    using Soulseek.NET.Messaging;
    using System;
    using Xunit;

    public class MessageReaderTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentNullException given null byte array")]
        public void Instantiation_Throws_ArgumentNullException_Given_Null_Byte_Array()
        {
            byte[] bytes = null;
            var ex = Record.Exception(() => new MessageReader(bytes));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentOutOfRangeException given short byte array")]
        public void Instantiation_Throws_ArgumentOutOfRangeException_Given_Short_Byte_Array()
        {
            byte[] bytes = new byte[7];
            var ex = Record.Exception(() => new MessageReader(bytes));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentOutOfRangeException given empty byte array")]
        public void Instantiation_Throws_ArgumentOutOfRangeException_Given_Empty_Byte_Array()
        {
            byte[] bytes = new byte[0];
            var ex = Record.Exception(() => new MessageReader(bytes));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentNullException given null message")]
        public void Instantiation_Throws_ArgumentNullException_Given_Null_Message()
        {
            Message msg = null;
            var ex = Record.Exception(() => new MessageReader(msg));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly given valid Message")]
        public void Instantiates_Properly_Given_Valid_Message()
        {
            var num = new Random().Next();
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build();

            var reader = new MessageReader(msg);

            Assert.Equal(MessageCode.PeerBrowseRequest, reader.Code);
            Assert.Equal(BitConverter.GetBytes(num), reader.Payload);
            Assert.Equal(0, reader.Position);
            Assert.Equal(num, reader.ReadInteger());
            Assert.Equal(4, reader.Position);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly given valid byte array")]
        public void Instantiates_Properly_Given_Valid_Byte_Array()
        {
            var num = new Random().Next();
            var msgBytes = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build()
                .ToByteArray();

            var reader = new MessageReader(msgBytes);

            Assert.Equal(MessageCode.PeerBrowseRequest, reader.Code);
            Assert.Equal(BitConverter.GetBytes(num), reader.Payload);
            Assert.Equal(0, reader.Position);
            Assert.Equal(num, reader.ReadInteger());
            Assert.Equal(4, reader.Position);
        }
    }
}
