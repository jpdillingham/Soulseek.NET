// <copyright file="SoulseekClientEventArgsTests.cs" company="JP Dillingham">
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

    public class SoulseekClientEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Class", "SoulseekClientStateChangedEventArgs")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void SoulseekClientStateChangedEventArgs_Instantiates_With_The_Given_Data(SoulseekClientStates previousState, SoulseekClientStates state, string message)
        {
            var s = new SoulseekClientStateChangedEventArgs(previousState, state, message);

            Assert.Equal(previousState, s.PreviousState);
            Assert.Equal(state, s.State);
            Assert.Equal(message, s.Message);
        }
    }
}
