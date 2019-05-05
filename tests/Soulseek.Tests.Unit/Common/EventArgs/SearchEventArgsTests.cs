// <copyright file="SearchEventArgsTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class SearchEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Class", "SearchEventArgs")]
        [Theory(DisplayName = "Instantiates with valid Search"), AutoData]
        public void SearchEventArgs_Instantiates_With_Valid_Search(string searchText, int token, SearchOptions options)
        {
            var search = new Search(searchText, token, options);

            var e = new SearchEventArgs(search);

            Assert.Equal(searchText, e.SearchText);
            Assert.Equal(token, e.Token);
            Assert.Equal(options, e.Options);
            Assert.Equal(search.State, e.State);
            Assert.Empty(e.Responses);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Class", "SearchResponseReceivedEventArgs")]
        [Fact(DisplayName = "Instantiates with valid Search and SearchResponse")]
        public void SearchResponseReceivedEventArgs_Instantiates_With_Valid_Search_And_SearchResponse()
        {
            var searchText = Guid.NewGuid().ToString();
            var token = new Random().Next();

            var search = new Search(searchText, token, new SearchOptions());

            var response = new SearchResponse("foo", 1, 1, 1, 1, 1);

            var e = new SearchResponseReceivedEventArgs(response, search);

            Assert.Equal(searchText, e.SearchText);
            Assert.Equal(token, e.Token);
            Assert.Equal(response, e.Response);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Class", "SearchStateChangedEventArgs")]
        [Fact(DisplayName = "Instantiates with valid Search")]
        public void SearchStateChangedEventArgs_Instantiates_With_Valid_Search()
        {
            var searchText = Guid.NewGuid().ToString();
            var token = new Random().Next();

            var search = new Search(searchText, token, new SearchOptions());
            search.SetProperty("State", SearchStates.Completed);

            var e = new SearchStateChangedEventArgs(SearchStates.None, search);

            Assert.Equal(searchText, e.SearchText);
            Assert.Equal(token, e.Token);
            Assert.Equal(SearchStates.None, e.PreviousState);
            Assert.Equal(SearchStates.Completed, e.State);
        }
    }
}
