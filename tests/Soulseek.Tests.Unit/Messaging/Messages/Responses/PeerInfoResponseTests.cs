// <copyright file="PeerInfoResponseTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class PeerInfoResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string description, bool hasPicture, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            UserInfoResponse response = null;

            var ex = Record.Exception(() => response = new UserInfoResponse(description, hasPicture, picture, uploadSlots, queueLength, hasFreeSlot));

            Assert.Null(ex);

            Assert.Equal(description, response.Description);
            Assert.Equal(hasPicture, response.HasPicture);
            Assert.Equal(picture, response.Picture);
            Assert.Equal(uploadSlots, response.UploadSlots);
            Assert.Equal(queueLength, response.QueueLength);
            Assert.Equal(hasFreeSlot, response.HasFreeUploadSlot);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => UserInfoResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString("foo")
                .Build();

            var ex = Record.Exception(() => UserInfoResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data with picture"), AutoData]
        public void Parse_Returns_Expected_Data_With_Picture(string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(description)
                .WriteByte(1)
                .WriteInteger(picture.Length)
                .WriteBytes(picture)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte((byte)(hasFreeSlot ? 1 : 0))
                .Build();

            var response = UserInfoResponse.Parse(msg);

            Assert.Equal(description, response.Description);
            Assert.True(response.HasPicture);
            Assert.Equal(picture, response.Picture);
            Assert.Equal(uploadSlots, response.UploadSlots);
            Assert.Equal(queueLength, response.QueueLength);
            Assert.Equal(hasFreeSlot, response.HasFreeUploadSlot);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data without picture"), AutoData]
        public void Parse_Returns_Expected_Data_Without_Picture(string description, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(description)
                .WriteByte(0)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte((byte)(hasFreeSlot ? 1 : 0))
                .Build();

            var response = UserInfoResponse.Parse(msg);

            Assert.Equal(description, response.Description);
            Assert.False(response.HasPicture);
            Assert.Equal(uploadSlots, response.UploadSlots);
            Assert.Equal(queueLength, response.QueueLength);
            Assert.Equal(hasFreeSlot, response.HasFreeUploadSlot);
        }
    }
}
