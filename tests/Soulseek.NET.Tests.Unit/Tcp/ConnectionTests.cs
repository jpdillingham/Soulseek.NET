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
    using Moq;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
    using System.Net;
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
    }
}
