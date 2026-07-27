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
    using System.Threading.Tasks;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Network.Tcp;
    using Xunit;

    [Collection(nameof(GlobalDiagnosticTests))]
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

            var l = new Listener(IPAddress.Any, port, options);

            Assert.Equal(IPAddress.Any, l.IPAddress);
            Assert.Equal(port, l.Port);
            Assert.Equal(options, l.ConnectionOptions);

            Assert.False(l.Listening);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start starts listening")]
        public void Start_Starts_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var l = new Listener(IPAddress.Any, port, options);

            var first = l.Listening;

            l.Start();

            Assert.False(first);
            Assert.True(l.Listening);
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
    }
}
