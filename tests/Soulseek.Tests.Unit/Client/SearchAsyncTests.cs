// <copyright file="SearchAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Messaging.Tcp;
    using Xunit;

    public class SearchAsyncTests
    {
        [Trait("Category", "SearchAsync")]
        [Fact(DisplayName = "SearchAsync throws InvalidOperationException when not connected")]
        public async Task SearchAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            var s = new SoulseekClient();

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync("foo", 0, cancellationToken: CancellationToken.None));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "SearchAsync")]
        [Fact(DisplayName = "SearchAsync throws InvalidOperationException when not logged in")]
        public async Task SearchAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync("foo", 0));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync throws ArgumentException given bad search text")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData(" ")]
        public async Task SearchAsync_Throws_ArgumentException_Given_Bad_Search_Text(string search)
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync(search, 0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("searchText", ((ArgumentException)ex).ParamName);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync throws ArgumentException given a token in use"), AutoData]
        public async Task SearchAsync_Throws_ArgumentException_Given_A_Token_In_Use(string text, int token)
        {
            var dict = new ConcurrentDictionary<int, Search>();
            dict.TryAdd(token, new Search(text, token, new SearchOptions()));

            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
            s.SetProperty("Searches", dict);

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync(text, token));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("token", ((ArgumentException)ex).ParamName);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync returns completed search"), AutoData]
        public async Task SearchAsync_Returns_Completed_Search(string searchText, int token, string username)
        {
            var options = new SearchOptions(searchTimeout: 1);

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1)
                .WriteInteger(1)
                .WriteLong(1)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var task = s.SearchAsync(searchText, token, options);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            var responses = await task.ConfigureAwait(false);

            var res = responses.ToList()[0];

            Assert.Equal(username, res.Username);
            Assert.Equal(token, res.Token);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync adds search to ActiveSearches"), AutoData]
        public async Task SearchInternalAsync_Adds_Search_To_ActiveSearches(string searchText, int token)
        {
            var options = new SearchOptions(searchTimeout: 1, fileLimit: 1);
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var search = new Search(searchText, token, options)
            {
                State = SearchStates.InProgress
            };

            search.SetProperty("ResponseBag", new ConcurrentBag<SearchResponse>() { response });

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var cts = new CancellationTokenSource(1000);

            var task = s.SearchAsync(searchText, token, options, cts.Token);

            var active = s.GetProperty<ConcurrentDictionary<int, Search>>("Searches").ToList();

            cts.Cancel();

            await Record.ExceptionAsync(async () => await task); // swallow the cancellation exception

            Assert.Single(active);
            Assert.Contains(active, kvp => kvp.Key == token);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync creates token when not given"), AutoData]
        public async Task SearchInternalAsync_Creates_Token_When_Not_Given(string searchText)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var cts = new CancellationTokenSource(1000);

            var task = s.SearchAsync(searchText, cancellationToken: cts.Token);

            var active = s.GetProperty<ConcurrentDictionary<int, Search>>("Searches").ToList();

            cts.Cancel();

            await Record.ExceptionAsync(async () => await task); // swallow the cancellation exception

            Assert.Single(active);
            Assert.Contains(active, kvp => kvp.Value.SearchText == searchText);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task SearchInternalAsync_Throws_OperationCanceledException_On_Cancellation(string searchText, int token)
        {
            var options = new SearchOptions();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ct = new CancellationToken(true);

            var ex = await Record.ExceptionAsync(() => s.SearchAsync(searchText, token, options, ct));

            Assert.NotNull(ex);
            Assert.IsType<SearchException>(ex);
            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync throws SearchException on error"), AutoData]
        public async Task SearchInternalAsync_Throws_SearchException_On_Error(string searchText, int token)
        {
            var options = new SearchOptions(searchTimeout: 1);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new Exception("foo")));

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(() => s.SearchAsync(searchText, token, options, null));

            Assert.NotNull(ex);
            Assert.IsType<SearchException>(ex);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync invokes StateChanged delegate"), AutoData]
        public async Task SearchAsync_Invokes_StateChanged_Delegate(string searchText, int token)
        {
            var fired = false;
            var options = new SearchOptions(searchTimeout: 1, fileLimit: 1, stateChanged: (e) => fired = true);
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var search = new Search(searchText, token, options)
            {
                State = SearchStates.InProgress
            };

            search.SetProperty("ResponseBag", new ConcurrentBag<SearchResponse>() { response });

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var task = s.SearchAsync(searchText, token, options, null);

            await task;

            Assert.True(fired);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync fires SearchStateChanged event"), AutoData]
        public async Task SearchAsync_Fires_SearchStateChanged_Event(string searchText, int token)
        {
            var fired = false;
            var options = new SearchOptions(searchTimeout: 1, fileLimit: 1);
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var search = new Search(searchText, token, options)
            {
                State = SearchStates.InProgress
            };

            search.SetProperty("ResponseBag", new ConcurrentBag<SearchResponse>() { response });

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SearchStateChanged += (sender, e) => fired = true;
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var task = s.SearchAsync(searchText, token, options, null);

            await task;

            Assert.True(fired);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync invokes ResponseReceived delegate"), AutoData]
        public async Task SearchAsync_Invokes_ResponseReceived_Delegate(string searchText, int token)
        {
            var fired = false;
            var options = new SearchOptions(searchTimeout: 1, fileLimit: 1, responseReceived: (e) => fired = true);
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var task = s.SearchAsync(searchText, token, options, null);

            var search = s.GetProperty<ConcurrentDictionary<int, Search>>("Searches")[token];
            search.ResponseReceived.Invoke(response);

            await task;

            Assert.True(fired);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync fires SearchResponseReceived event"), AutoData]
        public async Task SearchAsync_Fires_SearchResponseReceived_Event(string searchText, int token)
        {
            var fired = false;
            var options = new SearchOptions(searchTimeout: 1, fileLimit: 1);
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>(), null))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SearchResponseReceived += (sender, e) => fired = true;
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var task = s.SearchAsync(searchText, token, options, null);

            var search = s.GetProperty<ConcurrentDictionary<int, Search>>("Searches")[token];
            search.ResponseReceived.Invoke(response);

            await task;

            Assert.True(fired);
        }
    }
}
