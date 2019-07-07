// <copyright file="MessageConnectionTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Tcp
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Tcp;
    using Soulseek.Tcp;
    using Xunit;
    using Xunit.Abstractions;

    public class MessageConnectionTests
    {
        public MessageConnectionTests(ITestOutputHelper output)
        {
            Output = output;
        }

        private ITestOutputHelper Output { get; }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates peer connection with given username and IP"), AutoData]
        public void Instantiates_Peer_Connection_With_Given_Username_And_IP(string username, IPAddress ipAddress, int port, ConnectionOptions options)
        {
            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port, options);

            Assert.Equal(MessageConnectionType.Peer, c.Type);
            Assert.Equal(username, c.Username);
            Assert.Equal(ipAddress, c.IPAddress);
            Assert.Equal(port, c.Port);
            Assert.Equal(options, c.Options);
            Assert.False(c.ReadingContinuously);

            Assert.Equal(new ConnectionKey(username, ipAddress, port, MessageConnectionType.Peer), c.Key);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates server connection with given IP"), AutoData]
        public void Instantiates_Peer_Connection_With_Given_IP(IPAddress ipAddress, int port, ConnectionOptions options)
        {
            var c = new MessageConnection(MessageConnectionType.Server, ipAddress, port, options);

            Assert.Equal(MessageConnectionType.Server, c.Type);
            Assert.Equal(ipAddress, c.IPAddress);
            Assert.Equal(port, c.Port);
            Assert.Equal(options, c.Options);

            Assert.Equal(new ConnectionKey(string.Empty, ipAddress, port, MessageConnectionType.Server), c.Key);
        }

        [Trait("Category", "WriteMessageAsync")]
        [Theory(DisplayName = "WriteMessageAsync throws InvalidOperationException when disconnected"), AutoData]
        public async Task WriteMessageAsync_Throws_InvalidOperationException_When_Disconnected(string username, IPAddress ipAddress, int port)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port);
            c.SetProperty("State", ConnectionState.Disconnected);

            var ex = await Record.ExceptionAsync(async () => await c.WriteMessageAsync(msg));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "WriteMessageAsync")]
        [Theory(DisplayName = "WriteMessageAsync throws InvalidOperationException when disconnected"), AutoData]
        public async Task WriteMessageAsync_Throws_InvalidOperationException_When_Disconnecting(string username, IPAddress ipAddress, int port)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port);
            c.SetProperty("State", ConnectionState.Disconnecting);

            var ex = await Record.ExceptionAsync(async () => await c.WriteMessageAsync(msg));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "WriteMessageAsync")]
        [Theory(DisplayName = "WriteMessageAsync writes when connected"), AutoData]
        public async Task WriteMessageAsync_Writes_When_Connected(string username, IPAddress ipAddress, int port)
        {
            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var tcpMock = new Mock<ITcpClient>();
            tcpMock.Setup(m => m.Client).Returns(new Socket(SocketType.Stream, ProtocolType.IP));
            tcpMock.Setup(s => s.Connected).Returns(true);
            tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port, tcpClient: tcpMock.Object);

            await c.WriteMessageAsync(msg);

            streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "WriteMessageAsync")]
        [Theory(DisplayName = "WriteMessageAsync throws ConnectionWriteException when Stream.WriteAsync throws"), AutoData]
        public async Task WriteMessageAsync_Throws_ConnectionWriteException_When_Stream_WriteAsync_Throws(IPAddress ipAddress, int port)
        {
            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new IOException());
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var tcpMock = new Mock<ITcpClient>();
            tcpMock.Setup(m => m.Client).Returns(new Socket(SocketType.Stream, ProtocolType.IP));
            tcpMock.Setup(s => s.Connected).Returns(true);
            tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var c = new MessageConnection(MessageConnectionType.Server, ipAddress, port, tcpClient: tcpMock.Object);

            var ex = await Record.ExceptionAsync(async () => await c.WriteMessageAsync(msg));

            Assert.NotNull(ex);
            Assert.IsType<ConnectionWriteException>(ex);
            Assert.IsType<IOException>(ex.InnerException);

            streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "Code Normalization")]
        [Theory(DisplayName = "Codes normalized for server connections"), AutoData]
        public async Task Codes_Normalized_For_Server_Connections(IPAddress ipAddress, int port)
        {
            int code = 0;

            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));
            streamMock.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<byte[], int, int, CancellationToken>((bytes, offset, length, token) => code = BitConverter.ToInt32(bytes, 4))
                .Returns(Task.CompletedTask);

            var tcpMock = new Mock<ITcpClient>();
            tcpMock.Setup(m => m.Client).Returns(new Socket(SocketType.Stream, ProtocolType.IP));
            tcpMock.Setup(s => s.Connected).Returns(true);
            tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddUser)
                .Build();

            var c = new MessageConnection(MessageConnectionType.Server, ipAddress, port, tcpClient: tcpMock.Object);

            await c.WriteMessageAsync(msg);

            Assert.Equal((int)MessageCode.Server.AddUser - 10000, code);

            streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "Code Normalization")]
        [Theory(DisplayName = "Codes normalized for peer connections"), AutoData]
        public async Task Codes_Normalized_For_Peer_Connections(string username, IPAddress ipAddress, int port)
        {
            int code = 0;

            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));
            streamMock.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<byte[], int, int, CancellationToken>((bytes, offset, length, token) => code = BitConverter.ToInt32(bytes, 4))
                .Returns(Task.CompletedTask);

            var tcpMock = new Mock<ITcpClient>();
            tcpMock.Setup(m => m.Client).Returns(new Socket(SocketType.Stream, ProtocolType.IP));
            tcpMock.Setup(s => s.Connected).Returns(true);
            tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoRequest)
                .Build();

            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port, tcpClient: tcpMock.Object);

            await c.WriteMessageAsync(msg);

            Assert.Equal((int)MessageCode.Peer.InfoRequest - 20000, code);

            streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "ReadContinuouslyAsync")]
        [Theory(DisplayName = "ReadContinuouslyAsync raises MessageRead on read"), AutoData]
        public void ReadContinuouslyAsync_Raises_MessageRead_On_Read(string username, IPAddress ipAddress, int port)
        {
            int callCount = 0;

            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<byte[], int, int, CancellationToken>((bytes, offset, length, token) =>
                {
                    if (callCount % 2 == 0)
                    {
                        var data = BitConverter.GetBytes(4);
                        Array.Copy(data, bytes, data.Length);
                    }
                    else if (callCount % 2 == 1)
                    {
                        var data = BitConverter.GetBytes((int)MessageCode.Peer.InfoRequest - 20000);
                        Array.Copy(data, bytes, data.Length);
                    }

                    callCount++;
                })
                .Returns(Task.Run(() => 4));

            var tcpMock = new Mock<ITcpClient>();
            tcpMock.Setup(m => m.Client).Returns(new Socket(SocketType.Stream, ProtocolType.IP));
            tcpMock.Setup(s => s.Connected).Returns(true);
            tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

            byte[] readMessage = null;

            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port, tcpClient: tcpMock.Object);
            c.StartReadingContinuously();

            c.MessageRead += (sender, e) => readMessage = e;

            Thread.Sleep(1000); // ReadContinuouslyAsync() runs in a separate task, so events won't arrive immediately after connect

            Assert.Equal(MessageCode.Peer.InfoRequest, new MessageReader<MessageCode.Peer>(readMessage).ReadCode());
        }

        [Trait("Category", "ReadingContinuously")]
        [Theory(DisplayName = "ReadingContinuously changes as expected"), AutoData]
        public async Task ReadingContinuously_Returns_Expected_Values(string username, IPAddress ipAddress, int port)
        {
            bool b = false;

            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<byte[], int, int, CancellationToken>((bytes, offset, length, token) =>
                {
                    b = true;
                })
                .Throws(new Exception());

            var tcpMock = new Mock<ITcpClient>();
            tcpMock.Setup(m => m.Client).Returns(new Socket(SocketType.Stream, ProtocolType.IP));
            tcpMock.Setup(s => s.Connected).Returns(true);
            tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

            var c = new MessageConnection(MessageConnectionType.Peer, username, ipAddress, port, tcpClient: tcpMock.Object);

            var a = c.ReadingContinuously;

            await Record.ExceptionAsync(async () => await c.InvokeMethod<Task>("ReadContinuouslyAsync"));

            Assert.False(a);
            Assert.True(b);
            Assert.False(c.ReadingContinuously);
        }
    }
}