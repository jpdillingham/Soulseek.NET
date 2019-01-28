// <copyright file="DiagnosticFactoryTests.cs" company="JP Dillingham">
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

using AutoFixture.Xunit2;
using System;
using Xunit;

namespace Soulseek.NET.Tests.Unit
{
    public class DiagnosticFactoryTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(object source, DiagnosticLevel level, EventHandler<DiagnosticGeneratedEventArgs> handler)
        {
            var d = new DiagnosticFactory(source, level, handler);

            Assert.Equal(source, d.GetProperty<object>("Source"));
            Assert.Equal(level, d.GetProperty<DiagnosticLevel>("MinimumLevel"));
            Assert.Equal(handler, d.GetProperty<EventHandler<DiagnosticGeneratedEventArgs>>("EventHandler"));
        }

        [Trait("Category", "Debug")]
        [Theory(DisplayName = "Raises event on debug"), AutoData]
        public void Raises_Event_On_Debug(string message)
        {
            DiagnosticGeneratedEventArgs e = null;

            var d = new DiagnosticFactory(this, DiagnosticLevel.Debug, (sender, args) =>
            {
                e = args;
            });

            d.Debug(message);

            Assert.Equal(message, e.Message);
            Assert.Equal(DiagnosticLevel.Debug, e.Level);
            Assert.False(e.IncludesException);
            Assert.Null(e.Exception);
        }

        [Trait("Category", "Debug")]
        [Theory(DisplayName = "Does not raise event on debug when level is > Debug"), AutoData]
        public void Does_Not_Raise_Event_On_Debug_When_Level_Is_Gt_Debug(string message)
        {
            DiagnosticGeneratedEventArgs e = null;

            var d = new DiagnosticFactory(this, DiagnosticLevel.Info, (sender, args) =>
            {
                e = args;
            });

            d.Debug(message);

            Assert.Null(e);
        }
    }
}
