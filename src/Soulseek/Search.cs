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

namespace Soulseek
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using Soulseek.Options;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     A single file search.
    /// </summary>
    public sealed class Search : IDisposable
    {
        private int resultCount = 0;
        private int resultFileCount = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The options for the search.</param>
        internal Search(string searchText, int token, SearchOptions options = null)
        {
            SearchText = searchText;
            Token = token;

            Options = options ?? new SearchOptions();

            SearchTimeoutTimer = new SystemTimer()
            {
                Interval = Options.SearchTimeout * 1000,
                Enabled = false,
                AutoReset = false,
            };

            SearchTimeoutTimer.Elapsed += (sender, e) => { Complete(SearchStates.TimedOut); };
            SearchTimeoutTimer.Reset();
        }

        /// <summary>
        ///     Gets the options for the search.
        /// </summary>
        public SearchOptions Options { get; }

        /// <summary>
        ///     Gets the collection of responses received from peers.
        /// </summary>
        public IReadOnlyCollection<SearchResponse> Responses => ResponseBag.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the text for which to search.
        /// </summary>
        public string SearchText { get; }

        /// <summary>
        ///     Gets the state of the search.
        /// </summary>
        public SearchStates State { get; internal set; } = SearchStates.None;

        /// <summary>
        ///     Gets the unique identifier for the search.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets or sets the Action to invoke when a new search response is received.
        /// </summary>
        internal Action<SearchResponse> ResponseReceived { get; set; }

        private bool Disposed { get; set; } = false;
        private ConcurrentBag<SearchResponse> ResponseBag { get; set; } = new ConcurrentBag<SearchResponse>();
        private SystemTimer SearchTimeoutTimer { get; set; }
        private TaskCompletionSource<int> TaskCompletionSource { get; set; } = new TaskCompletionSource<int>();

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Adds the specified <paramref name="slimResponse"/> to the list of responses after applying the filters specified in
        ///     the search options.
        /// </summary>
        /// <param name="slimResponse">The response to add.</param>
        internal void AddResponse(SearchResponseSlim slimResponse)
        {
            // ensure the search is still active, the token matches and that the response meets basic filtering criteria we check
            // the slim response for fitness prior to extracting the file list from it for performance reasons.
            if (State.HasFlag(SearchStates.InProgress) && slimResponse.Token == Token && SlimResponseMeetsOptionCriteria(slimResponse))
            {
                // extract the file list from the response and filter it
                var fullResponse = new SearchResponse(slimResponse);
                var filteredFiles = fullResponse.Files.Where(f => Options.FileFilter?.Invoke(f) ?? true);

                fullResponse = new SearchResponse(fullResponse, filteredFiles);

                // ensure the filtered file count still meets the response criteria
                if ((Options.FilterResponses && fullResponse.FileCount < Options.MinimumResponseFileCount) || !(Options.ResponseFilter?.Invoke(fullResponse) ?? true))
                {
                    return;
                }

                Interlocked.Increment(ref resultCount);
                Interlocked.Add(ref resultFileCount, fullResponse.Files.Count);

                ResponseBag.Add(fullResponse);

                ResponseReceived?.Invoke(fullResponse);
                SearchTimeoutTimer.Reset();

                if (resultCount >= Options.ResponseLimit)
                {
                    Complete(SearchStates.ResponseLimitReached);
                }
                else if (resultFileCount >= Options.FileLimit)
                {
                    Complete(SearchStates.FileLimitReached);
                }
            }
        }

        /// <summary>
        ///     Completes the search with the specified <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The terminal state of the search.</param>
        internal void Complete(SearchStates state)
        {
            SearchTimeoutTimer.Stop();
            State = SearchStates.Completed | state;
            TaskCompletionSource.SetResult(0);
        }

        /// <summary>
        ///     Asynchronously waits for the search to be completed.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The collection of received search responses.</returns>
        internal async Task<IEnumerable<SearchResponse>> WaitForCompletion(CancellationToken cancellationToken)
        {
            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
            {
                var completedTask = await Task.WhenAny(TaskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);

                if (completedTask == cancellationTaskCompletionSource.Task)
                {
                    throw new OperationCanceledException("Operation cancelled.");
                }

                return ResponseBag.ToList().AsReadOnly();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    SearchTimeoutTimer.Dispose();
                    ResponseBag = default;
                }

                Disposed = true;
            }
        }

        private bool SlimResponseMeetsOptionCriteria(SearchResponseSlim response)
        {
            if (Options.FilterResponses && (
                    response.FileCount < Options.MinimumResponseFileCount ||
                    response.FreeUploadSlots < Options.MinimumPeerFreeUploadSlots ||
                    response.UploadSpeed < Options.MinimumPeerUploadSpeed ||
                    response.QueueLength >= Options.MaximumPeerQueueLength))
            {
                return false;
            }

            return true;
        }
    }
}