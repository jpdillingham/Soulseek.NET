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
    }
}
