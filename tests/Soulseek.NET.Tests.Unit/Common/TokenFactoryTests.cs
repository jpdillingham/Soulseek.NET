// <copyright file="TokenFactoryTests.cs" company="JP Dillingham">
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

using Soulseek.NET.Common;
using Xunit;

namespace Soulseek.NET.Tests.Unit
{
    public class TokenFactoryTests
    {
        [Trait("Category", "GetToken")]
        [Fact(DisplayName = "Returns a token given no collision")]
        public void Returns_A_token_Given_No_Collision()
        {
            int t = 0;
            var ex = Record.Exception(() => t = TokenFactory.GetToken());

            Assert.Null(ex);
            Assert.NotEqual(0, t);
        }

        [Trait("Category", "GetToken")]
        [Fact(DisplayName = "Throws TimeoutException given forced collision")]
        public void Throws_TimeoutException_Given_forced_Collision()
        {
            int t = 0;
            var ex = Record.Exception(() => t = TokenFactory.GetToken(s => true));

            Assert.NotNull(ex);
            Assert.Equal(0, t);
        }
    }
}
