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

    public class SoulseekClientDownloadAsyncTests
    {
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
            Assert.IsType<TimeoutException>(ex.InnerException);
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
            Assert.IsType<TimeoutException>(ex.InnerException);
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

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on download cancellation")]
        public async Task DownloadAsync_Throws_DownloadException_On_Download_Cancellation()
        {
            var options = new SoulseekClientOptions() { };
            options.MessageTimeout = 5;

            var response = new PeerTransferResponse(1, false, 1, "");
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, "username", 1);

            var request = new PeerTransferRequest(TransferDirection.Download, 1, "filename", 42);
            var requestWaitKey = new WaitKey(MessageCode.PeerTransferRequest, "username", "filename");

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));

            var conn = new Mock<IMessageConnection>();

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", "username", "filename", 1, null, conn.Object));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }
    }
}
