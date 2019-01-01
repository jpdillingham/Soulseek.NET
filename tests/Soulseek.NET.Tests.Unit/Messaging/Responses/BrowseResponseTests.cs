// <copyright file="BrowseResponseTests.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Zlib;
    using Xunit;

    public class BrowseResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "BrowseResponse instantiates properly")]
        public void BrowseResponse_Instantiates_Properly()
        {
            var num = new Random().Next();
            var a = new BrowseResponse(num);

            Assert.Equal(num, a.DirectoryCount);
            Assert.Empty(a.Directories);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "BrowseResponse instantiates with the given directory list")]
        public void BrowseResponse_Instantiates_With_The_Given_Directory_List()
        {
            var num = new Random().Next();

            var dir = new Directory("foo", 1);
            var list = new List<Directory>(new[] { dir });
            
            var a = new BrowseResponse(num, list);

            Assert.Equal(num, a.DirectoryCount);
            Assert.Single(a.Directories);
            Assert.Equal(dir, a.Directories.ToList()[0]);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "BrowseResponse throws on code mismatch")]
        public void BrowseResponse_Throws_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerDownloadResponse)
                .Build();

            var ex = Record.Exception(() => BrowseResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "BrowseResponse throws on uncompressed payload")]
        public void BrowseResponse_Throws_On_Uncompressed_Payload()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2, 0x3 })
                .Build();

            var ex = Record.Exception(() => BrowseResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
            Assert.IsType<ZStreamException>(ex.InnerException);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "BrowseResponse returns empty response given empty message")]
        public void BrowseResponse_Returns_Empty_Response_Given_Empty_Message()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteInteger(0)
                .Compress()
                .Build();

            BrowseResponse r = default(BrowseResponse);
            var ex = Record.Exception(() => r = BrowseResponse.Parse(msg));

            Assert.Equal(0, r.DirectoryCount);
            Assert.Empty(r.Directories);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "BrowseResponse handles empty directory")]
        public void BrowseResponse_Handles_Empty_Directory()
        {
            var name = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(name) // first directory name
                .WriteInteger(0) // first directory file count
                .Compress()
                .Build();

            BrowseResponse r = default(BrowseResponse);
            var ex = Record.Exception(() => r = BrowseResponse.Parse(msg));

            Assert.Equal(1, r.DirectoryCount);
            Assert.Single(r.Directories);

            var d = r.Directories.ToList();

            Assert.Equal(name, d[0].Directoryname);
            Assert.Equal(0, d[0].FileCount);
            Assert.Empty(d[0].Files);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "BrowseResponse handles multiple empty directories")]
        public void BrowseResponse_Handles_Multiple_Empty_Directories()
        {
            var name = Guid.NewGuid().ToString();
            var name2 = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteInteger(2) // directory count
                .WriteString(name) // first directory name
                .WriteInteger(0) // first directory file count
                .WriteString(name2) // dir 2 name
                .WriteInteger(0) // dir 2 file count
                .Compress()
                .Build();

            BrowseResponse r = default(BrowseResponse);
            var ex = Record.Exception(() => r = BrowseResponse.Parse(msg));

            Assert.Equal(2, r.DirectoryCount);
            Assert.Equal(2, r.Directories.Count());

            var d = r.Directories.ToList();

            Assert.Equal(name, d[0].Directoryname);
            Assert.Equal(0, d[0].FileCount);
            Assert.Empty(d[0].Files);

            Assert.Equal(name2, d[1].Directoryname);
            Assert.Equal(0, d[1].FileCount);
            Assert.Empty(d[1].Files);
        }
    }
}
