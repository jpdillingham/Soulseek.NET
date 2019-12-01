// <copyright file="Search.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A single file search.
    /// </summary>
    /// <remarks>
    ///     This DTO wouldn't be necessary if Json.NET didn't serialize internal properties by default.
    /// </remarks>
    public class Search
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="state">The state of the search.</param>
        /// <param name="responses">The collection of responses received from peers.</param>
        /// <param name="options">The options for the search.</param>
        public Search(string searchText, int token, SearchStates state, IEnumerable<SearchResponse> responses = null, SearchOptions options = null)
        {
            SearchText = searchText;
            Token = token;
            State = state;

            ResponseList = responses ?? Enumerable.Empty<SearchResponse>();

            if (options == null)
            {
                Options = new SearchOptions();
            }
            else
            {
                // create a new instance of options so we can strip out delegates. these don't serialize well and they shouldn't be
                // invoked by any code working with this DTO.
                Options = new SearchOptions(
                    options.SearchTimeout,
                    options.ResponseLimit,
                    options.FilterResponses,
                    options.MinimumResponseFileCount,
                    options.MinimumPeerFreeUploadSlots,
                    options.MaximumPeerQueueLength,
                    options.MinimumPeerUploadSpeed,
                    options.FileLimit,
                    responseFilter: null,
                    fileFilter: null,
                    stateChanged: null,
                    responseReceived: null);
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class.
        /// </summary>
        /// <param name="searchInternal">The internal instance from which to copy data.</param>
        internal Search(SearchInternal searchInternal)
            : this(
                searchInternal.SearchText,
                searchInternal.Token,
                searchInternal.State,
                searchInternal.Responses,
                searchInternal.Options)
        {
        }

        /// <summary>
        ///     Gets the options for the search.
        /// </summary>
        public SearchOptions Options { get; }

        /// <summary>
        ///     Gets the collection of responses received from peers.
        /// </summary>
        public IReadOnlyCollection<SearchResponse> Responses => ResponseList.ToList().AsReadOnly();

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

        private IEnumerable<SearchResponse> ResponseList { get; }
    }
}