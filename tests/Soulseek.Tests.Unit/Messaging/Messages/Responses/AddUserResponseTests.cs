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
    using System;
    using System.Net;
    using AutoFixture.Xunit2;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class AddUserResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string username, bool exists, UserStatus? status, int? averageSpeed, long? downloadCount, int? fileCount, int? directoryCount, string countryCode)
        {
            var r = new AddUserResponse(username, exists, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode);

            Assert.Equal(username, r.Username);
            Assert.Equal(exists, r.Exists);
            Assert.Equal(averageSpeed, r.AverageSpeed);
            Assert.Equal(downloadCount, r.DownloadCount);
            Assert.Equal(fileCount, r.FileCount);
            Assert.Equal(directoryCount, r.DirectoryCount);
            Assert.Equal(countryCode, r.CountryCode);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseRequest)
                .Build();

            var ex = Record.Exception(() => AddUserResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.ServerAddUser)
                .Build();

            var ex = Record.Exception(() => AddUserResponse.Parse(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when user exists"), AutoData]
        public void Parse_Returns_Expected_Data_When_User_Exists(string username, UserStatus status, int averageSpeed, long downloadCount, int fileCount, int directoryCount, string countryCode)
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.ServerAddUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)status)
                .WriteInteger(averageSpeed)
                .WriteLong(downloadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .WriteString(countryCode)
                .Build();

            var r = AddUserResponse.Parse(msg);

            Assert.Equal(username, r.Username);
            Assert.True(r.Exists);
            Assert.Equal(status, r.Status);
            Assert.Equal(averageSpeed, r.AverageSpeed);
            Assert.Equal(downloadCount, r.DownloadCount);
            Assert.Equal(fileCount, r.FileCount);
            Assert.Equal(directoryCount, r.DirectoryCount);
            Assert.Equal(countryCode, r.CountryCode);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when user does not exist"), AutoData]
        public void Parse_Returns_Expected_Data_When_User_Does_Not_Exist(string username)
        {
            var msg = new MessageBuilder()
                .Code(MessageCode.ServerAddUser)
                .WriteString(username)
                .WriteByte(0) // exists = false
                .Build();

            var r = AddUserResponse.Parse(msg);

            Assert.Equal(username, r.Username);
            Assert.False(r.Exists);
            Assert.Null(r.Status);
            Assert.Null(r.AverageSpeed);
            Assert.Null(r.DownloadCount);
            Assert.Null(r.FileCount);
            Assert.Null(r.DirectoryCount);
            Assert.Null(r.CountryCode);
        }
    }
}
