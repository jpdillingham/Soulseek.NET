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
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchQueryTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given values"), AutoData]
        public void Instantiates_With_Given_Values(string query, IEnumerable<string> exclusions, int? mbr, int? mfs, int? mfif, bool cbr, bool vbr)
        {
            var s = new SearchQuery(query, exclusions, mbr, mfs, mfif, vbr, cbr);

            Assert.Equal(query, s.Query);
            Assert.Equal(exclusions, s.Exclusions);
            Assert.Equal(mbr, s.MinimumBitrate);
            Assert.Equal(mfs, s.MinimumFileSize);
            Assert.Equal(mfif, s.MinimumFilesInFolder);
            Assert.Equal(cbr, s.IsCBR);
            Assert.Equal(vbr, s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Constructs expected search text")]
        [InlineData("foo", new[] { "bar", "baz" }, 1, 2, 3, true, true, "foo -bar -baz mbr:1 mfs:2 mfif:3 isvbr iscbr")]
        [InlineData("foo", new string[0], 1, 2, 3, true, true, "foo mbr:1 mfs:2 mfif:3 isvbr iscbr")]
        [InlineData("foo", new string[0], null, 2, 3, true, true, "foo mfs:2 mfif:3 isvbr iscbr")]
        [InlineData("foo", new string[0], 1, null, 3, true, true, "foo mbr:1 mfif:3 isvbr iscbr")]
        [InlineData("foo", new string[0], 1, 2, null, true, true, "foo mbr:1 mfs:2 isvbr iscbr")]
        [InlineData("foo", new string[0], null, null, null, true, false, "foo isvbr")]
        [InlineData("foo", new string[0], null, null, null, false, true, "foo iscbr")]
        [InlineData("foo", new string[0], null, null, null, false, false, "foo")]
        [InlineData("foo", new[] { "bar" }, null, null, null, false, false, "foo -bar")]
        public void Constructs_Expected_Search_Text(string query, string[] exclusions, int? mbr, int? mfs, int? mfif, bool cbr, bool vbr, string expected)
        {
            var s = new SearchQuery(query, exclusions, mbr, mfs, mfif, cbr, vbr);

            Assert.Equal(expected, s.SearchText);
        }

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

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Parses IsCBR")]
        [InlineData("foo iscbr", "foo", true)]
        [InlineData("foo", "foo", false)]
        [InlineData("foo iscbr iscbr", "foo", true)]
        [InlineData("iscbr foo", "foo", true)]
        public void Parses_IsCBR(string rawQuery, string query, bool value)
        {
            var s = new SearchQuery(rawQuery);

            Assert.Equal(query, s.Query);
            Assert.Equal(rawQuery, s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.Equal(value, s.IsCBR);
            Assert.False(s.IsVBR);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Parses IsVBR")]
        [InlineData("foo isvbr", "foo", true)]
        [InlineData("foo", "foo", false)]
        [InlineData("foo isvbr isvbr", "foo", true)]
        [InlineData("isvbr foo", "foo", true)]
        public void Parses_IsVBR(string rawQuery, string query, bool value)
        {
            var s = new SearchQuery(rawQuery);

            Assert.Equal(query, s.Query);
            Assert.Equal(rawQuery, s.SearchText);
            Assert.Empty(s.Exclusions);
            Assert.Null(s.MinimumBitrate);
            Assert.Null(s.MinimumFileSize);
            Assert.Null(s.MinimumFilesInFolder);
            Assert.False(s.IsCBR);
            Assert.Equal(value, s.IsVBR);
        }

        [Trait("Category", "FromText")]
        [Theory(DisplayName ="FromText returns new instance from given text"), AutoData]
        public void FromText_Returns_New_Instance_From_Given_Text(string searchText)
        {
            var s = SearchQuery.FromText(searchText);

            Assert.Equal(searchText, s.SearchText);
        }
    }
}
