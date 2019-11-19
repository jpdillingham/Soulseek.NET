// <copyright file="SearchEventArgs.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Options;

    /// <summary>
    ///     Generic event arguments for search events.
    /// </summary>
    public class SearchEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchEventArgs"/> class.
        /// </summary>
        /// <param name="searchText">The text for which the search is being conducted.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="state">The current state of the search.</param>
        /// <param name="responses">The current list of search responses.</param>
        /// <param name="options">The search options.</param>
        public SearchEventArgs(string searchText, int token, SearchStates state, IReadOnlyCollection<SearchResponse> responses, SearchOptions options)
        {
            SearchText = searchText;
            Token = token;
            State = state;
            Responses = responses.ToList();
            Options = options;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchEventArgs"/> class.
        /// </summary>
        /// <param name="search">The search instance with which to initialize data.</param>
        internal SearchEventArgs(Search search)
            : this(search.SearchText, search.Token, search.State, search.Responses, search.Options)
        {
        }

        /// <summary>
        ///     Gets the options for the search.
        /// </summary>
        public SearchOptions Options { get; }

        /// <summary>
        ///     Gets the collection of responses received from peers.
        /// </summary>
        public IReadOnlyCollection<SearchResponse> Responses { get; }

        /// <summary>
        ///     Gets the text for which to search.
        /// </summary>
        public string SearchText { get; }

        /// <summary>
        ///     Gets the state of the search.
        /// </summary>
        public SearchStates State { get; }

        /// <summary>
        ///     Gets the unique identifier for the search.
        /// </summary>
        public int Token { get; }
    }

    /// <summary>
    ///     Event arguments for events raised when a search response is received.
    /// </summary>
    public sealed class SearchResponseReceivedEventArgs : SearchEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponseReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="response">The received response.</param>
        /// <param name="searchText">The text for which the search is being conducted.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="state">The current state of the search.</param>
        /// <param name="responses">The current list of search responses.</param>
        /// <param name="options">The search options.</param>
        public SearchResponseReceivedEventArgs(SearchResponse response, string searchText, int token, SearchStates state, IReadOnlyCollection<SearchResponse> responses, SearchOptions options)
            : base(searchText, token, state, responses, options)
        {
            Response = response;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponseReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="response">The search response which raised the event.</param>
        /// <param name="search">The search instance with which to initialize data.</param>
        internal SearchResponseReceivedEventArgs(SearchResponse response, Search search)
            : base(search)
        {
            Response = response;
        }

        /// <summary>
        ///     Gets the search response which raised the event.
        /// </summary>
        public SearchResponse Response { get; }
    }

    /// <summary>
    ///     Event arguments for events raised by a change in search state.
    /// </summary>
    public sealed class SearchStateChangedEventArgs : SearchEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the search.</param>
        /// <param name="searchText">The text for which the search is being conducted.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="state">The current state of the search.</param>
        /// <param name="responses">The current list of search responses.</param>
        /// <param name="options">The search options.</param>
        public SearchStateChangedEventArgs(SearchStates previousState, string searchText, int token, SearchStates state, IReadOnlyCollection<SearchResponse> responses, SearchOptions options)
            : base(searchText, token, state, responses, options)
        {
            PreviousState = previousState;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the search.</param>
        /// <param name="search">The search instance with which to initialize data.</param>
        internal SearchStateChangedEventArgs(SearchStates previousState, Search search)
            : base(search)
        {
            PreviousState = previousState;
        }

        /// <summary>
        ///     Gets the previous search state.
        /// </summary>
        public SearchStates PreviousState { get; }
    }
}