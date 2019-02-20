// <copyright file="DownloadAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class DownloadAsyncTests
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
            Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
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
            Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
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
            Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on token generation failure")]
        public async Task DownloadAsync_Throws_DownloadException_On_Token_Generation_Failure()
        {
            var tokenFactory = new Mock<ITokenFactory>();
            tokenFactory.Setup(m => m.TryGetToken(It.IsAny<Func<int, bool>>(), out It.Ref<int?>.IsAny))
                .Returns(false);

            var s = new SoulseekClient("127.0.0.1", 1, tokenFactory: tokenFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.Contains("Unable to generate a unique token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws DownloadException on peer message connection timeout")]
        public async Task DownloadAsync_Throws_DownloadException_On_Peer_Message_Connection_Timeout()
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

            var s = new SoulseekClient("127.0.0.1", 1, options);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<TimeoutException>(ex.InnerException);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws DownloadException when WriteMessageAsync throws"), AutoData]
        public async Task DownloadInternalAsync_Throws_DownloadException_When_WriteMessageAsync_Throws(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Throws(new ConnectionWriteException());
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<ConnectionWriteException>(ex.InnerException);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws DownloadException on PeerTransferResponse timeout"), AutoData]
        public async Task DownloadInternalAsync_Throws_DownloadException_On_PeerTransferResponse_Timeout(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<TimeoutException>(ex.InnerException);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws DownloadException on PeerTransferResponse cancellation"), AutoData]
        public async Task DownloadInternalAsync_Throws_DownloadException_On_PeerTransferResponse_Cancellation(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var waitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), null, null))
                .Returns(Task.FromException<PeerTransferResponse>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws DownloadException on PeerTransferResponse allowed"), AutoData]
        public async Task DownloadInternalAsync_Throws_DownloadException_On_PeerTransferResponse_Allowed(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(token, true, size, string.Empty);
            var waitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.Contains("unreachable", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws DownloadException on PeerTransferRequest cancellation"), AutoData]
        public async Task DownloadInternalAsync_Throws_DownloadException_On_PeerTransferRequest_Cancellation(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(token, false, size, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<PeerTransferRequest>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws DownloadException on download cancellation"), AutoData]
        public async Task DownloadInternalAsync_Throws_DownloadException_On_Download_Cancellation(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(token, false, size, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var request = new PeerTransferRequest(TransferDirection.Download, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws DownloadException on download start timeout"), AutoData]
        public async Task DownloadInternalAsync_Throws_DownloadException_On_Download_Start_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(token, false, size, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var request = new PeerTransferRequest(TransferDirection.Download, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromException<object>(new TimeoutException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<TimeoutException>(ex.InnerException);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync returns expected data on completion"), AutoData]
        public async Task DownloadInternalAsync_Returns_Expected_Data_On_Completion(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(token, false, size, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var request = new PeerTransferRequest(TransferDirection.Download, token, filename, size);

            var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult<byte[]>(data));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            byte[] downloadedData = null;
            var ex = await Record.ExceptionAsync(async () => downloadedData = await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.Null(ex);
            Assert.Equal(data, downloadedData);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync raises expected events on success"), AutoData]
        public async Task DownloadInternalAsync_Raises_Expected_Events_On_Success(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(1, false, 1, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var request = new PeerTransferRequest(TransferDirection.Download, token, filename, size);

            var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult<byte[]>(data));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var events = new List<DownloadStateChangedEventArgs>();

            s.DownloadStateChanged += (sender, e) =>
            {
                events.Add(e);
            };

            await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null);

            Assert.Equal(5, events.Count);

            Assert.Equal(DownloadStates.None, events[0].PreviousState);
            Assert.Equal(DownloadStates.Queued, events[0].State);

            Assert.Equal(DownloadStates.Queued, events[1].PreviousState);
            Assert.Equal(DownloadStates.Initializing, events[1].State);

            Assert.Equal(DownloadStates.Initializing, events[2].PreviousState);
            Assert.Equal(DownloadStates.InProgress, events[2].State);

            Assert.Equal(DownloadStates.InProgress, events[3].PreviousState);
            Assert.Equal(DownloadStates.Succeeded, events[3].State);

            Assert.Equal(DownloadStates.Succeeded, events[4].PreviousState);
            Assert.Equal(DownloadStates.Completed | DownloadStates.Succeeded, events[4].State);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync raises Download events on failure"), AutoData]
        public async Task DownloadInternalAsync_Raises_Download_Events_On_Failure(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(token, false, size, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var request = new PeerTransferRequest(TransferDirection.Download, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<byte[]>(new MessageReadException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var events = new List<DownloadStateChangedEventArgs>();

            s.DownloadStateChanged += (sender, e) =>
            {
                events.Add(e);
            };

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<MessageReadException>(ex.InnerException);

            Assert.Equal(DownloadStates.Errored, events[events.Count - 1].PreviousState);
            Assert.Equal(DownloadStates.Completed | DownloadStates.Errored, events[events.Count - 1].State);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync raises Download events on timeout"), AutoData]
        public async Task DownloadInternalAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(1, false, 1, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var request = new PeerTransferRequest(TransferDirection.Download, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<byte[]>(new TimeoutException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var events = new List<DownloadStateChangedEventArgs>();

            s.DownloadStateChanged += (sender, e) =>
            {
                events.Add(e);
            };

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<TimeoutException>(ex.InnerException);

            Assert.Equal(DownloadStates.TimedOut, events[events.Count - 1].PreviousState);
            Assert.Equal(DownloadStates.Completed | DownloadStates.TimedOut, events[events.Count - 1].State);
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync raises Download events on cancellation"), AutoData]
        public async Task DownloadInternalAsync_Raises_Expected_Final_Event_On_Cancellation(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new PeerTransferResponse(token, false, size, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.PeerTransferResponse, username, token);

            var request = new PeerTransferRequest(TransferDirection.Download, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PeerTransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), null, null))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<PeerTransferRequest>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, null))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            var connFactory = new Mock<IMessageConnectionFactory>();
            connFactory.Setup(m => m.GetMessageConnection(It.IsAny<MessageConnectionType>(), It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, options, messageWaiter: waiter.Object, messageConnectionFactory: connFactory.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var events = new List<DownloadStateChangedEventArgs>();

            s.DownloadStateChanged += (sender, e) =>
            {
                events.Add(e);
            };

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, null));

            Assert.NotNull(ex);
            Assert.IsType<DownloadException>(ex);
            Assert.IsType<OperationCanceledException>(ex.InnerException);

            Assert.Equal(DownloadStates.Cancelled, events[events.Count - 1].PreviousState);
            Assert.Equal(DownloadStates.Completed | DownloadStates.Cancelled, events[events.Count - 1].State);
        }
    }
}
