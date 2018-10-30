// <copyright file="Search.cs" company="JP Dillingham">
//     Copyright(C) 2018 JP Dillingham
//     
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//     
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//     GNU General Public License for more details.
//     
//     You should have received a copy of the GNU General Public License
//     along with this program.If not, see<https://www.gnu.org/licenses/>.
// </copyright>

namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    public sealed class Search : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class with the specified <paramref name="searchText"/>, <paramref name="options"/>, and <paramref name="serverConnection"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="options">The options for the search.</param>
        /// <param name="serverConnection">The connection to use when searching.</param>
        internal Search(string searchText, SearchOptions options, Connection serverConnection)
        {
            SearchText = searchText;
            Options = options;
            ServerConnection = serverConnection;

            Ticket = new Random().Next(1, 2147483647);

            SearchTimeoutTimer = new SystemTimer()
            {
                Interval = Options.Timeout * 1000,
                Enabled = false,
                AutoReset = false,
            };
        }

        public event EventHandler<SearchCompletedEventArgs> SearchEnded;

        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        public SearchOptions Options { get; private set; }
        public IEnumerable<SearchResponse> Responses => ResponseList.AsReadOnly();
        public string SearchText { get; private set; }
        public SearchState State { get; private set; } = SearchState.Pending;
        public int Ticket { get; private set; }

        private bool Disposed { get; set; } = false;
        private List<SearchResponse> ResponseList { get; set; } = new List<SearchResponse>();
        private int SearchTimeout { get; set; }
        private SystemTimer SearchTimeoutTimer { get; set; }
        private Connection ServerConnection { get; set; }

        public void Dispose()
        {
            Dispose(true);
        }

        internal void AddResponse(SearchResponse response, NetworkEventArgs e)
        {
            if (State == SearchState.InProgress && ResponseMeetsOptionCriteria(response))
            {
                response.ParseFiles();

                if (Options.FilterFiles || true)
                {
                    response.Files = response.Files.Where(f => FileMeetsOptionCriteria(f));
                }

                ResponseList.Add(response);
                Task.Run(() => SearchResponseReceived?.Invoke(this, new SearchResponseReceivedEventArgs(e) { Response = response })).Forget();

                SearchTimeoutTimer.Reset();
            }
        }

        private bool ResponseMeetsOptionCriteria(SearchResponse response)
        {
            if (
                Options.FilterResponses && (
                    response.FileCount < Options.MinimumResponseFileCount ||
                    response.FreeUploadSlots < Options.MinimumPeerFreeUploadSlots ||
                    response.UploadSpeed < Options.MinimumPeerUploadSpeed ||
                    response.QueueLength > Options.MaximumPeerQueueLength
                )
            )
            {
                return false;
            }

            return true;
        }

        private bool FileMeetsOptionCriteria(File file)
        {
            if (
                Options.FilterFiles && (
                    file.Size < Options.MinimumFileSize ||
                    FileHasIgnoredExtension(file)
                )
            )
            {
                return false;
            }

            var bitRate = file.GetAttributeValue(FileAttributeType.BitRate);
            var length = file.GetAttributeValue(FileAttributeType.Length);
            var bitDepth = file.GetAttributeValue(FileAttributeType.BitDepth);
            var sampleRate = file.GetAttributeValue(FileAttributeType.SampleRate);

            if (
                bitRate != null && bitRate < Options.MinimumFileBitRate ||
                length != null && length < Options.MinimumFileLength ||
                bitDepth != null && bitDepth < Options.MinimumFileBitDepth ||
                sampleRate != null && sampleRate < Options.MinimumFileSampleRate
            )
            {
                return false;
            }

            var constantBitRates = new[] { 32, 64, 128, 192, 256, 320 };
            var isConstant = constantBitRates.Any(b => b == bitRate);

            if (bitRate != null && (!Options.IncludeConstantBitRate && isConstant || !Options.IncludeVariableBitRate && !isConstant))
            {
                return false;
            }

            return true;
        }

        private bool FileHasIgnoredExtension(File file)
        {
            return Options.IgnoredFileExtensions == null ? false : 
                Options.IgnoredFileExtensions.Any(e => e == System.IO.Path.GetExtension(file.Filename));
        }

        internal void End(SearchState state)
        {
            if (State != SearchState.Completed && State != SearchState.Stopped)
            {
                State = state;
                Task.Run(() => SearchEnded?.Invoke(this, new SearchCompletedEventArgs() { Search = this })).Forget();
            }
        }

        internal async Task<int> StartAsync()
        {
            if (State != SearchState.Pending)
            {
                throw new SearchException($"The Search is already in progress or has completed.");
            }

            State = SearchState.InProgress;

            var request = new SearchRequest(SearchText, Ticket);

            Console.WriteLine($"Searching for {SearchText}...");
            await ServerConnection.SendAsync(request.ToMessage().ToByteArray());

            SearchTimeoutTimer.Reset();
            SearchTimeoutTimer.Elapsed += (sender, e) => End(SearchState.Completed);

            return Ticket;
        }

        internal void Stop()
        {
            End(SearchState.Stopped);
        }

        private void Dispose(bool disposing)
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
    }
}