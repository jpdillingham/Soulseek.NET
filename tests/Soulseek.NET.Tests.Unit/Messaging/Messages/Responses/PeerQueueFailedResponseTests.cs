// <copyright file="PeerQueueFailedResponseTests.cs" company="JP Dillingham">
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
    using System;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Xunit;

    public class PeerQueueFailedResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with the given data")]
        public void Instantiates_With_The_Given_Data()
        {
            var file = Guid.NewGuid().ToString();
            var reason = Guid.NewGuid().ToString();

            PeerQueueFailedResponse response = null;

            var ex = Record.Exception(() => response = new PeerQueueFailedResponse(file, reason));

            Assert.Null(ex);

            Assert.Equal(file, response.Filename);
            Assert.Equal(reason, response.Message);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .Build();

            var ex = Record.Exception(() => PeerQueueFailedResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerQueueFailed)
                .Build();

            var ex = Record.Exception(() => PeerQueueFailedResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data")]
        public void Parse_Returns_Expected_Data()
        {
            var file = Guid.NewGuid().ToString();
            var reason = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerQueueFailed)
                .WriteString(file)
                .WriteString(reason)
                .Build();

            var response = PeerQueueFailedResponse.Parse(msg);

            Assert.Equal(file, response.Filename);
            Assert.Equal(reason, response.Message);
        }
    }
}
