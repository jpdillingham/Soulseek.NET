// <copyright file="MessageTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Messaging
{
    using Soulseek.NET.Messaging;
    using System;
    using Xunit;

    public class MessageTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates without exception given good data")]
        public void Instantiates_Without_Exception_Given_Good_Data()
        {
            var num = new Random().Next();
            var bytes = new MessageBuilder()
                .Code(MessageCode.ServerLogin)
                .WriteInteger(num)
                .Build()
                .ToByteArray();

            var msg = default(Message);

            var ex = Record.Exception(() => msg = new Message(bytes));

            Assert.Null(ex);
            Assert.NotNull(msg);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiate throws exception given null data")]
        public void Instantiate_Throws_Exception_Given_Null_Data()
        {
            var ex = Record.Exception(() => new Message(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiate throws exception given too short data")]
        public void Instantiate_Throws_Exception_Given_Too_Short_Data()
        {
            var ex = Record.Exception(() => new Message(new byte[] { 0x0, 0x1, 0x2, 0x3 }));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "ToByteArray")]
        [Fact(DisplayName = "ToByteArray returns given bytes")]
        public void ToByteArray_Returns_Given_Bytes()
        {
            var num = new Random().Next();
            var bytes = new MessageBuilder()
                .Code(MessageCode.ServerLogin)
                .WriteInteger(num)
                .Build()
                .ToByteArray();

            var msg = new Message(bytes);

            Assert.Equal(bytes, msg.ToByteArray());
        }

        [Trait("Category", "Properties")]
        [Fact(DisplayName = "Properties return expected data")]
        public void Properties_Return_Expected_Data()
        {
            var num = new Random().Next();
            var bytes = new MessageBuilder()
                .Code(MessageCode.ServerLogin)
                .WriteInteger(num)
                .Build()
                .ToByteArray();

            var msg = new Message(bytes);

            Assert.Equal(MessageCode.ServerLogin, msg.Code);
            Assert.Equal(8, msg.Length);
            Assert.Equal(BitConverter.GetBytes(num), msg.Payload);
        }
    }
}
