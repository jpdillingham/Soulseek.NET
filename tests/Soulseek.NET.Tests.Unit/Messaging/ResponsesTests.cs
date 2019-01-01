// <copyright file="ResponsesTests.cs" company="JP Dillingham">
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

    public class ResponsesTests
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
    }
}
