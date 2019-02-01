// <copyright file="SearchResponseSlimTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Zlib;
    using Xunit;

    public class SearchResponseSlimTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Given_Data(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var messageReader = new MessageReader(new byte[8]);

            var r = new SearchResponseSlim(username, token, fileCount, freeUploadSlots, uploadSpeed, queueLength, messageReader);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(fileCount, r.FileCount);
            Assert.Equal(freeUploadSlots, r.FreeUploadSlots);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);
            Assert.Equal(messageReader, r.MessageReader);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageException on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .Build();

            var ex = Record.Exception(() => SearchResponseSlim.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageCompressionException on uncompressed payload")]
        public void Parse_Throws_MessageCompressionException_On_Uncompressed_Payload()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2, 0x3 })
                .Build();

            var ex = Record.Exception(() => SearchResponseSlim.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageCompressionException>(ex);
            Assert.IsType<ZStreamException>(ex.InnerException);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString("foo")
                .Compress()
                .Build();

            var ex = Record.Exception(() => SearchResponseSlim.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponseSlim.Parse(msg);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(1, r.FileCount);
            Assert.Equal(freeUploadSlots, r.FreeUploadSlots);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);

            // assure the messagereader has been rewound to the start of the file list
            // position is equal to 4 byte username length + username + 4 byte token + 4 byte file count
            Assert.Equal(4 + username.Length + 4 + 4, r.MessageReader.Position);
        }
    }
}
