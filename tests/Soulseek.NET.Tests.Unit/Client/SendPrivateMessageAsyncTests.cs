// <copyright file="SendPrivateMessageAsyncTests.cs" company="JP Dillingham">
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
    using System;
    using System.Threading.Tasks;
    using Moq;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Tcp;
    using Xunit;

    public class SendPrivateMessageAsyncTests
    {
        [Trait("Category", "AcknowledgePrivateMessageAsync")]
        [Fact(DisplayName = "AcknowledgePrivateMessageAsync throws InvalidOperationException when not connected")]
        public async Task AcknowledgePrivateMessageAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            var s = new SoulseekClient();

            var ex = await Record.ExceptionAsync(async () => await s.AcknowledgePrivateMessageAsync(1));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "AcknowledgePrivateMessageAsync")]
        [Fact(DisplayName = "AcknowledgePrivateMessageAsync throws InvalidOperationException when not logged in")]
        public async Task Acknowledge_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.AcknowledgePrivateMessageAsync(1));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "AcknowledgePrivateMessageAsync")]
        [Fact(DisplayName = "AcknowledgePrivateMessageAsync does not throw when write does not throw")]
        public async Task Acknowledge_Does_Not_Throw_When_Write_Does_Not_Throw()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.AcknowledgePrivateMessageAsync(1));

            Assert.Null(ex);
        }

        [Trait("Category", "AcknowledgePrivateMessageAsync")]
        [Fact(DisplayName = "AcknowledgePrivateMessageAsync throws PrivateMessageException when write throws")]
        public async Task Acknowledge_Throws_PrivateMessageException_When_Write_Throws()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Throws(new ConnectionWriteException());

            var s = new SoulseekClient("127.0.0.1", 1, serverConnection: conn.Object);
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.AcknowledgePrivateMessageAsync(1));

            Assert.NotNull(ex);
            Assert.IsType<PrivateMessageException>(ex);
            Assert.IsType<ConnectionWriteException>(ex.InnerException);
        }
    }
}
