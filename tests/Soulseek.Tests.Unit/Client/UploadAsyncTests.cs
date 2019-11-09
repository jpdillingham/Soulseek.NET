// <copyright file="UploadAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Soulseek.Options;
    using Xunit;

    public class UploadAsyncTests
    {
        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync(username, "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws ArgumentException given bad filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_Throws_ArgumentException_Given_Bad_Filename(string filename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync("username", filename, new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync throws InvalidOperationException when not connected")]
        public async Task UploadAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync("username", "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync throws InvalidOperationException when not logged in")]
        public async Task UploadAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync("username", "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync throws DuplicateTokenException when token used")]
        public async Task UploadAsync_Throws_DuplicateTokenException_When_Token_Used()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, Transfer>();
                queued.TryAdd(1, new Transfer(TransferDirection.Upload, "foo", "bar", 1));

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync("username", "filename", new byte[] { 0x0 }, 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws DuplicateTransferException when an existing Upload matches the username and filename"), AutoData]
        public async Task UploadAsync_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_The_Username_And_Filename(string username, string filename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, Transfer>();
                queued.TryAdd(0, new Transfer(TransferDirection.Upload, username, filename, 0));

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync(username, filename, new byte[] { 0x0 }, 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws TimeoutException on peer message connection timeout"), AutoData]
        public async Task UploadAsync_Throws_TimeoutException_On_Peer_Message_Connection_Timeout(IPAddress ip, int port)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var options = new ClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse("username", ip, port)));

            var manager = new Mock<IPeerConnectionManager>();
            manager.Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<string>(), ip, port, It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object, serverConnection: conn.Object, options: options, peerConnectionManager: manager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync("username", "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException when WriteAsync throws"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_When_WriteAsync_Throws(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new ClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

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

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException on TransferResponse timeout"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_On_TransferResponse_Timeout(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new ClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));
            waiter.Setup(m => m.Wait<TransferResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException on TransferResponse cancellation"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_On_TransferResponse_Cancellation(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var waitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferResponse>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException on TransferRequest cancellation"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_On_TransferRequest_Cancellation(string username, IPAddress ip, int port, string filename, int token)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferRequest>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException on Upload cancellation"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_On_Upload_Cancellation(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TimeoutException on Upload start timeout"), AutoData]
        public async Task UploadInternalAsync_Throws_TimeoutException_On_Upload_Start_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<object>(new TimeoutException()));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new TimeoutException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync returns expected data on completion"), AutoData]
        public async Task UploadInternalAsync_Returns_Expected_Data_On_Completion(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                byte[] UploadedData = null;
                var ex = await Record.ExceptionAsync(async () => UploadedData = await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.Null(ex);
                Assert.Equal(data, UploadedData);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync returns expected data when acknowledgement is allowed"), AutoData]
        public async Task UploadInternalAsync_Returns_Expected_Data_When_Acknowledgement_Is_Allowed(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                byte[] UploadedData = null;
                var ex = await Record.ExceptionAsync(async () => UploadedData = await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.Null(ex);
                Assert.Equal(data, UploadedData);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws UploadRejectedException when acknowledgement is disallowed and message contains 'File not shared'"), AutoData]
        public async Task UploadInternalAsync_Throws_UploadRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "File not shared."); // not shared
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                byte[] UploadedData = null;
                var ex = await Record.ExceptionAsync(async () => UploadedData = await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<TransferRejectedException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises expected events on success when skipping queue"), AutoData]
        public async Task UploadInternalAsync_Raises_Expected_Events_On_Success_When_Skipping_Queue(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start Upload immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null);

                Assert.Equal(4, events.Count);

                Assert.Equal(TransferStates.None, events[0].PreviousState);
                Assert.Equal(TransferStates.Requested, events[0].State);

                Assert.Equal(TransferStates.Requested, events[1].PreviousState);
                Assert.Equal(TransferStates.Initializing, events[1].State);

                Assert.Equal(TransferStates.Initializing, events[2].PreviousState);
                Assert.Equal(TransferStates.InProgress, events[2].State);

                Assert.Equal(TransferStates.InProgress, events[3].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, events[3].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises expected events on success when queued"), AutoData]
        public async Task UploadInternalAsync_Raises_Expected_Events_On_Success_When_Queued(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null);

                Assert.Equal(5, events.Count);

                Assert.Equal(TransferStates.None, events[0].PreviousState);
                Assert.Equal(TransferStates.Requested, events[0].State);

                Assert.Equal(TransferStates.Requested, events[1].PreviousState);
                Assert.Equal(TransferStates.Queued, events[1].State);

                Assert.Equal(TransferStates.Queued, events[2].PreviousState);
                Assert.Equal(TransferStates.Initializing, events[2].State);

                Assert.Equal(TransferStates.Initializing, events[3].PreviousState);
                Assert.Equal(TransferStates.InProgress, events[3].State);

                Assert.Equal(TransferStates.InProgress, events[4].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, events[4].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync invokes StateChanged delegate on state change"), AutoData]
        public async Task UploadInternalAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var fired = false;

                await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(stateChanged: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises UploadProgressUpdated event on data read"), AutoData]
        public async Task UploadInternalAsync_Raises_UploadProgressUpdated_Event_On_Data_Read(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[size]))
                .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(1, 1));

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferProgressUpdatedEventArgs>();

                s.TransferProgressUpdated += (d, e) => events.Add(e);

                await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null);

                Assert.Equal(2, events.Count);
                Assert.Equal(TransferStates.InProgress, events[0].State);

                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, events[1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync invokes ProgressUpdated delegate on data read"), AutoData]
        public async Task UploadInternalAsync_Invokes_ProgressUpdated_Delegate_On_Data_Read(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)))
                .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(1, 1));

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var fired = false;

                await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(progressUpdated: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises Upload events on failure"), AutoData]
        public async Task UploadInternalAsync_Raises_Upload_Events_On_Failure(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new MessageReadException()));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises Upload events on timeout"), AutoData]
        public async Task UploadInternalAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new TimeoutException()));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.TimedOut, events[events.Count - 1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises Upload events on cancellation"), AutoData]
        public async Task UploadInternalAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, int token)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException("Wait cancelled."));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient("127.0.0.1", 1, null, waiter: waiter.Object, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);

                Assert.Equal(TransferStates.Completed | TransferStates.Cancelled, events[events.Count - 1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException and ConnectionException on transfer exception"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_And_ConnectionException_On_Transfer_Exception(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new NullReferenceException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new ConnectionException("foo", new NullReferenceException())));

            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TimeoutException on transfer timeout"), AutoData]
        public async Task UploadInternalAsync_Throws_TimeoutException_On_Transfer_Timeout(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new TimeoutException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new TimeoutException()));

            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task UploadInternalAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException on transfer rejection"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_On_Transfer_Rejection(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Throws(new TransferRejectedException("foo"));

            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<byte[]>(data));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                byte[] UploadedData = null;
                var ex = await Record.ExceptionAsync(async () => UploadedData = await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<TransferRejectedException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws ConnectionException when transfer connection fails"), AutoData]
        public async Task UploadInternalAsync_Throws_ConnectionException_When_Transfer_Connection_Fails(string username, IPAddress ip, int port, string filename, int token, int size)
        {
            var options = new ClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

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
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            // mock a failing indirect connection
            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
            waiter.Setup(m => m.Wait<IConnection>(indirectKey, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            // mock a failing direct connection
            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
            waiter.Setup(m => m.Wait<IConnection>(directKey, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                byte[] UploadedData = null;
                var ex = await Record.ExceptionAsync(async () => UploadedData = await s.InvokeMethod<Task<byte[]>>("UploadInternalAsync", username, filename, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.IsType<AggregateException>(ex.InnerException.InnerException);
                Assert.True(ex.InnerException.Message.ContainsInsensitive("Failed to establish a direct or indirect connection."));
            }
        }
    }
}
