// <copyright file="GlobalDiagnosticTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit
{
    using System;
    using Moq;
    using Soulseek.Diagnostics;
    using Xunit;

    public class GlobalDiagnosticTests
    {
        [Trait("Category", "GlobalDiagnostic")]
        [Fact]
        public void Does_Not_Throw_If_Uninitialized()
        {
            var ex = Record.Exception(() =>
            {
                GlobalDiagnostic.Debug("foo");
                GlobalDiagnostic.Debug("foo", new Exception("bar"));
                GlobalDiagnostic.Info("asdfasdfa");
                GlobalDiagnostic.Warning("warn");
                GlobalDiagnostic.Warning("asdf", new Exception("asdf"));
            });

            Assert.Null(ex);
        }

        [Trait("Category", "GlobalDiagnostic")]
        [Fact]
        public void Binds_Given_Factory()
        {
            var f = new Mock<IDiagnosticFactory>();

            GlobalDiagnostic.Init(f.Object);

            GlobalDiagnostic.Info("test");

            f.Verify(m => m.Info("test"), Times.Exactly(1));
        }
    }
}
