// <copyright file="SoulseekClientOptionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit
{
    using AutoFixture.Xunit2;
    using Xunit;

    public class SoulseekClientOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiation(
            int concurrentPeerConnections,
            int messageTimeout,
            bool autoAcknowledgePrivateMessages)
        {
            var o = new SoulseekClientOptions(concurrentPeerConnections, messageTimeout, autoAcknowledgePrivateMessages);

            Assert.Equal(concurrentPeerConnections, o.ConcurrentPeerConnections);
            Assert.Equal(messageTimeout, o.MessageTimeout);
            Assert.Equal(autoAcknowledgePrivateMessages, o.AutoAcknowledgePrivateMessages);

            Assert.NotNull(o.ServerConnectionOptions);
            Assert.NotNull(o.PeerConnectionOptions);
            Assert.NotNull(o.TransferConnectionOptions);
        }
    }
}
