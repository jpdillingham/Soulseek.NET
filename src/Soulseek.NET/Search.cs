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
    using Soulseek.NET.Messaging.Messages;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     A single file search.
    /// </summary>
    internal sealed class Search : IDisposable
    {
        private int resultCount = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The options for the search.</param>
        public Search(string searchText, int token, SearchOptions options = null)
            : this(searchText, token, (search, response) => { }, (search, state) => { }, options)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="responseHandler">The action invoked upon receipt of a search response.</param>
        /// <param name="completeHandler">The action invoked upon completion of the search.</param>
        /// <param name="options">The options for the search.</param>
        public Search(string searchText, int token, Action<Search, SearchResponse> responseHandler, Action<Search, SearchStates> completeHandler, SearchOptions options = null)
        {
            SearchText = searchText;
            Token = token;

            ResponseHandler = responseHandler ?? ((search, response) => { });
            CompleteHandler = completeHandler ?? ((search, state) => { });

            Options = options ?? new SearchOptions();

            SearchTimeoutTimer = new SystemTimer()
            {
                Interval = Options.SearchTimeout * 1000,
                Enabled = false,
                AutoReset = false,
            };

            SearchTimeoutTimer.Elapsed += (sender, e) => { Complete(SearchStates.Completed | SearchStates.TimedOut); };
            SearchTimeoutTimer.Reset();
        }

        /// <summary>
        ///     Gets the options for the search.
        /// </summary>
        public SearchOptions Options { get; }

        /// <summary>
        ///     Gets the collection of responses received from peers.
        /// </summary>
        public IEnumerable<SearchResponse> Responses => ResponseList.AsReadOnly();

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

        private Action<Search, SearchStates> CompleteHandler { get; }
        private bool Disposed { get; set; } = false;
        private Action<Search, SearchResponse> ResponseHandler { get; }
        private List<SearchResponse> ResponseList { get; set; } = new List<SearchResponse>();
        private SystemTimer SearchTimeoutTimer { get; set; }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Adds the specified <paramref name="slimResponse"/> to the list of responses after applying the filters specified in the search options.
        /// </summary>
        /// <param name="slimResponse">The response to add.</param>
        public void AddResponse(SearchResponseSlim slimResponse)
        {
            if (State.HasFlag(SearchStates.InProgress) && slimResponse.Token == Token && ResponseMeetsOptionCriteria(slimResponse))
            {
                var fullResponse = new SearchResponse(slimResponse);

                if (Options.FilterFiles)
                {
                    fullResponse = new SearchResponse(fullResponse, fullResponse.Files.Where(f => FileMeetsOptionCriteria(f)).ToList());
                }

                Interlocked.Add(ref resultCount, fullResponse.Files.Count());

                if (resultCount >= Options.FileLimit)
                {
                    Complete(SearchStates.Completed | SearchStates.FileLimitReached);
                    return;
                }

                ResponseList.Add(fullResponse);

                Task.Run(() => ResponseHandler(this, fullResponse)).Forget();

                SearchTimeoutTimer.Reset();
            }
        }

        /// <summary>
        ///     Completes the search with the specified <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The terminal state of the search.</param>
        public void Complete(SearchStates state)
        {
            SearchTimeoutTimer.Stop();
            State = state;
            CompleteHandler(this, state);
        }

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

        private bool FileMeetsOptionCriteria(File file)
        {
            if (!Options.FilterFiles)
            {
                return true;
            }

            bool fileHasIgnoredExtension(File f)
            {
                return (Options.IgnoredFileExtensions != null) && Options.IgnoredFileExtensions.Any(e => e == System.IO.Path.GetExtension(f.Filename));
            }

            if (file.Size < Options.MinimumFileSize || fileHasIgnoredExtension(file))
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
                    response.QueueLength > Options.MaximumPeerQueueLength))
            {
                return false;
            }

            return true;
        }
    }
}