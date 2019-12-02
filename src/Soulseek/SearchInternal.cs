// <copyright file="SearchInternal.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     A single file search.
    /// </summary>
    internal class SearchInternal : IDisposable
    {
        private int fileCount = 0;
        private int responseCount = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchInternal"/> class.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The options for the search.</param>
        public SearchInternal(string searchText, int token, SearchOptions options = null)
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
        ///     Gets the total number of files contained within received responses.
        /// </summary>
        public int FileCount => fileCount;

        /// <summary>
        ///     Gets the options for the search.
        /// </summary>
        public SearchOptions Options { get; }

        /// <summary>
        ///     Gets the current number of responses received.
        /// </summary>
        public int ResponseCount => responseCount;

        /// <summary>
        ///     Gets or sets the Action to invoke when a new search response is received.
        /// </summary>
        public Action<SearchResponse> ResponseReceived { get; set; }

        /// <summary>
        ///     Gets the text for which to search.
        /// </summary>
        public string SearchText { get; }

        /// <summary>
        ///     Gets or sets the state of the search.
        /// </summary>
        public SearchStates State { get; set; } = SearchStates.None;

        /// <summary>
        ///     Gets the unique identifier for the search.
        /// </summary>
        public int Token { get; }

        private bool Disposed { get; set; } = false;
        private SystemTimer SearchTimeoutTimer { get; set; }
        private TaskCompletionSource<int> TaskCompletionSource { get; set; } = new TaskCompletionSource<int>();

        /// <summary>
        ///     Completes the search with the specified <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The terminal state of the search.</param>
        public void Complete(SearchStates state)
        {
            SearchTimeoutTimer.Stop();
            State = SearchStates.Completed | state;
            TaskCompletionSource.SetResult(0);
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="SearchInternal"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Adds the specified <paramref name="slimResponse"/> to the list of responses after applying the filters specified
        ///     in the search options.
        /// </summary>
        /// <param name="slimResponse">The response to add.</param>
        public void TryAddResponse(SearchResponseSlim slimResponse)
        {
            // ensure the search is still active, the token matches and that the response meets basic filtering criteria we check
            // the slim response for fitness prior to extracting the file list from it for performance reasons.
            if (!Disposed && State.HasFlag(SearchStates.InProgress) && slimResponse.Token == Token && SlimResponseMeetsOptionCriteria(slimResponse))
            {
                // extract the file list from the response and filter it
                var fullResponse = SearchResponseFactory.FromSlimResponse(slimResponse);
                var filteredFiles = fullResponse.Files.Where(f => Options.FileFilter?.Invoke(f) ?? true);

                fullResponse = new SearchResponse(fullResponse.Username, fullResponse.Token, filteredFiles.Count(), fullResponse.FreeUploadSlots, fullResponse.UploadSpeed, fullResponse.QueueLength, filteredFiles);

                // ensure the filtered file count still meets the response criteria
                if ((Options.FilterResponses && fullResponse.FileCount < Options.MinimumResponseFileCount) || !(Options.ResponseFilter?.Invoke(fullResponse) ?? true))
                {
                    return;
                }

                Interlocked.Increment(ref responseCount);
                Interlocked.Add(ref fileCount, fullResponse.Files.Count);

                ResponseReceived?.Invoke(fullResponse);
                SearchTimeoutTimer.Reset();

                if (responseCount >= Options.ResponseLimit)
                {
                    Complete(SearchStates.ResponseLimitReached);
                }
                else if (fileCount >= Options.FileLimit)
                {
                    Complete(SearchStates.FileLimitReached);
                }
            }
        }

        /// <summary>
        ///     Asynchronously waits for the search to be completed.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The collection of received search responses.</returns>
        public async Task WaitForCompletion(CancellationToken cancellationToken)
        {
            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
            {
                var completedTask = await Task.WhenAny(TaskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);

                if (completedTask == cancellationTaskCompletionSource.Task)
                {
                    throw new OperationCanceledException("Operation cancelled.");
                }
            }
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="SearchInternal"/>.
        /// </summary>
        /// <param name="disposing">A value indicating whether the object is in the process of disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    SearchTimeoutTimer.Dispose();
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