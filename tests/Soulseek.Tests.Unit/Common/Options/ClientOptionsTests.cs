// <copyright file="ClientOptionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using AutoFixture.Xunit2;
    using Xunit;

    public class ClientOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiation(
            int concurrentPeerMessageConnectionLimit,
            int messageTimeout,
            bool autoAcknowledgePrivateMessages,
            DiagnosticLevel minimumDiagnosticLevel,
            int startingToken,
            ConnectionOptions serverConnectionOptions,
            ConnectionOptions peerConnectionOptions,
            ConnectionOptions transferConnectionOptions)
        {
            var o = new ClientOptions(
                concurrentPeerMessageConnectionLimit: concurrentPeerMessageConnectionLimit,
                messageTimeout: messageTimeout,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                minimumDiagnosticLevel: minimumDiagnosticLevel,
                startingToken: startingToken,
                serverConnectionOptions: serverConnectionOptions,
                peerConnectionOptions: peerConnectionOptions,
                transferConnectionOptions: transferConnectionOptions);

            Assert.Equal(concurrentPeerMessageConnectionLimit, o.ConcurrentPeerMessageConnectionLimit);
            Assert.Equal(messageTimeout, o.MessageTimeout);
            Assert.Equal(autoAcknowledgePrivateMessages, o.AutoAcknowledgePrivateMessages);
            Assert.Equal(minimumDiagnosticLevel, o.MinimumDiagnosticLevel);
            Assert.Equal(startingToken, o.StartingToken);
            Assert.Equal(serverConnectionOptions, o.ServerConnectionOptions);
            Assert.Equal(peerConnectionOptions, o.PeerConnectionOptions);
            Assert.Equal(transferConnectionOptions, o.TransferConnectionOptions);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with default options if null"), AutoData]
        public void Instantiation_Defaults_Options_If_Null(
            int concurrentPeerMessageConnectionLimit,
            int messageTimeout,
            bool autoAcknowledgePrivateMessages,
            DiagnosticLevel minimumDiagnosticLevel,
            int startingToken)
        {
            var o = new ClientOptions(
                concurrentPeerMessageConnectionLimit: concurrentPeerMessageConnectionLimit,
                messageTimeout: messageTimeout,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                minimumDiagnosticLevel: minimumDiagnosticLevel,
                startingToken: startingToken);

            Assert.NotNull(o.ServerConnectionOptions);
            Assert.NotNull(o.PeerConnectionOptions);
            Assert.NotNull(o.TransferConnectionOptions);
        }
    }
}
