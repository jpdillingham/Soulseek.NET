// <copyright file="SoulseekClientTests.cs" company="JP Dillingham">
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
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class SoulseekClientTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with with given options"), AutoData]
        public void Instantiates_With_Given_Options(SoulseekClientOptions options)
        {
            var s = new SoulseekClient(options);

            Assert.Equal(options, s.Options);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with defaults for minimal constructor")]
        public void Instantiates_With_Defaults_For_Minimal_Constructor()
        {
            var s = new SoulseekClient();

            var defaultServer = s.GetField<string>("DefaultAddress");
            var defaultPort = s.GetField<int>("DefaultPort");

            Assert.Equal(defaultServer, s.Address);
            Assert.Equal(defaultPort, s.Port);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates without exception")]
        public void Instantiates_Without_Exception()
        {
            SoulseekClient s = null;

            var ex = Record.Exception(() => s = new SoulseekClient());

            Assert.Null(ex);
            Assert.NotNull(s);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "State is Disconnected initially")]
        public void State_Is_Disconnected_Initially()
        {
            var s = new SoulseekClient();

            Assert.Equal(SoulseekClientStates.Disconnected, s.State);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Username is null initially")]
        public void Username_Is_Null_Initially()
        {
            var s = new SoulseekClient();

            Assert.Null(s.Username);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect fails if connected")]
        public async Task Connect_Fails_If_Connected()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws when TcpConnection throws")]
        public async Task Connect_Throws_When_TcpConnection_Throws()
        {
            var c = new Mock<IMessageConnection>();
            c.Setup(m => m.ConnectAsync()).Throws(new ConnectionException());

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);

            var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

            Assert.NotNull(ex);
            Assert.IsType<ConnectionException>(ex);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect succeeds when TcpConnection succeeds")]
        public async Task Connect_Succeeds_When_TcpConnection_Succeeds()
        {
            var c = new Mock<IMessageConnection>();

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);

            var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

            Assert.Null(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws on a bad address")]
        public void Instantiation_Throws_On_A_Bad_Address()
        {
            var ex = Record.Exception(() => new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), new SoulseekClientOptions()));

            Assert.NotNull(ex);
            Assert.IsType<SoulseekClientException>(ex);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect disconnects")]
        public async Task Disconnect_Disconnects()
        {
            var c = new Mock<IMessageConnection>();

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);
            await s.ConnectAsync();

            var ex = Record.Exception(() => s.Disconnect());

            Assert.Null(ex);
            Assert.Equal(SoulseekClientStates.Disconnected, s.State);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect clears searches")]
        public async Task Disconnect_Clears_Searches()
        {
            var c = new Mock<IMessageConnection>();

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);
            await s.ConnectAsync();

            var searches = new ConcurrentDictionary<int, Search>();
            searches.TryAdd(0, new Search(string.Empty, 0, new SearchOptions()));
            searches.TryAdd(1, new Search(string.Empty, 1, new SearchOptions()));

            s.SetProperty("ActiveSearches", searches);

            var ex = Record.Exception(() => s.Disconnect());

            Assert.Null(ex);
            Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            Assert.Empty(searches);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect clears downloads")]
        public async Task Disconnect_Clears_Downloads()
        {
            var c = new Mock<IMessageConnection>();

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);
            await s.ConnectAsync();

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            activeDownloads.TryAdd(0, new Download(string.Empty, string.Empty, 0));
            activeDownloads.TryAdd(1, new Download(string.Empty, string.Empty, 1));

            var queuedDownloads = new ConcurrentDictionary<int, Download>();
            queuedDownloads.TryAdd(0, new Download(string.Empty, string.Empty, 0));
            queuedDownloads.TryAdd(1, new Download(string.Empty, string.Empty, 1));

            s.SetProperty("ActiveDownloads", activeDownloads);
            s.SetProperty("QueuedDownloads", queuedDownloads);

            var ex = Record.Exception(() => s.Disconnect());

            Assert.Null(ex);
            Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            Assert.Empty(activeDownloads);
            Assert.Empty(queuedDownloads);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect clears peer queue")]
        public async Task Disconnect_Clears_Peer_Queue()
        {
            var c = new Mock<IMessageConnection>();

            var p = new Mock<IConnectionManager<IMessageConnection>>();

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object, peerConnectionManager: p.Object);
            await s.ConnectAsync();

            var ex = Record.Exception(() => s.Disconnect());

            Assert.Null(ex);
            Assert.Equal(SoulseekClientStates.Disconnected, s.State);

            p.Verify(m => m.RemoveAll(), Times.AtLeastOnce);
        }

        [Trait("Category", "Dispose/Finalize")]
        [Fact(DisplayName = "Disposes without exception")]
        public void Disposes_Without_Exception()
        {
            var s = new SoulseekClient();

            var ex = Record.Exception(() => s.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "Dispose/Finalize")]
        [Fact(DisplayName = "Finalizes without exception")]
        public void Finalizes_Without_Exception()
        {
            var s = new SoulseekClient();

            var ex = Record.Exception(() => s.InvokeMethod("Finalize"));

            Assert.Null(ex);
        }
    }
}
