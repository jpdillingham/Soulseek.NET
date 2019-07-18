// <copyright file="ServerMessageHandlerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Handlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class PeerMessageHandlerTests
    {
        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on message"), AutoData]
        public void Creates_Diagnostic_On_Message(string username, IPAddress ip, int port)
        {
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(msg));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentMinSpeed)
                .WriteInteger(1)
                .Build();

            handler.HandleMessage(mocks.Connection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("peer message received", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        private (PeerMessageHandler Handler, Mocks Mocks) GetFixture(string username = null, IPAddress ip = null, int port = 0)
        {
            var mocks = new Mocks();

            mocks.Connection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.Connection.Setup(m => m.IPAddress)
                .Returns(ip ?? IPAddress.Parse("0.0.0.0"));
            mocks.Connection.Setup(m => m.Port)
                .Returns(port);

            var handler = new PeerMessageHandler(
                mocks.Client.Object,
                mocks.Waiter.Object,
                mocks.Downloads,
                mocks.Searches,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private class Mocks
        {
            public Mock<ISoulseekClient> Client { get; } = new Mock<ISoulseekClient>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public ConcurrentDictionary<int, Transfer> Downloads { get; } = new ConcurrentDictionary<int, Transfer>();
            public ConcurrentDictionary<int, Search> Searches { get; } = new ConcurrentDictionary<int, Search>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<IMessageConnection> Connection { get; } = new Mock<IMessageConnection>();
        }
    }
}
