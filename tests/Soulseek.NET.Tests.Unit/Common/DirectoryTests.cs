using AutoFixture.Xunit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Soulseek.NET.Tests.Unit.Common
{
    public class DirectoryTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string directoryname, int fileCount)
        {
            var d = new Directory(directoryname, fileCount);

            Assert.Equal(directoryname, d.Directoryname);
            Assert.Equal(fileCount, d.FileCount);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with empty File list given no list"), AutoData]
        public void Instantiates_With_Empty_File_List_Given_No_List(string directoryname, int fileCount)
        {
            var d = new Directory(directoryname, fileCount);

            Assert.NotNull(d.Files);
            Assert.Empty(d.Files);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given File list given list"), AutoData]
        public void Instantiates_With_Given_File_List_Given_List(string directoryname, int fileCount)
        {
            var files = new List<File>() { new File(1, "a", 2, "b", 0) };

            var d = new Directory(directoryname, fileCount, files);

            Assert.NotNull(d.Files);
            Assert.Single(d.Files);
            Assert.Equal(files[0], d.Files.ToList()[0]);
        }
    }
}
