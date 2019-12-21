// <copyright file="MessageConnectionEventArgsTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Soulseek.Network;
    using Xunit;

    public class MessageConnectionEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "MessageDataReadEventArgs instantiates with the expected values"), AutoData]
        public void MessageDataReadEventArgs_Instantiates_With_The_Expected_Values(byte[] code, long current, long total)
        {
            var a = new MessageDataReadEventArgs(code, current, total);

            Assert.Equal(code, a.Code);
            Assert.Equal(current, a.CurrentLength);
            Assert.Equal(total, a.TotalLength);
            Assert.Equal((current / (double)total) * 100d, a.PercentComplete);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "MessageReadEventArgs instantiates with the expected values"), AutoData]
        public void MessageReadEventArgs_Instantiates_With_The_Expected_Values(byte[] message)
        {
            var a = new MessageReadEventArgs(message);

            Assert.Equal(message, a.Message);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "MessageReceivedEventArgs instantiates with the expected values"), AutoData]
        public void MessageReceivedEventArgs_Instantiates_With_The_Expected_Values(long length, byte[] code)
        {
            var a = new MessageReceivedEventArgs(length, code);

            Assert.Equal(code, a.Code);
            Assert.Equal(length, a.Length);
        }
    }
}