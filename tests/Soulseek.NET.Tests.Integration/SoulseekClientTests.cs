// <copyright file="SoulseekClientTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Integration
{
    using System.Threading.Tasks;
    using Xunit;

    public class SoulseekClientTests
    {
        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client connects")]
        public async Task Client_Connects()
        {
            var client = new SoulseekClient();

            var ex = await Record.ExceptionAsync(() => client.ConnectAsync());

            Assert.Null(ex);
            Assert.Equal(SoulseekClientStates.Connected, client.State);
        }

        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client disconnects")]
        public async Task Client_Disconnects()
        {
            var client = new SoulseekClient();
            await client.ConnectAsync();

            var ex = Record.Exception(() => client.Disconnect());

            Assert.Null(ex);
            Assert.Equal(SoulseekClientStates.Disconnected, client.State);
        }
    }
}
