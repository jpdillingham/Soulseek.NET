// <copyright file="Search.cs" company="JP Dillingham">
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

namespace Soulseek.NET
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     A single file search.
    /// </summary>
    public sealed class Search : IDisposable
    {
        private int resultCount = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class with the specified <paramref name="searchText"/>,
        ///     <paramref name="options"/>, and <paramref name="serverConnection"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="options">The options for the search.</param>
        /// <param name="serverConnection">The connection to use when searching.</param>
        internal Search(string searchText, int token, SearchOptions options, IMessageConnection serverConnection)
        {
            SearchText = searchText;
            Token = token;
            Options = options;
            ServerConnection = serverConnection;
            SearchFilters = new SearchFilters(Options);

            SearchTimeoutTimer = new SystemTimer()
            {
                Interval = Options.SearchTimeout * 1000,
                Enabled = false,
                AutoReset = false,
            };

            SearchTimeoutTimer.Elapsed += (sender, e) => { End($"The search completed after {options.SearchTimeout} seconds of inactivity."); };
            SearchTimeoutTimer.Reset();
        }

        internal Action<Search, SearchResponse> ResponseHandler { get; set; } = (search, response) => { };
        internal Action<Search, string> EndHandler { get; set; } = (search, message) => { };
        internal Action<Search> TimeoutHandler { get; set; } = (search) => { };

        /// <summary>
        ///     Gets the options for the search.
        /// </summary>
        public SearchOptions Options { get; private set; }

        /// <summary>
        ///     Gets the collection of responses received from peers.
        /// </summary>
        public IEnumerable<SearchResponse> Responses => ResponseList.AsReadOnly();

        /// <summary>
        ///     Gets the text for which to search.
        /// </summary>
        public string SearchText { get; private set; }

        /// <summary>
        ///     Gets the unique identifier for the search.
        /// </summary>
        public int Token { get; private set; }

        private SearchFilters SearchFilters { get; set; }
        private bool Disposed { get; set; } = false;
        private List<SearchResponse> ResponseList { get; set; } = new List<SearchResponse>();
        private SystemTimer SearchTimeoutTimer { get; set; }
        private IMessageConnection ServerConnection { get; set; }
        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        internal void AddResponse(IMessageConnection connection, SearchResponse response)
        {
            Console.WriteLine($"[Adding]");
            if (response.Token == Token && SearchFilters.ResponseMeetsOptionCriteria(response))
            {
                response.ParseFiles();

                if (Options.FilterFiles)
                {
                    response.Files = response.Files.Where(f => SearchFilters.FileMeetsOptionCriteria(f));
                }

                Interlocked.Add(ref resultCount, response.Files.Count());

                if (resultCount >= Options.FileLimit)
                {
                    End($"The search completed after receiving {Options.FileLimit} results.");
                    return;
                }

                ResponseList.Add(response);

                Task.Run(() => ResponseHandler(this, response)).Forget();

                SearchTimeoutTimer.Reset();
            }
        }

        /// <summary>
        ///     Ends the search with the specified <paramref name="state"/>.
        /// </summary>
        /// <remarks>
        ///     A state of <see cref="SearchState.Completed"/> indicates that the search completed normally by timeout or after
        ///     having reached the result limit, while <see cref="SearchState.Stopped"/> indicates that the search was stopped
        ///     prematurely, e.g., by error or user request.
        /// </remarks>
        /// <param name="state">The desired state of the search.</param>
        internal void End(string message)
        {
            SearchTimeoutTimer.Stop();
            EndHandler(this, message);
        }

        ///// <summary>
        /////     Asynchronously starts the search.
        ///// </summary>
        ///// <returns>This search.</returns>
        //internal async Task<Search> SearchAsync(CancellationToken? cancellationToken)
        //{
        //    if (State != SearchState.Pending)
        //    {
        //        throw new SearchException($"The Search is already in progress or has completed.");
        //    }

        //    State = SearchState.InProgress;

        //    var request = new SearchRequest(SearchText, Ticket);

        //    Console.WriteLine($"Searching for {SearchText}...");
        //    await ServerConnection.SendMessageAsync(request.ToMessage());

        //    SearchTimeoutTimer.Reset();
        //    SearchTimeoutTimer.Elapsed += (sender, e) => End(SearchState.Completed);

        //    return await MessageWaiter.WaitIndefinitely<Search>(MessageCode.ServerFileSearch, Ticket.ToString(), cancellationToken);
        //}

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    SearchTimeoutTimer.Dispose();
                    ResponseList = default(List<SearchResponse>);
                }

                Disposed = true;
            }
        }
    }
}