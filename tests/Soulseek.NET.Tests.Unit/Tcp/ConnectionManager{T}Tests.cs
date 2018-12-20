// <copyright file="ConnectionManager{T}Tests.cs" company="JP Dillingham">
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
    using Xunit;

    public class ConnectionManager_T_Tests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            ConnectionManager<IConnection> c = null;

            var ex = Record.Exception(() => c = new ConnectionManager<IConnection>(1000));

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(1000, c.ConcurrentConnections);
            Assert.Equal(0, c.Active);
            Assert.Equal(0, c.Queued);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var c = new ConnectionManager<IConnection>();

            var ex = Record.Exception(() => c.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "Remove")]
        [Fact(DisplayName = "Removes does not throw on untracked connection")]
        public async void Removes_Does_Not_Throw_On_Untracked_Connection()
        {
            var mock = new Mock<IConnection>();
            mock.Setup(m => m.Key).Returns(new ConnectionKey(new System.Net.IPAddress(0x0), 1));

            var c = new ConnectionManager<IConnection>();

            var ex = await Record.ExceptionAsync(async () => await c.RemoveAsync(mock.Object));

            Assert.Null(ex);
        }

        [Trait("Category", "Remove")]
        [Fact(DisplayName = "Removes does not throw on null connection")]
        public async void Removes_Does_Not_Throw_On_Null_Connection()
        {
            var c = new ConnectionManager<IConnection>();

            var ex = await Record.ExceptionAsync(async () => await c.RemoveAsync(null));

            Assert.Null(ex);
        }

        [Trait("Category", "Remove")]
        [Fact(DisplayName = "Removes does not throw on null connection key")]
        public async void Removes_Does_Not_Throw_On_Null_Connection_Key()
        {
            var mock = new Mock<IConnection>();

            var c = new ConnectionManager<IConnection>();

            var ex = await Record.ExceptionAsync(async () => await c.RemoveAsync(mock.Object));

            Assert.Null(ex);
        }

        [Trait("Category", "Remove")]
        [Fact(DisplayName = "Removes does not dispose untracked connection")]
        public async void Removes_Does_Not_Dispose_Untracked_Connection()
        {
            var mock = new Mock<IConnection>();
            mock.Setup(m => m.Key).Returns(new ConnectionKey(new System.Net.IPAddress(0x0), 1));

            var c = new ConnectionManager<IConnection>();

            await c.RemoveAsync(mock.Object);

            mock.Verify(m => m.Dispose(), Times.Never);
        }
    }
}
