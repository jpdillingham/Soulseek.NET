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

namespace Soulseek.Tests.Unit.Network
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class MessageConnectionTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates peer connection with given username and IP"), AutoData]
        public void Instantiates_Peer_Connection_With_Given_Username_And_IP(string username, IPEndPoint endpoint, ConnectionOptions options)
        {
            using (var c = new MessageConnection(username, endpoint, options))
            {
                Assert.Equal(username, c.Username);
                Assert.Equal(endpoint.Address, c.IPEndPoint.Address);
                Assert.Equal(endpoint.Port, c.IPEndPoint.Port);
                Assert.Equal(options, c.Options);
                Assert.False(c.ReadingContinuously);

                Assert.Equal(new ConnectionKey(username, endpoint), c.Key);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws when given username is invalid")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData(null)]
        public void Throws_When_Given_Username_Is_Invalid(string username)
        {
            IMessageConnection c;
            var ex = Record.Exception(() => c = new MessageConnection(username, new IPEndPoint(IPAddress.Parse("0.0.0.0"), 1), null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates server connection with given IP"), AutoData]
        public void Instantiates_Server_Connection_With_Given_IP(IPEndPoint endpoint, ConnectionOptions options)
        {
            using (var c = new MessageConnection(endpoint, options))
            {
                Assert.Equal(endpoint.Address, c.IPEndPoint.Address);
                Assert.Equal(endpoint.Port, c.IPEndPoint.Port);
                Assert.Equal(options, c.Options);

                Assert.Equal(new ConnectionKey(string.Empty, endpoint), c.Key);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates peer connection with given IP and username"), AutoData]
        public void Instantiates_Peer_Connection_With_Given_IP_And_Username(string username, IPEndPoint endpoint, ConnectionOptions options)
        {
            using (var c = new MessageConnection(username, endpoint, options))
            {
                Assert.Equal(username, c.Username);
                Assert.Equal(endpoint.Address, c.IPEndPoint.Address);
                Assert.Equal(endpoint.Port, c.IPEndPoint.Port);
                Assert.Equal(options, c.Options);

                Assert.Equal(new ConnectionKey(username, endpoint), c.Key);
            }
        }

        [Trait("Category", "WriteAsync")]
        [Theory(DisplayName = "WriteAsync throws InvalidOperationException when disconnected"), AutoData]
        public async Task WriteAsync_Throws_InvalidOperationException_When_Disconnected(string username, IPEndPoint endpoint)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            using (var c = new MessageConnection(username, endpoint))
            {
                c.SetProperty("State", ConnectionState.Disconnected);

                var ex = await Record.ExceptionAsync(() => c.WriteAsync(msg));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "WriteAsync")]
        [Theory(DisplayName = "WriteAsync throws InvalidOperationException when disconnected"), AutoData]
        public async Task WriteAsync_Throws_InvalidOperationException_When_Disconnecting(string username, IPEndPoint endpoint)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            using (var c = new MessageConnection(username, endpoint))
            {
                c.SetProperty("State", ConnectionState.Disconnecting);

                var ex = await Record.ExceptionAsync(() => c.WriteAsync(msg));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "WriteAsync")]
        [Theory(DisplayName = "WriteAsync writes when connected"), AutoData]
        public async Task WriteAsync_Writes_When_Connected(string username, IPEndPoint endpoint)
        {
            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var tcpMock = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                tcpMock.Setup(m => m.Client).Returns(socket);
                tcpMock.Setup(s => s.Connected).Returns(true);
                tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

                var msg = new MessageBuilder()
                    .WriteCode(MessageCode.Peer.BrowseRequest)
                    .Build();

                using (var c = new MessageConnection(username, endpoint, tcpClient: tcpMock.Object))
                {
                    await c.WriteAsync(msg);

                    streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "WriteAsync")]
        [Theory(DisplayName = "WriteAsync throws ConnectionWriteException when Stream.WriteAsync throws"), AutoData]
        public async Task WriteAsync_Throws_ConnectionWriteException_When_Stream_WriteAsync_Throws(IPEndPoint endpoint)
        {
            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new IOException());
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var tcpMock = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                tcpMock.Setup(m => m.Client).Returns(socket);
                tcpMock.Setup(s => s.Connected).Returns(true);
                tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

                var msg = new MessageBuilder()
                    .WriteCode(MessageCode.Peer.BrowseRequest)
                    .Build();

                using (var c = new MessageConnection(endpoint, tcpClient: tcpMock.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(msg));

                    Assert.NotNull(ex);
                    Assert.IsType<ConnectionWriteException>(ex);
                    Assert.IsType<IOException>(ex.InnerException);

                    streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "ReadContinuously")]
        [Theory(DisplayName = "ReadContinuously raises MessageRead on read"), AutoData]
        public void ReadContinuously_Raises_MessageRead_On_Read(string username, IPEndPoint endpoint)
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
                        var data = BitConverter.GetBytes((int)MessageCode.Peer.InfoRequest);
                        Array.Copy(data, bytes, data.Length);
                    }

                    callCount++;
                })
                .Returns(Task.Run(() => 4));

            var tcpMock = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                tcpMock.Setup(m => m.Client).Returns(socket);
                tcpMock.Setup(s => s.Connected).Returns(true);
                tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

                byte[] readMessage = null;

                using (var c = new MessageConnection(username, endpoint, tcpClient: tcpMock.Object))
                {
                    c.StartReadingContinuously();

                    c.MessageRead += (sender, e) => readMessage = e.Message;

                    Thread.Sleep(1000); // ReadContinuouslyAsync() runs in a separate task, so events won't arrive immediately after connect

                    Assert.Equal(MessageCode.Peer.InfoRequest, new MessageReader<MessageCode.Peer>(readMessage).ReadCode());
                }
            }
        }

        [Trait("Category", "ReadContinuously")]
        [Theory(DisplayName = "ReadContinuously raises MessageDataRead on read"), AutoData]
        public void ReadContinuously_Raises_MessageDataRead_On_Read(string username, IPEndPoint endpoint, int code)
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
                        var data = BitConverter.GetBytes(code);
                        Array.Copy(data, bytes, data.Length);
                    }

                    callCount++;
                })
                .Returns(Task.Run(() => 4));

            var tcpMock = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                tcpMock.Setup(m => m.Client).Returns(socket);
                tcpMock.Setup(s => s.Connected).Returns(true);
                tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

                byte[] readMessage = null;

                using (var c = new MessageConnection(username, endpoint, tcpClient: tcpMock.Object))
                {
                    c.StartReadingContinuously();

                    c.MessageDataRead += (sender, e) => readMessage = e.Code;

                    Thread.Sleep(1000); // ReadContinuouslyAsync() runs in a separate task, so events won't arrive immediately after connect

                    Assert.Equal(code, BitConverter.ToInt32(readMessage));
                }
            }
        }

        [Trait("Category", "ReadContinuously")]
        [Theory(DisplayName = "ReadContinuously raises MessageReceived on read"), AutoData]
        public void ReadContinuously_Raises_MessageReceived_On_Read(string username, IPEndPoint endpoint, int code)
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
                        var data = BitConverter.GetBytes(code);
                        Array.Copy(data, bytes, data.Length);
                    }

                    callCount++;
                })
                .Returns(Task.Run(() => 4));

            var tcpMock = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                tcpMock.Setup(m => m.Client).Returns(socket);
                tcpMock.Setup(s => s.Connected).Returns(true);
                tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

                byte[] readMessage = null;

                using (var c = new MessageConnection(username, endpoint, tcpClient: tcpMock.Object))
                {
                    c.StartReadingContinuously();

                    c.MessageReceived += (sender, e) => readMessage = e.Code;

                    Thread.Sleep(1000); // ReadContinuouslyAsync() runs in a separate task, so events won't arrive immediately after connect

                    Assert.Equal(code, BitConverter.ToInt32(readMessage));
                }
            }
        }

        [Trait("Category", "ReadContinuously")]
        [Theory(DisplayName = "ReadContinuously changes as expected"), AutoData]
        public async Task ReadContinuously_Returns_Expected_Values(string username, IPEndPoint endpoint)
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

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                tcpMock.Setup(m => m.Client).Returns(socket);
                tcpMock.Setup(s => s.Connected).Returns(true);
                tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

                using (var c = new MessageConnection(username, endpoint, tcpClient: tcpMock.Object))
                {
                    var a = c.ReadingContinuously;

                    await Record.ExceptionAsync(() => c.InvokeMethod<Task>("ReadContinuouslyAsync"));

                    Assert.False(a);
                    Assert.True(b);
                    Assert.False(c.ReadingContinuously);
                }
            }

            streamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "ReadContinuously")]
        [Theory(DisplayName = "ReadContinuously changes as expected"), AutoData]
        public async Task ReadContinuously_Returns_If_Already_Reading(string username, IPEndPoint endpoint)
        {
            var streamMock = new Mock<INetworkStream>();
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            var tcpMock = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                tcpMock.Setup(m => m.Client).Returns(socket);
                tcpMock.Setup(s => s.Connected).Returns(true);
                tcpMock.Setup(s => s.GetStream()).Returns(streamMock.Object);

                using (var c = new MessageConnection(username, endpoint, tcpClient: tcpMock.Object))
                {
                    c.SetProperty("ReadingContinuously", true);

                    await c.InvokeMethod<Task>("ReadContinuouslyAsync");
                }
            }

            streamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}