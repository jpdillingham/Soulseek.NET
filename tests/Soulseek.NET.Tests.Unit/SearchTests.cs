﻿// <copyright file="SearchTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit
{
    using System;
    using AutoFixture.Xunit2;
    using Soulseek.NET.Messaging.Messages;
    using Xunit;

    public class SearchTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with expected data"), AutoData]
        public void Instantiates_With_Expected_Data(string searchText, int token, SearchOptions options)
        {
            var s = new Search(searchText, token, options);

            Assert.Equal(searchText, s.SearchText);
            Assert.Equal(token, s.Token);
            Assert.Equal(options, s.Options);

            Assert.Equal(SearchStates.None, s.State);
            Assert.Empty(s.Responses);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given delegates"), AutoData]
        internal void Instantiates_With_Given_Delegates(
            string searchText,
            int token,
            Action<Search, SearchResponse> responseHandler,
            Action<Search, SearchStates> completeHandler,
            SearchOptions options)
        {
            var s = new Search(searchText, token, responseHandler, completeHandler, options);

            Assert.Equal(responseHandler, s.GetProperty<Action<Search, SearchResponse>>("ResponseHandler"));
            Assert.Equal(completeHandler, s.GetProperty<Action<Search, SearchStates>>("CompleteHandler"));
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var s = new Search("foo", 42);

            var ex = Record.Exception(() => s.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "Complete")]
        [Fact(DisplayName = "Complete sets state")]
        public void Complete_Sets_State()
        {
            var s = new Search("foo", 42);

            s.Complete(SearchStates.Cancelled);

            Assert.True(s.State.HasFlag(SearchStates.Completed));
            Assert.True(s.State.HasFlag(SearchStates.Cancelled));
        }

        [Trait("Category", "Complete")]
        [Fact(DisplayName = "Complete sets state")]
        public void Complete_Invokes_CompleteHandler()
        {
            bool invoked = false;
            var s = new Search("foo", 42, (search, res) => { }, (search, state) => { invoked = true; });

            s.Complete(SearchStates.Cancelled);

            Assert.True(invoked);
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Fact(DisplayName = "Response filter returns true when FilterResponses option is false")]
        public void Response_Filter_Returns_True_When_FilterResponses_Option_Is_False()
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: false));
            var response = new SearchResponseSlim("u", 1, 1, 1, 1, 1, null);

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.True(filter);
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Theory(DisplayName = "Response filter respects MinimumResponseFileCount option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void Response_Filter_Respects_MinimumResponseFileCount_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: option));
            var response = new SearchResponseSlim("u", 1, actual, 1, 1, 1, null);

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Theory(DisplayName = "Response filter respects MinimumPeerFreeUploadSlots option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void Response_Filter_Respects_MinimumPeerFreeUploadSlots_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: true, minimumPeerFreeUploadSlots: option));
            var response = new SearchResponseSlim("u", 1, 1, actual, 1, 1, null);

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Theory(DisplayName = "Response filter respects MinimumPeerUploadSpeed option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void Response_Filter_Respects_MinimumPeerUploadSpeed_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: true, minimumPeerUploadSpeed: option));
            var response = new SearchResponseSlim("u", 1, 1, 1, actual, 1, null);

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Theory(DisplayName = "Response filter respects MaximumPeerQueueLength option")]
        [InlineData(0, 1, true)]
        [InlineData(1, 1, false)]
        [InlineData(1, 0, false)]
        public void Response_Filter_Respects_MaximumPeerQueueLength_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: true, maximumPeerQueueLength: option));
            var response = new SearchResponseSlim("u", 1, 1, 1, 1, actual, null);

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);
        }
    }
}
