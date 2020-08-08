// <copyright file="BrowseResponseTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class BrowseResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Instantiates with given data")]
        public void Instantiates_With_Given_Data()
        {
            var num = new Random().Next();
            var a = new BrowseResponse(num);

            Assert.Equal(num, a.DirectoryCount);
            Assert.Empty(a.Directories);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Instantiates with the given directory list")]
        public void Instantiates_With_The_Given_Directory_List()
        {
            var num = new Random().Next();

            var dir = new Directory("foo", 1);
            var list = new List<Directory>(new[] { dir });

            var a = new BrowseResponse(num, list);

            Assert.Equal(num, a.DirectoryCount);
            Assert.Single(a.Directories);
            Assert.Equal(dir, a.Directories.ToList()[0]);
        }
    }
}
