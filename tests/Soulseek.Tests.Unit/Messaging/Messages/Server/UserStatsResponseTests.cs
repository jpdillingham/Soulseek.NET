// <copyright file="UserStatsResponseTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class UserStatsResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var stats = new UserStats(username, averageSpeed, uploadCount, fileCount, directoryCount);
            var r = new UserStatsResponse(username, stats);

            Assert.Equal(username, r.Username);
            Assert.Equal(stats, r.UserStats);
            Assert.Equal(stats.Username, username);
            Assert.Equal(stats.AverageSpeed, averageSpeed);
            Assert.Equal(stats.UploadCount, uploadCount);
            Assert.Equal(stats.FileCount, fileCount);
            Assert.Equal(stats.DirectoryCount, directoryCount);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => UserStatsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .Build();

            var ex = Record.Exception(() => UserStatsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString(username)
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .Build();

            var r = UserStatsResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.Equal(averageSpeed, r.UserStats.AverageSpeed);
            Assert.Equal(uploadCount, r.UserStats.UploadCount);
            Assert.Equal(fileCount, r.UserStats.FileCount);
            Assert.Equal(directoryCount, r.UserStats.DirectoryCount);
        }
    }
}
