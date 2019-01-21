namespace Soulseek.NET.Tests.Unit
{
    using Moq;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class SoulseekClientTests
    {
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

        [Trait("Category", "Login")]
        [Fact(DisplayName = "Login throws on null username")]
        public async Task Login_Throws_On_Null_Username()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(null, Guid.NewGuid().ToString()));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "Login")]
        [Theory(DisplayName = "Login throws on bad input")]
        [InlineData(null, "a")]
        [InlineData("", "a")]
        [InlineData("a", null)]
        [InlineData("a", "")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public async Task Login_Throws_On_Bad_Input(string username, string password)
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(username, password));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "Login")]
        [Fact(DisplayName = "Login throws if logged in")]
        public async Task Login_Throws_If_Logged_In()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync("a", "b"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Login")]
        [Fact(DisplayName = "Login throws if not connected")]
        public async Task Login_Throws_If_Not_Connected()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Disconnected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync("a", "b"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }
        
        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            var s = new SoulseekClient();

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync(username, "filename"));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Filename(string filename)
        {
            var s = new SoulseekClient();

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", filename));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws InvalidOperationException when not connected")]
        public async Task DownloadAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            var s = new SoulseekClient();

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("Connected", ex.Message);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws InvalidOperationException when not logged in")]
        public async Task DownloadAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("logged in", ex.Message);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws ArgumentException when token used")]
        public async Task DownloadAsync_Throws_ArgumentException_When_Token_Used()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var queued = new ConcurrentDictionary<int, Download>();
            queued.TryAdd(1, new Download("foo", "bar", 1));

            s.SetProperty("QueuedDownloads", queued);

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename", 1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.Contains("token", ex.Message);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on token generation failure")]
        public async Task DownloadAsync_Throws_DownloadException_On_Token_Generation_Failure()
        {
            var tokenFactory= new Mock<ITokenFactory>();
            tokenFactory.Setup(m => m.TryGetToken(It.IsAny<Func<int, bool>>(), out It.Ref<int?>.IsAny))
                .Returns(false);

            var s = new SoulseekClient("127.0.0.1", 1, tokenFactory: tokenFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.Contains("Unable to generate a unique token", ex.Message);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on peer message connection timeout")]
        public async Task DownloadAsync_Throws_DownloadException_On_Peer_Message_Connection_Timeout()
        {
            var options = new SoulseekClientOptions() { };
            options.MessageTimeout = 1;

            var s = new SoulseekClient("127.0.0.1", 1, options);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<MessageTimeoutException>(ex.InnerException);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException when WriteMessageAsync throws")]
        public async Task DownloadAsync_Throws_DownloadException_When_WriteMessageAsync_Throws()
        {
            var options = new SoulseekClientOptions() { };
            options.MessageTimeout = 1;

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Throws(new ConnectionWriteException());

            var s = new SoulseekClient("127.0.0.1", 1, options);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", "username", "filename", 1, null, conn.Object));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<ConnectionWriteException>(ex.InnerException);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on PeerTransferResponse timeout")]
        public async Task DownloadAsync_Throws_DownloadException_On_PeerTransferResponse_Timeout()
        {
            var options = new SoulseekClientOptions() { };
            options.MessageTimeout = 1;

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, options);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", "username", "filename", 1, null, conn.Object));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<MessageTimeoutException>(ex.InnerException);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on PeerTransferResponse cancellation")]
        public async Task DownloadAsync_Throws_DownloadException_On_PeerTransferResponse_Cancellation()
        {
            var options = new SoulseekClientOptions() { };
            options.MessageTimeout = 5;

            var response = new PeerTransferResponse(1, true, 1, "");
            var waitKey = new WaitKey(MessageCode.PeerTransferResponse, "username", 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), null, null))
                .Returns(Task.FromException<PeerTransferResponse>(new MessageCancelledException()));

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", "username", "filename", 1, null, conn.Object));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<MessageCancelledException>(ex.InnerException);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on PeerTransferResponse allowed")]
        public async Task DownloadAsync_Throws_DownloadException_On_PeerTransferResponse_Allowed()
        {
            var options = new SoulseekClientOptions() { };
            options.MessageTimeout = 5;

            var response = new PeerTransferResponse(1, true, 1, "");
            var waitKey = new WaitKey(MessageCode.PeerTransferResponse, "username", 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), null, null))
                .Returns(Task.FromResult(response));

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", "username", "filename", 1, null, conn.Object));
            
            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.Contains("unreachable", ex.Message);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on PeerTransferRequest cancellation")]
        public async Task DownloadAsync_Throws_DownloadException_On_PeerTransferRequest_Cancellation()
        {
            var options = new SoulseekClientOptions() { };
            options.MessageTimeout = 5;

            var response = new PeerTransferResponse(1, false, 1, "");
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, "username", 1);

            var requestWaitKey = new WaitKey(MessageCode.PeerTransferRequest, "username", "filename");

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<PeerTransferRequest>(new MessageCancelledException()));

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", "username", "filename", 1, null, conn.Object));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<MessageCancelledException>(ex.InnerException);
        }
    }
}
