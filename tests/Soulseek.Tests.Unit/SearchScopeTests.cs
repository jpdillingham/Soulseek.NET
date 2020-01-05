// <copyright file="SearchScopeTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchScopeTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given data")]
        public void Instantiates_With_Default()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope());

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.Default, s.Type);
            Assert.Empty(s.Subjects);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates room"), AutoData]
        public void Instantiates_Room(string room)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, room));

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.Room, s.Type);
            Assert.Single(s.Subjects);
            Assert.Equal(room, s.Subjects.First());
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates user"), AutoData]
        public void Instantiates_User(string user)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.User, user));

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.User, s.Type);
            Assert.Single(s.Subjects);
            Assert.Equal(user, s.Subjects.First());
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates users"), AutoData]
        public void Instantiates_Users(string[] users)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.User, users));

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.User, s.Type);
            Assert.Equal(users.Length, s.Subjects.Count());
            Assert.Equal(users, s.Subjects);
        }
    }
}
