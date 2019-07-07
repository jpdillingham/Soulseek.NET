// <copyright file="SearchResponseTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Zlib;
    using Xunit;

    public class SearchResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Given_Data(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var r = new SearchResponse(username, token, fileCount, freeUploadSlots, uploadSpeed, queueLength);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(fileCount, r.FileCount);
            Assert.Equal(freeUploadSlots, r.FreeUploadSlots);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given response and list, replacing filecount with list length"), AutoData]
        public void Instantiates_With_Given_Response_And_List(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var r1 = new SearchResponse(username, token, fileCount, freeUploadSlots, uploadSpeed, queueLength);

            var r2 = new SearchResponse(r1, new List<File>() { new File(1, "foo", 2, "ext", 0) });

            Assert.Empty(r1.Files);
            Assert.Single(r2.Files);

            Assert.Equal(r1.Username, r2.Username);
            Assert.Equal(r1.Token, r2.Token);
            Assert.Equal(1, r2.FileCount);
            Assert.Equal(r1.FreeUploadSlots, r2.FreeUploadSlots);
            Assert.Equal(r1.UploadSpeed, r2.UploadSpeed);
            Assert.Equal(r1.QueueLength, r2.QueueLength);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageException on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => SearchResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageCompressionException on uncompressed payload")]
        public void Parse_Throws_MessageCompressionException_On_Uncompressed_Payload()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2, 0x3 })
                .Build();

            var ex = Record.Exception(() => SearchResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageCompressionException>(ex);
            Assert.IsType<ZStreamException>(ex.InnerException);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("foo")
                .Compress()
                .Build();

            var ex = Record.Exception(() => SearchResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on file count mismatch")]
        public void Parse_Throws_MessageReadException_On_File_Count_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("foo")
                .WriteInteger(0)
                .WriteInteger(20) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(0x20) // code
                .WriteString("filename2") // filename
                .WriteLong(30) // size
                .WriteString("ext2") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(40) // attribute[0].value
                .WriteByte(0)
                .WriteInteger(0)
                .WriteLong(0)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var ex = Record.Exception(() => SearchResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
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

            var r = SearchResponse.Parse(msg);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(1, r.FileCount);
            Assert.Equal(freeUploadSlots, r.FreeUploadSlots);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);

            Assert.Single(r.Files);

            var file = r.Files.ToList()[0];

            Assert.Equal(0x2, file.Code);
            Assert.Equal("filename", file.Filename);
            Assert.Equal(3, file.Size);
            Assert.Equal("ext", file.Extension);
            Assert.Equal(1, file.AttributeCount);
            Assert.Single(file.Attributes);
            Assert.Equal(FileAttributeType.BitDepth, file.Attributes.ToList()[0].Type);
            Assert.Equal(4, file.Attributes.ToList()[0].Value);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles empty responses"), AutoData]
        public void Parse_Handles_Empty_Responses(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(0) // file count
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponse.Parse(msg);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(0, r.FileCount);
            Assert.Equal(freeUploadSlots, r.FreeUploadSlots);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);

            Assert.Empty(r.Files);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles multiple files"), AutoData]
        public void Parse_Handles_Multiple_Files(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(2) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(0x20) // code
                .WriteString("filename2") // filename
                .WriteLong(30) // size
                .WriteString("ext2") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(40) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponse.Parse(msg);

            Assert.Equal(2, r.Files.Count);

            var file = r.Files.ToList();

            Assert.Equal(0x2, file[0].Code);
            Assert.Equal("filename", file[0].Filename);
            Assert.Equal(3, file[0].Size);
            Assert.Equal("ext", file[0].Extension);
            Assert.Equal(1, file[0].AttributeCount);
            Assert.Single(file[0].Attributes);
            Assert.Equal(FileAttributeType.BitDepth, file[0].Attributes.ToList()[0].Type);
            Assert.Equal(4, file[0].Attributes.ToList()[0].Value);

            Assert.Equal(0x20, file[1].Code);
            Assert.Equal("filename2", file[1].Filename);
            Assert.Equal(30, file[1].Size);
            Assert.Equal("ext2", file[1].Extension);
            Assert.Equal(1, file[1].AttributeCount);
            Assert.Single(file[1].Attributes);
            Assert.Equal(FileAttributeType.BitRate, file[1].Attributes.ToList()[0].Type);
            Assert.Equal(40, file[1].Attributes.ToList()[0].Value);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles empty attributes"), AutoData]
        public void Parse_Handles_Empty_Attributes(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(0) // attribute count
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponse.Parse(msg);

            Assert.Single(r.Files);
            Assert.Empty(r.Files.ToList()[0].Attributes);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles multiple attributes"), AutoData]
        public void Parse_Handles_Multiple_Attributes(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(2) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(5) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponse.Parse(msg);

            Assert.Single(r.Files);

            var file = r.Files.ToList()[0];

            Assert.Equal(FileAttributeType.BitDepth, file.Attributes.ToList()[0].Type);
            Assert.Equal(4, file.Attributes.ToList()[0].Value);

            Assert.Equal(FileAttributeType.BitRate, file.Attributes.ToList()[1].Type);
            Assert.Equal(5, file.Attributes.ToList()[1].Value);
        }
    }
}
