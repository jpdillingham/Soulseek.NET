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
    using System;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchScopeTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates Default")]
        public void Instantiates_Default()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope());

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.Default, s.Type);
            Assert.Empty(s.Subjects);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws on Default when subjects is not empty"), AutoData]
        public void Throws_On_Default_When_Subjects_Is_Not_Empty(string[] subjects)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Default, subjects));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("accepts no subjects"));
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates Room"), AutoData]
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
        [Fact(DisplayName = "Throws on Room when subjects is empty")]
        public void Throws_On_Room_When_Subjects_Is_Empty()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on Room when subjects is one null string")]
        public void Throws_On_Room_When_Subjects_Is_One_Null_String()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, new string[] { null }));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on Room when subjects is one empty string")]
        public void Throws_On_Room_When_Subjects_Is_One_Empty_String()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, new string[] { string.Empty }));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on Room when subjects is more than one")]
        public void Throws_On_Room_When_Subjects_Is_More_Than_One()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, new[] { "one", "two" }));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
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
