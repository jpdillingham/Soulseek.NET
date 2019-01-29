// <copyright file="LoginAsyncTests.cs" company="JP Dillingham">
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
    using Xunit;

    public class LoginAsyncTests
    {
        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "Login throws ArgumentException on null username")]
        public async Task Login_Throws_ArgumentException_On_Null_Username()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(null, Guid.NewGuid().ToString()));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "LoginAsync")]
        [Theory(DisplayName = "Login throws ArgumentException on bad input")]
        [InlineData(null, "a")]
        [InlineData("", "a")]
        [InlineData("a", null)]
        [InlineData("a", "")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public async Task Login_Throws_ArgumentException_On_Bad_Input(string username, string password)
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync(username, password));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "Login throws InvalidOperationException if logged in")]
        public async Task Login_Throws_InvalidOperationException_If_Logged_In()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync("a", "b"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "LoginAsync")]
        [Fact(DisplayName = "Login throws InvalidOperationException if not connected")]
        public async Task Login_Throws_InvalidOperationException_If_Not_Connected()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Disconnected);

            var ex = await Record.ExceptionAsync(async () => await s.LoginAsync("a", "b"));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }
    }
}
