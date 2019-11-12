// <copyright file="AddUserResponseTests.cs" company="JP Dillingham">
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

    public class AddUserResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string username, bool exists, User user)
        {
            var r = new AddUserResponse(username, exists, user);

            Assert.Equal(username, r.Username);
            Assert.Equal(exists, r.Exists);
            Assert.Equal(user, r.User);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => AddUserResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddUser)
                .Build();

            var ex = Record.Exception(() => AddUserResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when user exists"), AutoData]
        public void Parse_Returns_Expected_Data_When_User_Exists(string username, User user)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)user.Status)
                .WriteInteger(user.AverageSpeed)
                .WriteLong(user.DownloadCount)
                .WriteInteger(user.FileCount)
                .WriteInteger(user.DirectoryCount)
                .WriteString(user.CountryCode)
                .Build();

            var r = AddUserResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.True(r.Exists);
            Assert.Equal(user.Status, r.User.Status);
            Assert.Equal(user.AverageSpeed, r.User.AverageSpeed);
            Assert.Equal(user.DownloadCount, r.User.DownloadCount);
            Assert.Equal(user.FileCount, r.User.FileCount);
            Assert.Equal(user.DirectoryCount, r.User.DirectoryCount);
            Assert.Equal(user.CountryCode, r.User.CountryCode);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when user does not exist"), AutoData]
        public void Parse_Returns_Expected_Data_When_User_Does_Not_Exist(string username)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddUser)
                .WriteString(username)
                .WriteByte(0) // exists = false
                .Build();

            var r = AddUserResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.False(r.Exists);
            Assert.Null(r.User);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse does not throw if CountryCode is missing"), AutoData]
        public void Parse_Does_Not_Throw_If_CountryCode_Is_Missing(string username, User user)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)user.Status)
                .WriteInteger(user.AverageSpeed)
                .WriteLong(user.DownloadCount)
                .WriteInteger(user.FileCount)
                .WriteInteger(user.DirectoryCount)
                .Build();

            var r = AddUserResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.True(r.Exists);
            Assert.Equal(user.Status, r.User.Status);
            Assert.Equal(user.AverageSpeed, r.User.AverageSpeed);
            Assert.Equal(user.DownloadCount, r.User.DownloadCount);
            Assert.Equal(user.FileCount, r.User.FileCount);
            Assert.Equal(user.DirectoryCount, r.User.DirectoryCount);
            Assert.Null(r.User.CountryCode);
        }
    }
}
