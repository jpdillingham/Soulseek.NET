// <copyright file="SearchQueryTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using Xunit;

    public class SearchQueryTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses query-only search text")]
        public void Parses_Query_Only_Search_Text()
        {
            var s = new SearchQuery("foo");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo", s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses exclusions")]
        public void Parses_Exclusions()
        {
            var s = new SearchQuery("foo -bar -baz");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo -bar -baz", s.SearchText);
            Assert.Equal(2, s.Exclusions.Count);
            Assert.Equal("bar", s.Exclusions.ToList()[0]);
            Assert.Equal("baz", s.Exclusions.ToList()[1]);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses exclusions out of order")]
        public void Parses_Exclusions_Out_Of_Order()
        {
            var s = new SearchQuery("-bar foo -baz");

            Assert.Equal("foo", s.Query);
            Assert.Equal("-bar foo -baz", s.SearchText);
            Assert.Equal(2, s.Exclusions.Count);
            Assert.Equal("bar", s.Exclusions.ToList()[0]);
            Assert.Equal("baz", s.Exclusions.ToList()[1]);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses releated exclusions singly")]
        public void Parses_Repeated_Exclusions_Singly()
        {
            var s = new SearchQuery("-bar foo -baz -baz -bar");

            Assert.Equal("foo", s.Query);
            Assert.Equal("-bar foo -baz -baz -bar", s.SearchText);
            Assert.Equal(2, s.Exclusions.Count);
            Assert.Equal("bar", s.Exclusions.ToList()[0]);
            Assert.Equal("baz", s.Exclusions.ToList()[1]);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Parses MinimumBitrate")]
        [InlineData("foo minbitrate:5", "foo", 5)]
        [InlineData("bar mbr:12", "bar", 12)]
        [InlineData("baz mbr:111 qux", "baz qux", 111)]
        [InlineData("mbr:99", "", 99)]
        [InlineData("foo MBR:7", "foo", 7)]
        public void Parses_MinimumBitrate(string rawQuery, string query, int value)
        {
            var s = new SearchQuery(rawQuery);

            Assert.Equal(query, s.Query);
            Assert.Equal(rawQuery, s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Equal(value, s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Discards MinimumBitrate when invalid")]
        public void Discards_MinimumBitrate_When_Invalid()
        {
            var s = new SearchQuery("foo mbr:bar");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo mbr:bar", s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Parses MinimumFileSize")]
        [InlineData("foo minfilesize:5", "foo", 5)]
        [InlineData("bar mfs:12", "bar", 12)]
        [InlineData("baz mfs:111 qux", "baz qux", 111)]
        [InlineData("mfs:99", "", 99)]
        [InlineData("foo MFS:7", "foo", 7)]
        public void Parses_MinimumFileSize(string rawQuery, string query, int value)
        {
            var s = new SearchQuery(rawQuery);

            Assert.Equal(query, s.Query);
            Assert.Equal(rawQuery, s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Equal(value, s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Discards MinimumFileSize when invalid")]
        public void Discards_MinimumFileSize_When_Invalid()
        {
            var s = new SearchQuery("foo mfs:bar");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo mfs:bar", s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Parses MinimumFilesInFolder")]
        [InlineData("foo minfilesinfolder:5", "foo", 5)]
        [InlineData("bar mfif:12", "bar", 12)]
        [InlineData("baz mfif:111 qux", "baz qux", 111)]
        [InlineData("mfif:99", "", 99)]
        [InlineData("foo MFIF:7", "foo", 7)]
        public void Parses_MinimumFilesInFolder(string rawQuery, string query, int value)
        {
            var s = new SearchQuery(rawQuery);

            Assert.Equal(query, s.Query);
            Assert.Equal(rawQuery, s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Equal(value, s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Discards MinimumFilesInFolder when invalid")]
        public void Discards_MinimumFilesInFolder()
        {
            var s = new SearchQuery("foo mfif:bar");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo mfif:bar", s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.False(s.IsVBR);
        }
    }
}
