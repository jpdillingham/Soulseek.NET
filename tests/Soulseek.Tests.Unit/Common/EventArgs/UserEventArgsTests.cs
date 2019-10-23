// <copyright file="UserEventArgsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using AutoFixture.Xunit2;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class UserEventArgsTests
    {
        [Trait("Category", "UserStatusChangedEventArgs Instantiation")]
        [Theory(DisplayName = "UserStatusChangedEventArgs Instantiates with the given data"), AutoData]
        public void UserStatusChangedEventArgs_Instantiates_With_The_Given_Data(string username, UserStatus status, bool privileged)
        {
            var e = new UserStatusChangedEventArgs(new UserStatusResponse(username, status, privileged));

            Assert.Equal(username, e.Username);
            Assert.Equal(status, e.Status);
            Assert.Equal(privileged, e.Privileged);
        }
    }
}
