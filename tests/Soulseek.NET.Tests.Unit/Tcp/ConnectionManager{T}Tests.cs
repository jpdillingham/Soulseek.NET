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
    }
}
