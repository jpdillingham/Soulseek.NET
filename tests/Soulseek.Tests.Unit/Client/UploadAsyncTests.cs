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
        [Theory(DisplayName = "UploadAsync throws ArgumentException given bad filename")]
        [InlineData(null)]
        [InlineData(new byte[] { })]
        public async Task UploadAsync_Throws_ArgumentException_Given_Null_Or_Empty_Byte_Array(byte[] data)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(async () => await s.UploadAsync("username", "filename", data));

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

            var options = new SoulseekClientOptions(messageTimeout: 1);

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
        public async Task UploadInternalAsync_Throws_TransferException_When_WriteAsync_Throws(string username, IPAddress ip, int port, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, 0); // accept
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new ConnectionException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new ConnectionException()));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TimeoutException on TransferResponse timeout"), AutoData]
        public async Task UploadInternalAsync_Throws_TimeoutException_On_TransferResponse_Timeout(string username, IPAddress ip, int port, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

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

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws OperationCanceledException on TransferResponse cancellation"), AutoData]
        public async Task UploadInternalAsync_Throws_OperationCanceledException_On_TransferResponse_Cancellation(string username, IPAddress ip, int port, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

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

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws OperationCanceledException on request write cancellation"), AutoData]
        public async Task UploadInternalAsync_Throws_OperationCanceledException_On_Request_Write_Cancellation(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new OperationCanceledException()));

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TimeoutException on transfer response timeout"), AutoData]
        public async Task UploadInternalAsync_Throws_TimeoutException_On_Transfer_Response_Timeout(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferResponse>(new TimeoutException()));
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

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync completes without Exception when transfer is allowed"), AutoData]
        public async Task UploadInternalAsync_Completes_Without_Exception_When_Transfer_Is_Allowed(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferRejectedException when acknowledgement is disallowed and message contains 'File not shared'"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPAddress ip, int port, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty); // reject
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<TransferRejectedException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync invokes StateChanged delegate on state change"), AutoData]
        public async Task UploadInternalAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
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

                await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(stateChanged: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises UploadProgressUpdated event on data write"), AutoData]
        public async Task UploadInternalAsync_Raises_UploadProgressUpdated_Event_On_Data_Read(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Raises(m => m.DataWritten += null, this, new ConnectionDataEventArgs(1, 1));
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[size]));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null);

                // 2 events, 1 fired from the mock above, 1 fired in the finally block
                Assert.Equal(2, events.Count);
                Assert.Equal(TransferStates.InProgress, events[0].State);

                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, events[1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync invokes ProgressUpdated delegate on data read"), AutoData]
        public async Task UploadInternalAsync_Invokes_ProgressUpdated_Delegate_On_Data_Read(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

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

                await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(progressUpdated: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises Upload events on failure"), AutoData]
        public async Task UploadInternalAsync_Raises_Upload_Events_On_Failure(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

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
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new MessageReadException()));
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

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises Upload events on timeout"), AutoData]
        public async Task UploadInternalAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

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
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new TimeoutException()));
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

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.TimedOut, events[events.Count - 1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync raises Upload events on cancellation"), AutoData]
        public async Task UploadInternalAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, byte[] data, int token)
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

                var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);

                Assert.Equal(TransferStates.Completed | TransferStates.Cancelled, events[events.Count - 1].State);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException and ConnectionException on transfer exception"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_And_ConnectionException_On_Transfer_Exception(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

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

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new ConnectionException("foo", new NullReferenceException())));

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
                Assert.IsType<TransferException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TimeoutException on transfer timeout"), AutoData]
        public async Task UploadInternalAsync_Throws_TimeoutException_On_Transfer_Timeout(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new TimeoutException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new TimeoutException()));

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
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task UploadInternalAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
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

            transferConn.Verify(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws TransferException on transfer rejection"), AutoData]
        public async Task UploadInternalAsync_Throws_TransferException_On_Transfer_Rejection(string username, IPAddress ip, int port, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty); // reject
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<TransferRejectedException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync throws ConnectionException when transfer connection fails"), AutoData]
        public async Task UploadInternalAsync_Throws_ConnectionException_When_Transfer_Connection_Fails(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new ConnectionException()));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync updates remote user on failure"), AutoData]
        public void UploadInternalAsync_Updates_Remote_User_On_Failure(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

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

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new ConnectionException("foo", new NullReferenceException())));

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

                var ex = Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
            }

            var expectedBytes = new UploadFailed(filename).ToByteArray();
            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(expectedBytes)), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "UploadInternalAsync")]
        [Theory(DisplayName = "UploadInternalAsync swallows final read exception"), AutoData]
        public async Task UploadInternalAsync_Swallows_Final_Read_Exception(string username, IPAddress ip, int port, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            transferConn.Setup(m => m.ReadAsync(1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new ConnectionReadException("Remote connection closed.", new ConnectionException("Remote connection closed."))));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, ip, port, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, ip, port, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient("127.0.0.1", 1, options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadInternalAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.Null(ex);
            }
        }
    }
}
