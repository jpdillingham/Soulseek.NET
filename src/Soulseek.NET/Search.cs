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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging.Messages;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     A single file search.
    /// </summary>
    public sealed class Search : IDisposable
    {
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
            if (State.HasFlag(SearchStates.InProgress) && slimResponse.Token == Token && ResponseMeetsOptionCriteria(slimResponse))
            {
                var fullResponse = new SearchResponse(slimResponse);
                fullResponse = new SearchResponse(fullResponse, fullResponse.Files.Where(f => FileMeetsOptionCriteria(f)).ToList());

                if (Options.FilterResponses && fullResponse.FileCount < Options.MinimumResponseFileCount)
                {
                    return;
                }

                Interlocked.Add(ref resultFileCount, fullResponse.Files.Count);

                ResponseBag.Add(fullResponse);

                ResponseReceived?.Invoke(fullResponse);
                SearchTimeoutTimer.Reset();

                if (resultFileCount >= Options.FileLimit)
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
                    ResponseBag = default(ConcurrentBag<SearchResponse>);
                }

                Disposed = true;
            }
        }

        private bool FileHasIgnoredExtension(File f)
        {
            return (Options.IgnoredFileExtensions != null) &&
                Options.IgnoredFileExtensions.Any(e =>
                    e.Equals(f.Extension, StringComparison.InvariantCultureIgnoreCase) ||
                    $".{e}".Equals(Path.GetExtension(f.Filename), StringComparison.InvariantCultureIgnoreCase));
        }

        private bool FileMeetsOptionCriteria(File file)
        {
            if (!Options.FilterFiles)
            {
                return true;
            }

            if (file.Size < Options.MinimumFileSize || FileHasIgnoredExtension(file))
            {
                return false;
            }

            var bitRate = file.GetAttributeValue(FileAttributeType.BitRate);
            var length = file.GetAttributeValue(FileAttributeType.Length);
            var bitDepth = file.GetAttributeValue(FileAttributeType.BitDepth);
            var sampleRate = file.GetAttributeValue(FileAttributeType.SampleRate);

            if ((bitRate != null && bitRate < Options.MinimumFileBitRate) ||
                (length != null && length < Options.MinimumFileLength) ||
                (bitDepth != null && bitDepth < Options.MinimumFileBitDepth) ||
                (sampleRate != null && sampleRate < Options.MinimumFileSampleRate))
            {
                return false;
            }

            var constantBitRates = new[] { 32, 64, 128, 192, 256, 320 };
            var isConstant = constantBitRates.Any(b => b == bitRate);

            if (bitRate != null && ((!Options.IncludeConstantBitRate && isConstant) || (!Options.IncludeVariableBitRate && !isConstant)))
            {
                return false;
            }

            return true;
        }

        private bool ResponseMeetsOptionCriteria(SearchResponseSlim response)
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