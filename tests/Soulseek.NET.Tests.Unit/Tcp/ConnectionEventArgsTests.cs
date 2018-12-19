// <copyright file="ConnectionEventArgsTests.cs" company="JP Dillingham">
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
    using Soulseek.NET.Tcp;
    using Xunit;

    public class ConnectionEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ConnectionDataEventArgs instantiates properly")]
        public void ConnectionDataEventArgs_Instantiates_Properly()
        {
            var data = new byte[] { 0x0, 0x1, 0x3 };

            var d = new ConnectionDataEventArgs(data, data.Length, 20);

            Assert.Equal(data, d.Data);
            Assert.Equal(3, d.CurrentLength);
            Assert.Equal(20, d.TotalLength);
            Assert.Equal(15d, d.PercentComplete);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ConnectionStateChangedEventArgs instantiates properly")]
        public void ConnectionStateChangedEventArgs_Instantiates_Properly()
        {
            var s = new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected, "foo");

            Assert.Equal(ConnectionState.Connected, s.PreviousState);
            Assert.Equal(ConnectionState.Disconnected, s.CurrentState);
            Assert.Equal("foo", s.Message);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ConnectionStateChangedEventArgs message is null if omitted")]
        public void ConnectionStateChangedEventArgs_Message_Is_Null_If_Omitted()
        {
            var s = new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected);

            Assert.Equal(ConnectionState.Connected, s.PreviousState);
            Assert.Equal(ConnectionState.Disconnected, s.CurrentState);
            Assert.Null(s.Message);
        }
    }
}
