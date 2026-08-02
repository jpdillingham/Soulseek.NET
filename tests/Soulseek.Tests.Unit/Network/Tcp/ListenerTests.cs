// <copyright file="ListenerTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit.Network.Tcp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ListenerTests
    {
        private static readonly Random RNG = new Random();

        private static int GetPort()
        {
            return 50000 + RNG.Next(1, 9999);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            Assert.Equal(IPAddress.Any, l.IPAddress);
            Assert.Equal(port, l.Port);
            Assert.Equal(options, l.ConnectionOptions);

            Assert.False(l.Listening);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Uses default ConnectionOptions if none supplied")]
        public void Uses_Default_ConnectionOptions_If_None_Supplied()
        {
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, connectionOptions: null, tcpListener.Object);

            Assert.NotNull(l.ConnectionOptions);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Uses supplied TcpListener")]
        public void Uses_Supplied_TcpListener()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var listener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener: listener.Object);

            var val = l.GetProperty<ITcpListener>("TcpListener");

            Assert.Equal(listener.Object, val);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Creates TcpListener if none supplied")]
        public void Creates_TcpListener_If_None_Supplied()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var l = new Listener(IPAddress.Any, port, options);

            var val = l.GetProperty<ITcpListener>("TcpListener");

            Assert.NotNull(val);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start starts listening")]
        public void Start_Starts_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            var first = l.Listening;

            l.Start();

            Assert.False(first);
            Assert.True(l.Listening);

            tcpListener.Verify(m => m.Start(It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start starts TcpListener")]
        public void Start_Starts_TcpListner()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            tcpListener.Verify(m => m.Start(It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start raises Started when starting")]
        public void Start_Raises_Started_When_Starting()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            var raised = false;
            l.Started += (sender, args) => raised = true;

            l.Start();

            Assert.True(raised);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start does not raise Started if already listening")]
        public void Start_Does_Not_Raise_Started_If_Already_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            var raised = false;
            l.Started += (sender, args) => raised = true;

            l.Start();

            Assert.False(raised);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start does not start listener if already listening")]
        public void Start_Does_Not_Start_Listener_If_Already_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();
            l.Start();

            tcpListener.Verify(m => m.Start(It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start does not throw when Started is unbound")]
        public void Start_Does_Not_Throw_When_Started_Is_Unbound()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            var ex = Record.Exception(() => l.Start());

            Assert.Null(ex);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start does not throw when Started throws")]
        public void Start_Does_Not_Throw_When_Started_Throws()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Started += (_, e) => throw new Exception();

            var ex = Record.Exception(() => l.Start());

            Assert.Null(ex);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start stops the listener if an exception is encountered")]
        public void Start_Stops_The_Listener_If_An_Exception_Is_Encountered()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.Start(It.IsAny<int>()))
                .Throws(new SocketException());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            var startedRaised = false;
            l.Started += (sender, args) => startedRaised = true;

            var ex = Record.Exception(() => l.Start());

            Assert.NotNull(ex);
            Assert.False(l.Listening);
            Assert.False(startedRaised);

            // Stop() is invoked to unblock any pending AcceptTcpClientAsync() call, even though Start() never succeeded
            tcpListener.Verify(m => m.Stop(), Times.Once);
        }

        [Trait("Category", "Stop")]
        [Fact(DisplayName = "Stop stops listening")]
        public void Stop_Stops_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var l = new Listener(IPAddress.Any, port, options);

            l.Start();

            var first = l.Listening;

            l.Stop();

            Assert.True(first);
            Assert.False(l.Listening);
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop continues if AcceptTcpClientAsync throws")]
        public async Task Accept_Loop_Continues_If_AcceptTcpClientAsync_Throws()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(new SocketException());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            // if the exception thrown by AcceptTcpClientAsync() escaped the loop, it would only ever be called once
            tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.AtLeast(2));
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop continues if the accepted connection dispatch throws")]
        public async Task Accept_Loop_Continues_If_Accepted_Dispatch_Throws()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();

            // an unconnected TcpClient throws when its RemoteEndPoint is accessed; this happens inside the
            // fire-and-forget Task.Run() used to dispatch the Accepted event, not in the loop's try/catch
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ReturnsAsync(() => new TcpClient());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            // if the exception thrown while dispatching the accepted connection escaped and killed the loop,
            // AcceptTcpClientAsync() would only ever be called once
            tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.AtLeast(2));
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop continues if AcceptTcpClientAsync throws a non-socket exception")]
        public async Task Accept_Loop_Continues_If_AcceptTcpClientAsync_Throws_Non_Socket_Exception()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();

            // the catch block should be broad enough to handle any exception, not just SocketException
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(new InvalidOperationException("boom"));

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.AtLeast(2));
        }

        [Trait("Category", "Stop")]
        [Fact(DisplayName = "Stop halts the accept loop")]
        public async Task Stop_Halts_The_Accept_Loop()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(new SocketException());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            // give any in-flight iteration a chance to finish and the loop to observe Listening == false
            await Task.Delay(200);

            var countAfterStop = tcpListener.Invocations.Count;

            await Task.Delay(200);

            // no further calls should occur once the loop has actually exited
            Assert.Equal(countAfterStop, tcpListener.Invocations.Count);
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop raises Accepted with the accepted connection")]
        public async Task Accept_Loop_Raises_Accepted_With_Accepted_Connection()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            // a real, connected loopback pair is required here; an unconnected TcpClient throws
            // when its RemoteEndPoint is accessed, so a mock TcpClient won't do
            var serverListener = new TcpListener(IPAddress.Loopback, 0);
            serverListener.Start();
            var serverPort = ((IPEndPoint)serverListener.LocalEndpoint).Port;

            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync(IPAddress.Loopback, serverPort);
                var acceptedClient = await serverListener.AcceptTcpClientAsync();
                await connectTask;

                serverListener.Stop();

                var callCount = 0;

                var tcpListener = new Mock<ITcpListener>();
                tcpListener.Setup(m => m.AcceptTcpClientAsync())
                    .Returns(() =>
                    {
                        // hand back the real connection once, then fail fast on every subsequent call so the
                        // loop spins harmlessly (and quickly) until Stop() is called
                        if (Interlocked.Increment(ref callCount) == 1)
                        {
                            return Task.FromResult(acceptedClient);
                        }

                        return Task.FromException<TcpClient>(new ObjectDisposedException(nameof(TcpListener)));
                    });

                var tcs = new TaskCompletionSource<IConnection>();

                var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);
                l.Accepted += (sender, connection) => tcs.TrySetResult(connection);

                l.Start();

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));

                l.Stop();

                Assert.Same(tcs.Task, completed);

                var raised = await tcs.Task;

                Assert.NotNull(raised);
                Assert.Equal(((IPEndPoint)acceptedClient.Client.RemoteEndPoint).Address, raised.IPEndPoint.Address);
            }
        }
    }
}
