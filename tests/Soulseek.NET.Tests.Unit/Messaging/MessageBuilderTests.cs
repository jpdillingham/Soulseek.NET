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
    using Soulseek.NET.Messaging;
    using System;
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

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress produces valid data")]
        public void Compression_Works()
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
    }
}
