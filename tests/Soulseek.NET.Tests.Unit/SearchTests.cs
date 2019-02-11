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

namespace Soulseek.NET.Tests.Unit
{
    using System;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Soulseek.NET.Messaging;
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
            var s = new Search("foo", 42);

            s.Complete(SearchStates.Cancelled);

            Assert.False(true);
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

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Fact(DisplayName = "File filter returns true when FilterFiles option is false")]
        public void File_Filter_Returns_True_When_FilterFiles_Option_Is_False()
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: false));
            var file = new File(1, "name", 1, "ext", 0);

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.True(filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects IgnoredFileExtensions option")]
        [InlineData("mp3", true)]
        [InlineData("m4a", false)]
        public void File_Filter_Respects_IgnoredFileExtensions_Option(string extension, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, ignoredFileExtensions: new[] { "m4a" }));
            var file = new File(1, "name", 1, extension, 0);

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects MinimumFileSize option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void File_Filter_Respects_MinimumFileSize_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, minimumFileSize: option));
            var file = new File(1, "name", actual, "ext", 0);

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects MinimumFileBitRate option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void File_Filter_Respects_MinimumFileBitRate_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, minimumFileBitRate: option));
            var file = new File(1, "name", 1, "ext", 1, new[] { new FileAttribute(FileAttributeType.BitRate, actual) });

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects IncludeConstantBitRate option")]
        [InlineData(320, true, true)]
        [InlineData(320, false, false)]
        public void File_Filter_Respects_IncludeConstantBitRate_Option(int actual, bool option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, includeConstantBitRate: option));
            var file = new File(1, "name", 1, "ext", 1, new[] { new FileAttribute(FileAttributeType.BitRate, actual) });

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects IncludeVariableBitRate option")]
        [InlineData(234, true, true)]
        [InlineData(234, false, false)]
        public void File_Filter_Respects_IncludeVariableBitRate_Option(int actual, bool option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, includeVariableBitRate: option));
            var file = new File(1, "name", 1, "ext", 1, new[] { new FileAttribute(FileAttributeType.BitRate, actual) });

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects MinimumFileLength option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void File_Filter_Respects_MinimumFileLength_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, minimumFileLength: option));
            var file = new File(1, "name", 1, "ext", 1, new[] { new FileAttribute(FileAttributeType.Length, actual) });

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects MinimumFileBitDepth option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void File_Filter_Respects_MinimumFileBitDepth_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, minimumFileBitDepth: option));
            var file = new File(1, "name", 1, "ext", 1, new[] { new FileAttribute(FileAttributeType.BitDepth, actual) });

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "FileMeetsOptionCriteria")]
        [Theory(DisplayName = "File filter respects MinimumFileSampleRate option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void File_Filter_Respects_MinimumFileSampleRate_Option(int actual, int option, bool expected)
        {
            var s = new Search("foo", 42, new SearchOptions(filterFiles: true, minimumFileSampleRate: option));
            var file = new File(1, "name", 1, "ext", 1, new[] { new FileAttribute(FileAttributeType.SampleRate, actual) });

            var filter = s.InvokeMethod<bool>("FileMeetsOptionCriteria", file);

            Assert.Equal(expected, filter);
        }

        [Trait("Category", "AddResponse")]
        [Fact(DisplayName = "AddResponse ignores response when search is not in progress")]
        public void AddResponse_Ignores_Response_When_Search_Is_Not_In_Progress()
        {
            var s = new Search("foo", 42);
            s.State = SearchStates.Completed;

            s.AddResponse(new SearchResponseSlim("bar", 42, 1, 1, 1, 1, null));

            Assert.Empty(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Fact(DisplayName = "AddResponse ignores response when token does not match")]
        public void AddResponse_Ignores_Response_When_Token_Does_Not_Match()
        {
            var s = new Search("foo", 42);
            s.State = SearchStates.InProgress;

            s.AddResponse(new SearchResponseSlim("bar", 24, 1, 1, 1, 1, null));

            Assert.Empty(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Fact(DisplayName = "AddResponse ignores response when response criteria not met")]
        public void AddResponse_Ignores_Response_When_Response_Criteria_Not_Met()
        {
            var s = new Search("foo", 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
            s.State = SearchStates.InProgress;

            s.AddResponse(new SearchResponseSlim("bar", 42, 0, 1, 1, 1, null));

            Assert.Empty(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse adds response"), AutoData]
        public void AddResponse_Adds_Response(string username, int token, byte code, string filename, int size, string extension)
        {
            var s = new Search("foo", token, new SearchOptions(filterFiles: false, filterResponses: true, minimumResponseFileCount: 1));
            s.State = SearchStates.InProgress;

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
                    filterFiles: true,
                    minimumFileBitDepth: 44);

            var s = new Search("foo", token, options);

            s.State = SearchStates.InProgress;

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
        [Theory(DisplayName = "AddResponse adds response when all files are filtered and response filtering is disabled"), AutoData]
        public void AddResponse_Ignores_Response_When_All_Files_Are_Filtered_And_Response_Filtering_Is_Disabled(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: false,
                    minimumResponseFileCount: 1,
                    filterFiles: true,
                    minimumFileBitDepth: 44);

            var s = new Search("foo", token, options);

            s.State = SearchStates.InProgress;

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

            s.AddResponse(new SearchResponseSlim(username, token, 1, 1, 1, 1, reader));

            Assert.Single(s.Responses);
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse completes search and invokes completed event when file limit reached"), AutoData]
        public void AddResponse_Completes_Search_And_Invokes_Completed_Event_When_File_Limit_Reached(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: false,
                    minimumResponseFileCount: 1,
                    fileLimit: 1);

            var completedState = SearchStates.None;

            var s = new Search("foo", token, options);

            s.State = SearchStates.InProgress;

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

            s.AddResponse(new SearchResponseSlim(username, token, 1, 1, 1, 1, reader));

            Assert.True(completedState.HasFlag(SearchStates.Completed));
            Assert.True(completedState.HasFlag(SearchStates.FileLimitReached));
        }

        [Trait("Category", "AddResponse")]
        [Theory(DisplayName = "AddResponse invokes response received event"), AutoData]
        public void AddResponse_Invokes_Response_Received_Event_Handler(string username, int token, byte code, string filename, int size, string extension)
        {
            SearchResponse addResponse = null;

            var s = new Search("foo", token, new SearchOptions(filterFiles: false, filterResponses: true, minimumResponseFileCount: 1));
            s.State = SearchStates.InProgress;

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
