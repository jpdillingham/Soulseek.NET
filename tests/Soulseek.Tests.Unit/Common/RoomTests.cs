// <copyright file="RoomTests.cs" company="JP Dillingham">
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
    using System;
    using Xunit;

    public class RoomTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName ="Instantiates with the given data")]
        public void Instantiates_With_The_Given_Data()
        {
            var name = Guid.NewGuid().ToString();
            var num = new Random().Next();

            var room = default(Room);

            var ex = Record.Exception(() => room = new Room(name, num));

            Assert.Null(ex);

            Assert.Equal(name, room.Name);
            Assert.Equal(num, room.UserCount);
        }
    }
}
