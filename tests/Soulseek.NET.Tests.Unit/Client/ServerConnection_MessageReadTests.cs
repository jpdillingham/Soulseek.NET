// <copyright file="ServerConnection_MessageReadTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Client
{
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using System;
    using System.Net;
    using Xunit;

    public class ServerConnection_MessageReadTests
    {
        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates diagnostic on message")]
        public void Creates_Diagnostic_On_Message()
        {
            var diagnostic = new Mock<IDiagnosticFactory>();
            diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var message = new MessageBuilder()
                .Code(MessageCode.ServerParentMinSpeed)
                .WriteInteger(1)
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, diagnosticFactory: diagnostic.Object);

            s.InvokeMethod("ServerConnection_MessageRead", null, message);

            diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates unhandled diagnostic on unhandled message")]
        public void Creates_Unhandled_Diagnostic_On_Unhandled_Message()
        {
            string msg = null;

            var diagnostic = new Mock<IDiagnosticFactory>();
            diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(m => msg = m);

            var message = new MessageBuilder().Code(MessageCode.ServerPrivateRoomOwned).Build();

            var s = new SoulseekClient("127.0.0.1", 1, diagnosticFactory: diagnostic.Object);

            s.InvokeMethod("ServerConnection_MessageRead", null, message);

            diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Exactly(2));

            Assert.Contains("Unhandled", msg, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerGetPeerAddress"), AutoData]
        public void Handles_ServerGetPeerAddress(string username, IPAddress ip, int port)
        {
            GetPeerAddressResponse result = null;

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<GetPeerAddressResponse>()))
                .Callback<WaitKey, GetPeerAddressResponse>((key, response) => result = response);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var message = new MessageBuilder()
                .Code(MessageCode.ServerGetPeerAddress)
                .WriteString(username)
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, messageWaiter: waiter.Object);

            s.InvokeMethod("ServerConnection_MessageRead", null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(ip, result.IPAddress);
            Assert.Equal(port, result.Port);
        }
    }
}
