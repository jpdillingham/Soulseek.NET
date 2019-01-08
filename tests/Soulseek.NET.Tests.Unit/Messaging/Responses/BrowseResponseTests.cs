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

namespace Soulseek.NET.Tests.Unit.Messaging.Responses
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
        [Fact(DisplayName = "Instantiates with given data")]
        public void Instantiates_With_Given_Data()
        {
            var num = new Random().Next();
            var a = new BrowseResponse(num);

            Assert.Equal(num, a.DirectoryCount);
            Assert.Empty(a.Directories);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Instantiates with the given directory list")]
        public void Instantiates_With_The_Given_Directory_List()
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
        [Fact(DisplayName = "Throws MessageException on code mismatch")]
        public void Throws_On_Code_Mismatch()
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
        [Fact(DisplayName = "Throws MessageCompressionException on uncompressed payload")]
        public void Throws_On_Uncompressed_Payload()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2, 0x3 })
                .Build();

            var ex = Record.Exception(() => BrowseResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageCompressionException>(ex);
            Assert.IsType<ZStreamException>(ex.InnerException);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Returns empty response given empty message")]
        public void Returns_Empty_Response_Given_Empty_Message()
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
        [Fact(DisplayName = "Handles empty directory")]
        public void Handles_Empty_Directory()
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
        [Fact(DisplayName = "Handles files with no attributes")]
        public void Handles_Files_With_No_Attributes()
        {
            var name = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(name) // first directory name
                .WriteInteger(1) // first directory file count
                .WriteByte(0x0) // file code
                .WriteString("foo") // name
                .WriteLong(12) // size 
                .WriteString("bar") // extension
                .WriteInteger(0) // attribute count
                .Compress()
                .Build();

            BrowseResponse r = default(BrowseResponse);
            var ex = Record.Exception(() => r = BrowseResponse.Parse(msg));

            Assert.Equal(1, r.DirectoryCount);
            Assert.Single(r.Directories);

            var d = r.Directories.ToList();

            Assert.Equal(name, d[0].Directoryname);
            Assert.Equal(1, d[0].FileCount);
            Assert.Single(d[0].Files);

            var f = d[0].Files.ToList();

            Assert.Equal(0x0, f[0].Code);
            Assert.Equal("foo", f[0].Filename);
            Assert.Equal(12, f[0].Size);
            Assert.Equal("bar", f[0].Extension);
            Assert.Equal(0, f[0].AttributeCount);
            Assert.Empty(f[0].Attributes);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Handles a complete response")]
        public void Handles_A_Complete_Response()
        {
            var dirs = new List<Directory>();

            for (int i = 0; i < 5; i++)
            {
                dirs.Add(GetRandomDirectory(i));
            }

            var builder = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteInteger(dirs.Count);

            foreach (var dir in dirs)
            {
                BuildDirectory(builder, dir);
            }

            var msg = builder
                .Compress()
                .Build();

            BrowseResponse r = default(BrowseResponse);
            var ex = Record.Exception(() => r = BrowseResponse.Parse(msg));

            Assert.Equal(dirs.Count, r.DirectoryCount);
            Assert.Equal(dirs.Count, r.Directories.Count());

            var msgDirs = r.Directories.ToList();

            for (int i = 0; i < msgDirs.Count; i++)
            {
                Assert.Equal(dirs[i].Directoryname, msgDirs[i].Directoryname);
                Assert.Equal(dirs[i].FileCount, msgDirs[i].FileCount);

                var files = dirs[i].Files.ToList();
                var msgFiles = msgDirs[i].Files.ToList();

                for (int j = 0; j < msgDirs[i].FileCount; j++)
                {
                    Assert.Equal(files[j].Code, msgFiles[j].Code);
                    Assert.Equal(files[j].Filename, msgFiles[j].Filename);
                    Assert.Equal(files[j].Size, msgFiles[j].Size);
                    Assert.Equal(files[j].Extension, msgFiles[j].Extension);
                    Assert.Equal(files[j].AttributeCount, msgFiles[j].AttributeCount);

                    var attr = files[j].Attributes.ToList();
                    var msgAttr = files[j].Attributes.ToList();

                    for (int k = 0; k < msgFiles[j].AttributeCount; k++)
                    {
                        Assert.Equal(attr[k].Type, msgAttr[k].Type);
                        Assert.Equal(attr[k].Value, msgAttr[k].Value);
                    }
                }
            }
        }

        private Random Random { get; } = new Random();

        private MessageBuilder BuildDirectory(MessageBuilder builder, Directory dir)
        {
            builder
                .WriteString(dir.Directoryname)
                .WriteInteger(dir.FileCount);

            foreach (var file in dir.Files)
            {
                builder
                    .WriteByte((byte)file.Code)
                    .WriteString(file.Filename)
                    .WriteLong(file.Size)
                    .WriteString(file.Extension)
                    .WriteInteger(file.AttributeCount);

                foreach (var attribute in file.Attributes)
                {
                    builder
                        .WriteInteger((int)attribute.Type)
                        .WriteInteger(attribute.Value);
                }
            }

            return builder;
        }

        private FileAttribute GetRandomFileAttribute()
        {
            return new FileAttribute(
                type: (FileAttributeType)Random.Next(6),
                value: Random.Next());
        }

        private File GetRandomFile(int attributeCount)
        {
            var attributeList = new List<FileAttribute>();

            for (int i = 0; i < attributeCount; i++)
            {
                attributeList.Add(GetRandomFileAttribute());
            }

            return new File(
                code: Random.Next(2),
                filename: Guid.NewGuid().ToString(),
                size: Random.Next(),
                extension: Guid.NewGuid().ToString(),
                attributeCount: attributeCount,
                attributeList: attributeList);
        }

        private Directory GetRandomDirectory(int fileCount)
        {
            var fileList = new List<File>();

            for (int i = 0; i < fileCount; i++)
            {
                fileList.Add(GetRandomFile(Random.Next(5)));
            }

            return new Directory(
                directoryname: Guid.NewGuid().ToString(),
                fileCount: fileCount,
                fileList: fileList);
        }
    }
}
