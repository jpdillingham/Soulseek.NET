// <copyright file="SearchTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
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

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Fact(DisplayName = "Response filter returns true when FilterResponses option is false")]
        public void Response_Filter_Returns_True_When_FilterResponses_Option_Is_False()
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: false));
            var response = new SearchResponseSlim("u", 1, 1, 1, 1, 1, null);

            var filter = s.InvokeMethod<bool>("SlimResponseMeetsOptionCriteria", response);

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

            var filter = s.InvokeMethod<bool>("SlimResponseMeetsOptionCriteria", response);

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

            var filter = s.InvokeMethod<bool>("SlimResponseMeetsOptionCriteria", response);

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

            var filter = s.InvokeMethod<bool>("SlimResponseMeetsOptionCriteria", response);

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

            var filter = s.InvokeMethod<bool>("SlimResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "AddResponse")]
        [Fact(DisplayName = "AddResponse ignores response when search is not in progress")]
        public void AddResponse_Ignores_Response_When_Search_Is_Not_In_Progress()
        {
            var s = new Search("foo", 42)
            {
                State = SearchStates.Completed
            };

            s.AddResponse(new SearchResponseSlim("bar", 42, 1, 1, 1, 1, null));

            Assert.Empty(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Fact(DisplayName = "AddResponse ignores response when token does not match")]
        public void AddResponse_Ignores_Response_When_Token_Does_Not_Match()
        {
            var s = new Search("foo", 42)
            {
                State = SearchStates.InProgress
            };

            s.AddResponse(new SearchResponseSlim("bar", 24, 1, 1, 1, 1, null));

            Assert.Empty(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Fact(DisplayName = "AddResponse ignores response when response criteria not met")]
        public void AddResponse_Ignores_Response_When_Response_Criteria_Not_Met()
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1))
            {
                State = SearchStates.InProgress
            };

            s.AddResponse(new SearchResponseSlim("bar", 42, 0, 1, 1, 1, null));

            Assert.Empty(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse adds response"), AutoData]
        public void AddResponse_Adds_Response(string username, int token, byte code, string filename, int size, string extension)
        {
            var s = new Search("foo", token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1))
            {
                State = SearchStates.InProgress
            };

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            s.AddResponse(new SearchResponseSlim(username, token, 1, 1, 1, 1, reader));

            Assert.Single(s.Responses);

            var responses = s.Responses.ToList();
            var response = responses[0];
            var files = response.Files.ToList();

            Assert.Equal(1, response.FileCount);
            Assert.Equal(username, response.Username);
            Assert.Equal(filename, files[0].Filename);
            Assert.Equal(size, files[0].Size);
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse ignores response when all files are filtered and response filtering is enabled"), AutoData]
        public void AddResponse_Ignores_Response_When_All_Files_Are_Filtered_And_Response_Filtering_Is_Enabled(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: true,
                    minimumResponseFileCount: 1,
                    fileFilter: (f) => false);

            var s = new Search("foo", token, options)
            {
                State = SearchStates.InProgress
            };

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            s.AddResponse(new SearchResponseSlim(username, token, 1, 1, 1, 1, reader));

            Assert.Empty(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse completes search and invokes completed event when file limit reached"), AutoData]
        public async Task AddResponse_Completes_Search_And_Invokes_Completed_Event_When_File_Limit_Reached(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: false,
                    minimumResponseFileCount: 1,
                    fileLimit: 1);

            var s = new Search("foo", token, options)
            {
                State = SearchStates.InProgress
            };

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file lists

            var task = s.WaitForCompletion(CancellationToken.None);

            s.AddResponse(new SearchResponseSlim(username, token, 1, 1, 1, 1, reader));

            await task;

            Assert.True(s.State.HasFlag(SearchStates.Completed));
            Assert.True(s.State.HasFlag(SearchStates.FileLimitReached));
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse completes search and invokes completed event when response limit reached"), AutoData]
        public async Task AddResponse_Completes_Search_And_Invokes_Completed_Event_When_Response_Limit_Reached(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: false,
                    minimumResponseFileCount: 1,
                    responseLimit: 1,
                    fileLimit: 10000000);

            var s = new Search("foo", token, options)
            {
                State = SearchStates.InProgress
            };

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file lists

            var task = s.WaitForCompletion(CancellationToken.None);

            s.AddResponse(new SearchResponseSlim(username, token, 1, 1, 1, 1, reader));

            await task;

            Assert.True(s.State.HasFlag(SearchStates.Completed));
            Assert.True(s.State.HasFlag(SearchStates.ResponseLimitReached));
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse invokes response received event"), AutoData]
        public void AddResponse_Invokes_Response_Received_Event_Handler(string username, int token, byte code, string filename, int size, string extension)
        {
            SearchResponse addResponse = null;

            var s = new Search("foo", token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1))
            {
                State = SearchStates.InProgress
            };

            s.ResponseReceived += (response) => addResponse = response;

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            s.AddResponse(new SearchResponseSlim(username, token, 1, 1, 1, 1, reader));

            Assert.NotNull(addResponse);
            Assert.Equal(filename, addResponse.Files.ToList()[0].Filename);
        }
    }
}
