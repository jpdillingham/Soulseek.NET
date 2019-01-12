// <copyright file="PrivilegedUserListTests.cs" company="JP Dillingham">
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
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Responses;
    using System.Linq;
    using Xunit;

    public class PrivilegedUserListTests
    {
        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .Build();

            var ex = Record.Exception(() => PrivilegedUserList.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.ServerPrivilegedUsers)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => PrivilegedUserList.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data")]
        public void Parse_Returns_Expected_Data()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.ServerPrivilegedUsers)
                .WriteInteger(4)
                .WriteString("larry")
                .WriteString("moe")
                .WriteString("curly")
                .WriteString("shemp")
                .Build();

            var response = PrivilegedUserList.Parse(msg).ToList();

            Assert.Equal(4, response.Count);
            Assert.Contains("larry", response);
            Assert.Contains("moe", response);
            Assert.Contains("curly", response);
            Assert.Contains("shemp", response);
        }
    }
}
