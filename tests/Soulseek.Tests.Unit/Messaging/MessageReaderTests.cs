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

namespace Soulseek.Tests.Unit.Messaging
{
    using System;
    using System.Reflection;
    using System.Text;
    using AutoFixture.Xunit2;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Xunit;

    public class MessageReaderTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentNullException given null byte array")]
        public void Instantiation_Throws_ArgumentNullException_Given_Null_Byte_Array()
        {
            byte[] bytes = null;
            var ex = Record.Exception(() => new MessageReader<MessageCode>(bytes));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentOutOfRangeException given short byte array")]
        public void Instantiation_Throws_ArgumentOutOfRangeException_Given_Short_Byte_Array()
        {
            byte[] bytes = new byte[7];
            var ex = Record.Exception(() => new MessageReader<MessageCode>(bytes));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentOutOfRangeException given empty byte array")]
        public void Instantiation_Throws_ArgumentOutOfRangeException_Given_Empty_Byte_Array()
        {
            byte[] bytes = Array.Empty<byte>();
            var ex = Record.Exception(() => new MessageReader<MessageCode>(bytes));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws ArgumentNullException given null message")]
        public void Instantiation_Throws_ArgumentNullException_Given_Null_Message()
        {
            byte[] msg = null;
            var ex = Record.Exception(() => new MessageReader<MessageCode>(msg));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly given valid Message")]
        public void Instantiates_Properly_Given_Valid_Message()
        {
            var num = new Random().Next();
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.PeerBrowseRequest, code);
            Assert.Equal(BitConverter.GetBytes(num), reader.Payload.ToArray());
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
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build();

            var reader = new MessageReader<MessageCode>(msgBytes);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.PeerBrowseRequest, code);
            Assert.Equal(BitConverter.GetBytes(num), reader.Payload.ToArray());
            Assert.Equal(0, reader.Position);
            Assert.Equal(num, reader.ReadInteger());
            Assert.Equal(4, reader.Position);
        }

        [Trait("Category", "Seek")]
        [Fact(DisplayName = "Seek changes position")]
        public void Seek_Changes_Position()
        {
            var num = new Random().Next();
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var initial = reader.Position;

            reader.Seek(2);

            Assert.Equal(0, initial);
            Assert.Equal(2, reader.Position);
        }

        [Trait("Category", "Seek")]
        [Fact(DisplayName = "Seek throws ArgumentOutOfRangeException on negative")]
        public void Seek_Throws_ArgumentOutOfRangeException_On_Negative()
        {
            var num = new Random().Next();
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.Seek(-1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Seek")]
        [Fact(DisplayName = "Seek throws ArgumentOutOfRangeException on too large")]
        public void Seek_Throws_ArgumentOutOfRangeException_On_Too_Large()
        {
            var num = new Random().Next();
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.Seek(5));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "ReadInteger")]
        [Fact(DisplayName = "ReadInteger returns expected data")]
        public void ReadInteger_Returns_Expected_Data()
        {
            var num = new Random().Next();
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(num)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            Assert.Equal(num, reader.ReadInteger());
        }

        [Trait("Category", "ReadInteger")]
        [Fact(DisplayName = "ReadInteger throws MessageReadException given no data")]
        public void ReadInteger_Throws_MessageReadException_If_No_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.ReadInteger());

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "ReadLong")]
        [Fact(DisplayName = "ReadLong returns expected data")]
        public void ReadLong_Returns_Expected_Data()
        {
            var num = new Random().Next();
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteLong((long)num)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            Assert.Equal(num, reader.ReadLong());
        }

        [Trait("Category", "ReadLong")]
        [Fact(DisplayName = "ReadLong throws MessageReadException given no data")]
        public void ReadLong_Throws_MessageReadException_If_No_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.ReadLong());

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "ReadByte")]
        [Fact(DisplayName = "ReadByte returns expected data")]
        public void ReadByte_Returns_Expected_Data()
        {
            var bytes = new byte[1];
            new Random().NextBytes(bytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteByte(bytes[0])
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            Assert.Equal(bytes[0], reader.ReadByte());
        }

        [Trait("Category", "ReadByte")]
        [Fact(DisplayName = "ReadByte throws MessageReadException given no data")]
        public void ReadByte_Throws_MessageReadException_If_No_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.ReadByte());

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "ReadBytes")]
        [Fact(DisplayName = "ReadBytes returns expected data")]
        public void ReadBytes_Returns_Expected_Data()
        {
            var rand = new Random();

            var bytes = new byte[rand.Next(100)];
            new Random().NextBytes(bytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteBytes(bytes)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            Assert.Equal(bytes.Length, reader.Payload.Length);
            Assert.Equal(bytes, reader.ReadBytes(bytes.Length));
        }

        [Trait("Category", "ReadBytes")]
        [Fact(DisplayName = "ReadBytes from nonzero position returns expected data")]
        public void ReadBytes_From_Nonzero_Position_Returns_Expected_Data()
        {
            var rand = new Random();

            var bytes = new byte[rand.Next(100)];
            new Random().NextBytes(bytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteString("foo")
                .WriteBytes(bytes)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            reader.ReadString();

            Assert.Equal(bytes, reader.ReadBytes(bytes.Length));
        }

        [Trait("Category", "ReadBytes")]
        [Fact(DisplayName = "ReadBytes throws MessageReadException given no data")]
        public void ReadBytes_Throws_MessageReadException_If_No_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.ReadBytes(1));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "ReadBytes")]
        [Fact(DisplayName = "ReadBytes throws MessageReadException given length greater than payload length")]
        public void ReadBytes_Throws_MessageReadException_If_Length_Greater_Than_Payload_Length()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2 })
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.ReadBytes(4));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "ReadBytes")]
        [Fact(DisplayName = "ReadBytes from nonzero position throws MessageReadException given length greater than payload length")]
        public void ReadBytes_From_Nonzero_Position_Throws_MessageReadException_If_Length_Greater_Than_Payload_Length()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(42)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2 })
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var integer = reader.ReadInteger();

            var ex = Record.Exception(() => reader.ReadBytes(4));

            Assert.Equal(42, integer);
            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "ReadString")]
        [Fact(DisplayName = "ReadString returns expected data")]
        public void ReadString_Returns_Expected_Data()
        {
            var str = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteString(str)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            Assert.Equal(str, reader.ReadString());
        }

        [Trait("Category", "ReadString")]
        [Fact(DisplayName = "ReadString returns empty string given empty string")]
        public void ReadString_Returns_Empty_String_Given_Empty_String()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteString(string.Empty)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            Assert.Equal(string.Empty, reader.ReadString());
        }

        [Trait("Category", "ReadString")]
        [Fact(DisplayName = "ReadString from nonzero position returns expected data")]
        public void ReadString_From_Nonzero_Position_Returns_Expected_Data()
        {
            var str = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(42)
                .WriteString(str)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);
            reader.ReadInteger();

            Assert.Equal(str, reader.ReadString());
        }

        [Trait("Category", "ReadString")]
        [Fact(DisplayName = "ReadString throws MessageReadException given no data")]
        public void ReadString_Throws_MessageReadException_Given_No_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.ReadString());

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "ReadString")]
        [Fact(DisplayName = "ReadString throws MessageReadException given mismatched string length and data")]
        public void ReadString_Throws_MessageReadException_Given_Mismatched_String_Length_And_Data()
        {
            var str = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger((str.Length * 8) + 1)
                .WriteBytes(Encoding.ASCII.GetBytes(str))
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.ReadString());

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
            Assert.Contains("extends beyond", ex.Message, StringComparison.InvariantCultureIgnoreCase); // fragile, call the cops idc.
        }

        [Trait("Category", "Decompress")]
        [Theory(DisplayName = "Decompress produces valid data"), AutoData]
        public void Decompress_Produces_Valid_Data(string txt, int num, string txt2)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerInfoRequest)
                .WriteString(txt)
                .WriteInteger(num)
                .WriteString(txt2)
                .Compress()
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            reader.Decompress();

            Assert.Equal(txt, reader.ReadString());
            Assert.Equal(num, reader.ReadInteger());
            Assert.Equal(txt2, reader.ReadString());
        }

        [Trait("Category", "Decompress")]
        [Fact(DisplayName = "Decompress throws InvalidOperationException on empty payload")]
        public void Decompress_Throws_InvalidOperationException_On_Empty_Payload()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerInfoRequest)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.Decompress());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Decompress")]
        [Fact(DisplayName = "Decompress throws InvalidOperationException when already decompressed")]
        public void Decompress_Throws_InvalidOperationException_When_Already_Decompressed()
        {
            var txt = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerInfoRequest)
                .WriteString(txt)
                .Compress()
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            reader.Decompress();

            var ex = Record.Exception(() => reader.Decompress());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Decompress")]
        [Fact(DisplayName = "Decompress throws MessageCompressionException on compression exception")]
        public void Decompress_Throws_MessageCompressionException_On_Compression_Exception()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerInfoRequest)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);

            var ex = Record.Exception(() => reader.InvokeMethod("Decompress", BindingFlags.NonPublic | BindingFlags.Instance, null, null));

            Assert.NotNull(ex);
            Assert.NotNull(ex.InnerException);
            Assert.NotNull(ex.InnerException.InnerException);
            Assert.IsType<MessageCompressionException>(ex.InnerException.InnerException);
        }

        [Trait("Category", "HasMoreData")]
        [Fact(DisplayName = "HasMoreData returns expected value")]
        public void HasMoreData_Returns_Expected_Value()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.PeerBrowseRequest)
                .WriteInteger(1)
                .WriteInteger(2)
                .Build();

            var reader = new MessageReader<MessageCode>(msg);
            reader.ReadCode();

            Assert.True(reader.HasMoreData);

            reader.ReadInteger();

            Assert.True(reader.HasMoreData);

            reader.ReadInteger();

            Assert.False(reader.HasMoreData);
        }
    }
}
