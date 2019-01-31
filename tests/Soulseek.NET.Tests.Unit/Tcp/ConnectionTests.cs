// <copyright file="ConnectionTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class ConnectionTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            Connection c = null;

            var ip = new IPAddress(0x0);
            var port = 1;

            var ex = Record.Exception(() => c = new Connection(ip, port));

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(ip, c.IPAddress);
            Assert.Equal(port, c.Port);
            Assert.Equal(new ConnectionKey(ip, port), c.Key);
            Assert.Equal(ConnectionState.Pending, c.State);
            Assert.Null(c.Context);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given options")]
        public void Instantiates_With_Given_Options()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var options = new ConnectionOptions(1, 1, 1);

            var c = new Connection(ip, port, options);

            Assert.Equal(options, c.Options);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given TcpClient")]
        public void Instantiates_With_Given_TcpClient()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();

            var c = new Connection(ip, port, tcpClient: t.Object);

            var ct = c.GetProperty<ITcpClient>("TcpClient");

            Assert.Equal(t.Object, ct);
        }

        [Trait("Category", "Context")]
        [Fact(DisplayName = "Context get and set")]
        public void Context_Get_And_Set()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var c = new Connection(ip, port);

            var context = Guid.NewGuid();

            c.Context = context;

            Assert.Equal(context, c.Context);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var c = new Connection(ip, port);

            var ex = Record.Exception(() => c.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnects when disconnected without throwing")]
        public void Disconnects_When_Not_Connected_Without_Throwing()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var c = new Connection(ip, port);
            c.SetProperty("State", ConnectionState.Disconnected);

            var ex = Record.Exception(() => c.Disconnect());

            Assert.Null(ex);
            Assert.Equal(ConnectionState.Disconnected, c.State);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnects when not disconnected")]
        public void Disconnects_When_Not_Disconnected_Without_Throwing()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var c = new Connection(ip, port);
            c.SetProperty("State", ConnectionState.Connected);

            var ex = Record.Exception(() => c.Disconnect());

            Assert.Null(ex);
            Assert.Equal(ConnectionState.Disconnected, c.State);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect raises StateChanged event")]
        public void Disconnect_Raises_StateChanged_Event()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var c = new Connection(ip, port);
            c.SetProperty("State", ConnectionState.Connected);

            var eventArgs = new List<ConnectionStateChangedEventArgs>();

            c.StateChanged += (sender, e) => eventArgs.Add(e);

            c.Disconnect("foo");

            Assert.Equal(ConnectionState.Disconnected, c.State);

            // the event will fire twice, once on transition to Disconnecting, and again on transition to Disconnected.
            Assert.Equal(2, eventArgs.Count);
            Assert.Equal(ConnectionState.Disconnecting, eventArgs[0].CurrentState);
            Assert.Equal(ConnectionState.Disconnected, eventArgs[1].CurrentState);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect raises Disconnected event")]
        public void Disconnect_Raises_Disconnected_Event()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var c = new Connection(ip, port);
            c.SetProperty("State", ConnectionState.Connected);

            var eventArgs = new List<string>();

            c.Disconnected += (sender, e) => eventArgs.Add(e);

            c.Disconnect("foo");

            Assert.Equal(ConnectionState.Disconnected, c.State);

            Assert.Single(eventArgs);
            Assert.Equal("foo", eventArgs[0]);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws when not pending or disconnected")]
        public async Task Connect_Throws_When_Not_Pending_Or_Disconnected()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var c = new Connection(ip, port);
            c.SetProperty("State", ConnectionState.Connected);

            var ex = await Record.ExceptionAsync(async () => await c.ConnectAsync());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect connects when not connected or transitioning")]
        public async Task Connect_Connects_When_Not_Connected_Or_Transitioning()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();
            var c = new Connection(ip, port, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.ConnectAsync());

            Assert.Null(ex);
            Assert.Equal(ConnectionState.Connected, c.State);

            t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws when timed out")]
        public async Task Connect_Throws_When_Timed_Out()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()))
                .Returns(Task.Run(() => Thread.Sleep(10000)));

            var o = new ConnectionOptions(connectTimeout: 0);
            var c = new Connection(ip, port, options: o, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.ConnectAsync());

            Assert.NotNull(ex);
            Assert.IsType<ConnectionException>(ex);
            Assert.IsType<TimeoutException>(ex.InnerException);

            t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws when TcpClient throws")]
        public async Task Connect_Throws_When_TcpClient_Throws()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()))
                .Returns(Task.Run(() => throw new SocketException()));

            var c = new Connection(ip, port, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.ConnectAsync());

            Assert.NotNull(ex);
            Assert.IsType<ConnectionException>(ex);
            Assert.IsType<SocketException>(ex.InnerException);

            t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect raises Connected event")]
        public async Task Connect_Raises_Connected_Event()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();
            var c = new Connection(ip, port, tcpClient: t.Object);

            var eventArgs = new List<EventArgs>();

            c.Connected += (sender, e) => eventArgs.Add(e);

            await c.ConnectAsync();

            Assert.Equal(ConnectionState.Connected, c.State);
            Assert.Single(eventArgs);

            t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect raises StateChanged event")]
        public async Task Connect_Raises_StateChanged_Event()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();
            var c = new Connection(ip, port, tcpClient: t.Object);

            var eventArgs = new List<ConnectionStateChangedEventArgs>();

            c.StateChanged += (sender, e) => eventArgs.Add(e);

            await c.ConnectAsync();

            Assert.Equal(ConnectionState.Connected, c.State);

            // the event will fire twice, once on transition to Connecting, and again on transition to Connected.
            Assert.Equal(2, eventArgs.Count);
            Assert.Equal(ConnectionState.Connecting, eventArgs[0].CurrentState);
            Assert.Equal(ConnectionState.Connected, eventArgs[1].CurrentState);

            t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Watchdog")]
        [Fact(DisplayName = "Watchdog disconnects when TcpClient disconnects")]
        public async Task Watchdog_Disconnects_When_TcpClient_Disconnects()
        {
            var ip = new IPAddress(0x0);
            var port = 1;

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(false);

            var c = new Connection(ip, port, tcpClient: t.Object);

            var disconnectRaisedByWatchdog = false;
            c.Disconnected += (sender, e) => disconnectRaisedByWatchdog = true;

            var timer = c.GetProperty<System.Timers.Timer>("WatchdogTimer");
            timer.Interval = 1;
            timer.Reset();

            await c.ConnectAsync();

            Assert.Equal(ConnectionState.Connected, c.State);

            var start = DateTime.UtcNow;

            while (!disconnectRaisedByWatchdog)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > 10000)
                {
                    throw new Exception("Watchdog didn't disconnect in 10000ms");
                }
            }

            Assert.True(disconnectRaisedByWatchdog);
            Assert.Equal(ConnectionState.Disconnected, c.State);

            t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "Write throws given null bytes")]
        public async Task Write_Throws_Given_Null_Bytes()
        {
            var c = new Connection(new IPAddress(0x0), 1);

            var ex = await Record.ExceptionAsync(async () => await c.WriteAsync(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "Write throws given zero bytes")]
        public async Task Write_Throws_Given_Zero_Bytes()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.WriteAsync(Array.Empty<byte>()));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "Write throws if TcpClient is not connected")]
        public async Task Write_Throws_If_TcpClient_Is_Not_Connected()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(false);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.WriteAsync(new byte[] { 0x0, 0x1 }));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "Write throws if connection is not connected")]
        public async Task Write_Throws_If_Connection_Is_Not_Connected()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.WriteAsync(new byte[] { 0x0, 0x1 }));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "Write throws if Stream throws")]
        public async Task Write_Throws_If_Stream_Throws()
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Throws(new SocketException());

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var ex = await Record.ExceptionAsync(async () => await c.WriteAsync(new byte[] { 0x0, 0x1 }));

            Assert.NotNull(ex);
            Assert.IsType<ConnectionWriteException>(ex);
            Assert.IsType<SocketException>(ex.InnerException);

            s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "Write does not throw given good input and if Stream does not throw")]
        public async Task Write_Does_Not_Throw_Given_Good_Input_And_If_Stream_Does_Not_Throw()
        {
            var s = new Mock<INetworkStream>();
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var ex = await Record.ExceptionAsync(async () => await c.WriteAsync(new byte[] { 0x0, 0x1 }));

            Assert.Null(ex);

            s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read throws if TcpClient is not connected")]
        public async Task Read_Throws_If_TcpClient_Is_Not_Connected()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(false);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(1));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read throws if connection is not connected")]
        public async Task Read_Throws_If_Connection_Is_Not_Connected()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(1));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read throws if length is long and larger than int")]
        public async Task Read_Throws_If_Length_Is_Long_And_Larger_Than_Int()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            long length = 2147483648; // max = 2147483647

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(length));

            Assert.NotNull(ex);
            Assert.IsType<NotImplementedException>(ex);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read does not throw if length is long and fits in int")]
        public async Task Read_Does_Not_Throw_If_Length_Is_Long_And_Fits_In_Int()
        {
            long length = 2147483647; // max = 2147483647

            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.Run(() => (int)length));

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(length));

            Assert.Null(ex);

            s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read does not throw given good input and if Stream does not throw")]
        public async Task Read_Does_Not_Throw_Given_Good_Input_And_If_Stream_Does_Not_Throw()
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(1));

            Assert.Null(ex);

            s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read loops over Stream.ReadAsync on partial read")]
        public async Task Read_Loops_Over_Stream_ReadAsync_On_Partial_Read()
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            await c.ReadAsync(3);

            s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read throws if Stream throws")]
        public async Task Read_Throws_If_Stream_Throws()
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Throws(new SocketException());

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(1));

            Assert.NotNull(ex);
            Assert.IsType<ConnectionReadException>(ex);
            Assert.IsType<SocketException>(ex.InnerException);

            s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read does not throw given zero length")]
        public async Task Read_Does_Not_Throw_Given_Zero_Length()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(0));

            Assert.Null(ex);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read returns empty byte array given zero length")]
        public async Task Read_Returns_Empty_Byte_Array_Given_Zero_Length()
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            var bytes = await c.ReadAsync(0);

            Assert.Empty(bytes);
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read throws given negative length")]
        [InlineData(-12151353)]
        [InlineData(-1)]
        public async Task Read_Throws_Given_Negative_Length(int length)
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);

            var ex = await Record.ExceptionAsync(async () => await c.ReadAsync(length));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read disconnects if Stream returns 0")]
        public async Task Read_Disconnects_If_Stream_Returns_0()
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.Run(() => 0));

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);
            await c.ConnectAsync();

            await c.ReadAsync(1);

            Assert.Equal(ConnectionState.Disconnected, c.State);

            s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read raises DataRead event")]
        public async Task Read_Raises_DataRead_Event()
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);

            var eventArgs = new List<ConnectionDataEventArgs>();

            c.DataRead += (sender, e) => eventArgs.Add(e);

            await c.ConnectAsync();

            await c.ReadAsync(3);

            Assert.Equal(3, eventArgs.Count);
            Assert.Equal(1, eventArgs[0].CurrentLength);
            Assert.Equal(3, eventArgs[0].TotalLength);
            Assert.Equal(2, eventArgs[1].CurrentLength);
            Assert.Equal(3, eventArgs[1].TotalLength);
            Assert.Equal(3, eventArgs[2].CurrentLength);
            Assert.Equal(3, eventArgs[2].TotalLength);

            s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
        }

        [Trait("Category", "Read")]
        [Fact(DisplayName = "Read times out on inactivity")]
        public async Task Read_Times_Out_On_Inactivity()
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);
            t.Setup(m => m.GetStream()).Returns(s.Object);

            var c = new Connection(new IPAddress(0x0), 1, tcpClient: t.Object);

            var timer = c.GetProperty<System.Timers.Timer>("InactivityTimer").Interval = 1;

            await c.ConnectAsync();
            await c.ReadAsync(1);

            Thread.Sleep(1000);

            Assert.Equal(ConnectionState.Disconnected, c.State);

            s.Verify(m => m.Close(), Times.Once);
        }
    }
}
