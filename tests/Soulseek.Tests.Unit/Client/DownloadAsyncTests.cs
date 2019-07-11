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

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Tcp;
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
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync(username, "filename"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Filename(string filename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", filename));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws InvalidOperationException when not connected")]
        public async Task DownloadAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws InvalidOperationException when not logged in")]
        public async Task DownloadAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync throws ArgumentException when token used")]
        public async Task DownloadAsync_Throws_ArgumentException_When_Token_Used()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, Transfer>();
                queued.TryAdd(1, new Transfer(TransferDirection.Download, "foo", "bar", 1));

                s.SetProperty("Downloads", queued);

                var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename", 1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        //[Trait("Category", "DownloadAsync")]
        //[Theory(DisplayName = "DownloadAsync throws TransferException on peer message connection timeout"), AutoData]
        //public async Task DownloadAsync_Throws_TransferException_On_Peer_Message_Connection_Timeout(IPAddress ip, int port)
        //{
        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var options = new SoulseekClientOptions(messageTimeout: 1);

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse("username", ip, port)));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: conn.Object, options: options))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var ex = await Record.ExceptionAsync(async () => await s.DownloadAsync("username", "filename"));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<TimeoutException>(ex.InnerException);
        //    }
        //}

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws TransferException when WriteAsync throws"), AutoData]
        public async Task DownloadInternalAsync_Throws_TransferException_When_WriteAsync_Throws(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), CancellationToken.None))
                .Throws(new ConnectionWriteException());
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync throws TransferException on TransferResponse timeout"), AutoData]
        //public async Task DownloadInternalAsync_Throws_TransferException_On_TransferResponse_Timeout(string username, IPAddress ip, int port, string filename, int token)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 1);

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IPeerConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<TimeoutException>(ex.InnerException);
        //    }
        //}

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws TransferException on TransferResponse cancellation"), AutoData]
        public async Task DownloadInternalAsync_Throws_TransferException_On_TransferResponse_Cancellation(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var waitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferResponse>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<OperationCanceledException>(ex.InnerException);
            }
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws TransferException on TransferRequest cancellation"), AutoData]
        public async Task DownloadInternalAsync_Throws_TransferException_On_TransferRequest_Cancellation(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferRequest>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<OperationCanceledException>(ex.InnerException);
            }
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws TransferException on download cancellation"), AutoData]
        public async Task DownloadInternalAsync_Throws_TransferException_On_Download_Cancellation(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<OperationCanceledException>(ex.InnerException);
            }
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync throws TransferException on download start timeout"), AutoData]
        public async Task DownloadInternalAsync_Throws_TransferException_On_Download_Start_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<object>(new TimeoutException()));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new TimeoutException()));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<TimeoutException>(ex.InnerException);
            }
        }

        [Trait("Category", "DownloadInternalAsync")]
        [Theory(DisplayName = "DownloadInternalAsync returns expected data on completion"), AutoData]
        public async Task DownloadInternalAsync_Returns_Expected_Data_On_Completion(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<byte[]>(data));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                byte[] downloadedData = null;
                var ex = await Record.ExceptionAsync(async () => downloadedData = await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.Null(ex);
                Assert.Equal(data, downloadedData);
            }
        }

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync returns expected data when acknowledgement is allowed"), AutoData]
        //public async Task DownloadInternalAsync_Returns_Expected_Data_When_Acknowledgement_Is_Allowed(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(token, true, size, string.Empty); // allowed
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult<byte[]>(data));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));
        //    connManager.Setup(m => m.AddUnsolicitedTransferConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        byte[] downloadedData = null;
        //        var ex = await Record.ExceptionAsync(async () => downloadedData = await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.Null(ex);
        //        Assert.Equal(data, downloadedData);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync throws DownloadRejectedException when acknowledgement is disallowed and message contains 'File not shared'"), AutoData]
        //public async Task DownloadInternalAsync_Throws_DownloadRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(token, false, size, "File not shared."); // not shared
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult<byte[]>(data));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));
        //    connManager.Setup(m => m.AddUnsolicitedTransferConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        byte[] downloadedData = null;
        //        var ex = await Record.ExceptionAsync(async () => downloadedData = await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<DownloadRejectedException>(ex.InnerException);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync raises expected events on success"), AutoData]
        //public async Task DownloadInternalAsync_Raises_Expected_Events_On_Success(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(1, false, 1, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult<byte[]>(data));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var events = new List<DownloadStateChangedEventArgs>();

        //        s.DownloadStateChanged += (sender, e) =>
        //        {
        //            events.Add(e);
        //        };

        //        await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null);

        //        Assert.Equal(5, events.Count);

        //        Assert.Equal(TransferStates.None, events[0].PreviousState);
        //        Assert.Equal(TransferStates.Requested, events[0].State);

        //        Assert.Equal(TransferStates.Requested, events[1].PreviousState);
        //        Assert.Equal(TransferStates.Queued, events[1].State);

        //        Assert.Equal(TransferStates.Queued, events[2].PreviousState);
        //        Assert.Equal(TransferStates.Initializing, events[2].State);

        //        Assert.Equal(TransferStates.Initializing, events[3].PreviousState);
        //        Assert.Equal(TransferStates.InProgress, events[3].State);

        //        Assert.Equal(TransferStates.InProgress, events[4].PreviousState);
        //        Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, events[4].State);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync invokes StateChanged delegate on state change"), AutoData]
        //public async Task DownloadInternalAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(1, false, 1, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult<byte[]>(data));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var fired = false;

        //        await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(stateChanged: (e) => fired = true), null);

        //        Assert.True(fired);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync raises DownloadProgressUpdated event on data read"), AutoData]
        //public async Task DownloadInternalAsync_Raises_DownloadProgressUpdated_Event_On_Data_Read(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(1, false, 1, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(BitConverter.GetBytes(token)))
        //        .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(Array.Empty<byte>(), 1, 1));

        //    var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult<byte[]>(data));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var events = new List<DownloadProgressUpdatedEventArgs>();

        //        s.DownloadProgressUpdated += (d, e) => events.Add(e);

        //        await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null);

        //        Assert.Equal(2, events.Count);
        //        Assert.Equal(1, events[0].BytesDownloaded);
        //        Assert.Equal(TransferStates.InProgress, events[0].State);

        //        Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, events[1].State);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync invokes ProgressUpdated delegate on data read"), AutoData]
        //public async Task DownloadInternalAsync_Invokes_ProgressUpdated_Delegate_On_Data_Read(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(1, false, 1, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(BitConverter.GetBytes(token)))
        //        .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(Array.Empty<byte>(), 1, 1));

        //    var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult<byte[]>(data));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var fired = false;

        //        await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(progressUpdated: (e) => fired = true), null);

        //        Assert.True(fired);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync raises Download events on failure"), AutoData]
        //public async Task DownloadInternalAsync_Raises_Download_Events_On_Failure(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(token, false, size, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromException<byte[]>(new MessageReadException()));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var events = new List<DownloadStateChangedEventArgs>();

        //        s.DownloadStateChanged += (sender, e) =>
        //        {
        //            events.Add(e);
        //        };

        //        var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<MessageReadException>(ex.InnerException);

        //        Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
        //        Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].State);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync raises Download events on timeout"), AutoData]
        //public async Task DownloadInternalAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(1, false, 1, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromException<byte[]>(new TimeoutException()));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var events = new List<DownloadStateChangedEventArgs>();

        //        s.DownloadStateChanged += (sender, e) =>
        //        {
        //            events.Add(e);
        //        };

        //        var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<TimeoutException>(ex.InnerException);

        //        Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
        //        Assert.Equal(TransferStates.Completed | TransferStates.TimedOut, events[events.Count - 1].State);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync raises Download events on cancellation"), AutoData]
        //public async Task DownloadInternalAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, int token)
        //{
        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
        //        .Throws(new OperationCanceledException("Wait cancelled."));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    using (var s = new SoulseekClient("127.0.0.1", 1, null, waiter: waiter.Object, serverConnection: conn.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var events = new List<DownloadStateChangedEventArgs>();

        //        s.DownloadStateChanged += (sender, e) =>
        //        {
        //            events.Add(e);
        //        };

        //        var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<OperationCanceledException>(ex.InnerException);

        //        Assert.Equal(TransferStates.Completed | TransferStates.Cancelled, events[events.Count - 1].State);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync throws TransferException and ConnectionException on transfer exception"), AutoData]
        //public async Task DownloadInternalAsync_Throws_TransferException_And_ConnectionException_On_Transfer_Exception(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(1, false, 1, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromException<byte[]>(new NullReferenceException()));

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromException<byte[]>(new ConnectionException("foo", new NullReferenceException())));

        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<ConnectionException>(ex.InnerException);
        //        Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync throws TransferException and TimeoutException on transfer timeout"), AutoData]
        //public async Task DownloadInternalAsync_Throws_TransferException_And_TimeoutException_On_Transfer_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(1, false, 1, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var request = new TransferRequest(TransferDirection.Download, token, filename, size);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromException<byte[]>(new TimeoutException()));

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));
        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(request));
        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromException<byte[]>(new TimeoutException()));

        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, connectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<TimeoutException>(ex.InnerException);
        //    }
        //}

        //[Trait("Category", "DownloadInternalAsync")]
        //[Theory(DisplayName = "DownloadInternalAsync throws TransferException on transfer rejection"), AutoData]
        //public async Task DownloadInternalAsync_Throws_TransferException_On_Transfer_Rejection(string username, IPAddress ip, int port, string filename, int token, int size)
        //{
        //    var options = new SoulseekClientOptions(messageTimeout: 5);

        //    var response = new TransferResponse(token, false, size, string.Empty);
        //    var responseWaitKey = new WaitKey(MessageCode.TransferResponse, username, token);

        //    var transferConn = new Mock<IConnection>();
        //    transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

        //    var waiter = new Mock<IWaiter>();
        //    waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(response));

        //    waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Throws(new DownloadRejectedException("foo"));

        //    waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult<byte[]>(data));
        //    waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(transferConn.Object));
        //    waiter.Setup(m => m.Wait<GetPeerAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(new GetPeerAddressResponse(username, ip, port)));

        //    var conn = new Mock<IMessageConnection>();
        //    conn.Setup(m => m.State)
        //        .Returns(ConnectionState.Connected);

        //    var connManager = new Mock<IPeerConnectionManager>();
        //    connManager.Setup(m => m.GetOrAddUnsolicitedPeerConnectionAsync(It.IsAny<ConnectionKey>(), It.IsAny<string>(), It.IsAny<EventHandler<Message>>(), It.IsAny<ConnectionOptions>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.FromResult(conn.Object));

        //    using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
        //    {
        //        s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

        //        byte[] downloadedData = null;
        //        var ex = await Record.ExceptionAsync(async () => downloadedData = await s.InvokeMethod<Task<byte[]>>("DownloadInternalAsync", username, filename, token, new TransferOptions(), null));

        //        Assert.NotNull(ex);
        //        Assert.IsType<TransferException>(ex);
        //        Assert.IsType<DownloadRejectedException>(ex.InnerException);
        //    }
        //}
    }
}
