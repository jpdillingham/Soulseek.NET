// <copyright file="MessageBuilderTests.cs" company="JP Dillingham">
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
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Xunit;

    public class MessageBuilderTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates without exception")]
        public void Instantiates_Without_Exception()
        {
            MessageBuilder builder = null;
            var ex = Record.Exception(() => builder = new MessageBuilder());

            Assert.Null(ex);
        }

        [Trait("Category", "Code")]
        [Fact(DisplayName = "Code sets code bytes")]
        public void Code_Sets_Code_Bytes()
        {
            var builder = new MessageBuilder();

            builder.Code(MessageCode.PeerBrowseRequest);

            var code = builder.GetProperty<List<byte>>("CodeBytes");

            Assert.Equal(BitConverter.GetBytes((int)MessageCode.PeerBrowseRequest), code);
        }

        [Trait("Category", "Code")]
        [Fact(DisplayName = "Code resets code bytes")]
        public void Code_Resets_Code_Bytes()
        {
            var builder = new MessageBuilder();

            builder.Code(MessageCode.PeerBrowseRequest);

            var code1 = builder.GetProperty<List<byte>>("CodeBytes");

            builder.Code(MessageCode.PeerBrowseResponse);

            var code2 = builder.GetProperty<List<byte>>("CodeBytes");

            Assert.Equal(BitConverter.GetBytes((int)MessageCode.PeerBrowseRequest).ToList(), code1);
            Assert.Equal(BitConverter.GetBytes((int)MessageCode.PeerBrowseResponse).ToList(), code2);
        }

        [Trait("Category", "Build")]
        [Fact(DisplayName = "Build throws when code not set")]
        public void Build_Throws_When_Code_Not_Set()
        {
            var builder = new MessageBuilder();

            var ex = Record.Exception(() => builder.Build());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Build")]
        [Fact(DisplayName = "Build returns empty message when empty")]
        public void Build_Returns_Empty_Message_When_Empty()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .Build();

            Assert.Equal(MessageCode.PeerBrowseRequest, msg.Code);
            Assert.Equal(4, msg.Length);
            Assert.Empty(msg.Payload);
        }

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress produces valid data")]
        public void Compress_Produces_Valid_Data()
        {
            var txt = Guid.NewGuid().ToString();
            var num = new Random().Next();
            var txt2 = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerInfoRequest)
                .WriteString(txt)
                .WriteInteger(num)
                .WriteString(txt2)
                .Compress()
                .Build();

            var reader = new MessageReader(msg);

            reader.Decompress();

            Assert.Equal(txt, reader.ReadString());
            Assert.Equal(num, reader.ReadInteger());
            Assert.Equal(txt2, reader.ReadString());
        }

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress throws when payload is empty")]
        public void Compress_Throws_When_Payload_Is_Empty()
        {
            var ex = Record.Exception(() => new MessageBuilder()
                .Code(MessageCode.PeerInfoRequest)
                .Compress());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress throws when already compressed")]
        public void Compress_Throws_When_Already_Compressed()
        {
            var ex = Record.Exception(() => new MessageBuilder()
                .Code(MessageCode.PeerInfoRequest)
                .WriteString("foo")
                .Compress()
                .Compress());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("already", ex.Message);
        }

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress throws MessageCompressionException on compression exception")]
        public void Compress_Throws_MessageCompressionException_On_Zlib_Exception()
        {
            var builder = new MessageBuilder();
            var ex = Record.Exception(() => builder.InvokeMethod("Compress", BindingFlags.NonPublic | BindingFlags.Instance, null, null));

            Assert.NotNull(ex);
            Assert.NotNull(ex.InnerException);
            Assert.NotNull(ex.InnerException.InnerException);
            Assert.IsType<MessageCompressionException>(ex.InnerException.InnerException);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes throws InvalidOperationException when payload has been compressed")]
        public void WriteBytes_Throws_When_Payload_Has_Been_Compressed()
        {
            var builder = new MessageBuilder();
            builder.Code(MessageCode.PeerBrowseRequest);
            builder.WriteString("foo");
            builder.Compress();

            var ex = Record.Exception(() => builder.WriteBytes(new byte[] { 0x0 }));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes throws ArgumentNullException given null byte array")]
        public void WriteBytes_Throws_Given_Null_Byte_Array()
        {
            var builder = new MessageBuilder();

            var ex = Record.Exception(() => builder.WriteBytes(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes writes given bytes")]
        public void WriteBytes_Writes_Given_Bytes()
        {
            var bytes = new byte[] { 0x0, 0x1, 0x2 };

            var builder = new MessageBuilder();
            builder.WriteBytes(bytes);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Equal(3, payload.Count);
            Assert.Equal(bytes.ToList(), payload);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes appends given bytes")]
        public void WriteBytes_Appends_Given_Bytes()
        {
            var bytes = new byte[] { 0x0, 0x1, 0x2 };

            var builder = new MessageBuilder();
            builder.WriteBytes(bytes);

            var payload1 = builder.GetProperty<List<byte>>("PayloadBytes").ToList();

            builder.WriteBytes(bytes);

            var payload2 = builder.GetProperty<List<byte>>("PayloadBytes").ToList();
            var bytes2 = bytes.ToList();
            bytes2.AddRange(bytes);

            Assert.Equal(3, payload1.Count);
            Assert.Equal(bytes.ToList(), payload1);
            Assert.Equal(6, payload2.Count);
            Assert.Equal(bytes2, payload2);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteByte writes given byte")]
        public void WriteByte_Writes_Given_Byte()
        {
            var data = (byte)0x0;

            var builder = new MessageBuilder();
            builder.WriteByte(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Single(payload);
            Assert.Equal(new[] { data }.ToList(), payload);
        }


        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteInteger writes given int")]
        public void WriteInteger_Writes_Given_Int()
        {
            var data = new Random().Next();

            var builder = new MessageBuilder();
            builder.WriteInteger(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Equal(4, payload.Count);
            Assert.Equal(BitConverter.GetBytes(data).ToList(), payload);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteLong writes given long")]
        public void WriteLong_Writes_Given_Long()
        {
            var data = (long)(new Random().Next());

            var builder = new MessageBuilder();
            builder.WriteLong(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Equal(8, payload.Count);
            Assert.Equal(BitConverter.GetBytes(data).ToList(), payload);
        }

        [Trait("Category", "WriteBytes")]
        [Fact(DisplayName = "WriteString writes given string prepended with length")]
        public void WriteString_Writes_Given_String_Prepended_With_Length()
        {
            var data = Guid.NewGuid().ToString();

            var builder = new MessageBuilder();
            builder.WriteString(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            var expectedBytes = new List<byte>();
            expectedBytes.AddRange(BitConverter.GetBytes(data.Length));
            expectedBytes.AddRange(Encoding.ASCII.GetBytes(data));

            Assert.Equal(expectedBytes.Count, payload.Count);
            Assert.Equal(expectedBytes, payload);
        }
    }
}
