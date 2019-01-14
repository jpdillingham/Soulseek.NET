using Soulseek.NET.Messaging.Responses;
using System;
using Xunit;

namespace Soulseek.NET.Tests.Unit.Common.EventArgs
{
    public class SearchEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Class", "SearchEventArgs")]
        [Fact(DisplayName = "Instantiates with valid Search")]
        public void SearchEventArgs_Instantiates_With_Valid_Search()
        {
            var searchText = Guid.NewGuid().ToString();
            var token = new Random().Next();

            var search = new Search(searchText, token, new SearchOptions());

            var e = new SearchEventArgs(search);

            Assert.Equal(searchText, e.SearchText);
            Assert.Equal(token, e.Token);
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

            var e = new SearchResponseReceivedEventArgs(search, response);

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

            var e = new SearchStateChangedEventArgs(search);

            Assert.Equal(searchText, e.SearchText);
            Assert.Equal(token, e.Token);
            Assert.Equal(SearchStates.Completed, e.State);
        }
    }
}
