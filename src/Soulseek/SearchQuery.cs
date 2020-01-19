// <copyright file="SearchQuery.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    ///     A search query.
    /// </summary>
    public class SearchQuery
    {
        private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchQuery"/> class.
        /// </summary>
        /// <param name="query">The query text.</param>
        /// <param name="exclusions">The list of excluded terms.</param>
        /// <param name="minimumBitrate">The minimum desired bitrate.</param>
        /// <param name="minimumFileSize">The minimum desired file size.</param>
        /// <param name="minimumFilesInFolder">The minimum desired number of files in the containing folder.</param>
        /// <param name="isVBR">A value indicating whether to restrict the search to variable bit rate files.</param>
        /// <param name="isCBR">A value indicating whehter to restrict the search to constant bit rate files.</param>
        public SearchQuery(string query, IEnumerable<string> exclusions, int? minimumBitrate, int? minimumFileSize, int? minimumFilesInFolder, bool isVBR, bool isCBR)
        {
            Query = query;
            ExclusionList = exclusions;
            MinimumBitrate = minimumBitrate;
            MinimumFileSize = minimumFileSize;
            MinimumFilesInFolder = minimumFilesInFolder;
            IsVBR = isVBR;
            IsCBR = isCBR;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchQuery"/> class.
        /// </summary>
        /// <param name="searchText">The full search text of the query.</param>
        public SearchQuery(string searchText)
        {
            RawSearchText = searchText;

            IEnumerable<string> tokens = searchText.Split(' ').ToList();

            var excludedTokens = tokens.Where(t => t.StartsWith("-", IgnoreCase));
            tokens = tokens.Except(excludedTokens);

            ExclusionList = excludedTokens.Select(t => t.TrimStart('-')).Distinct();

            var filters = new[] { "isvbr", "iscbr", "minbitrate:", "mbr:", "minfilesize:", "mfs:", "minfilesinfolder:", "mfif:" };

            var filterTokens = tokens.Where(t =>
                filters.Any(f => t.StartsWith(f, IgnoreCase)));

            Query = string.Join(" ", tokens.Except(filterTokens));
            MinimumBitrate = GetFilterValue(filterTokens, "minbitrate:", "mbr:");
            MinimumFileSize = GetFilterValue(filterTokens, "minfilesize:", "mfs:");
            MinimumFilesInFolder = GetFilterValue(filterTokens, "minfilesinfolder:", "mfif:");
            IsVBR = filterTokens.Any(t => t.Equals("isvbr", IgnoreCase));
            IsCBR = filterTokens.Any(t => t.Equals("iscbr", IgnoreCase));
        }

        /// <summary>
        ///     Gets the list of excluded terms.
        /// </summary>
        public IReadOnlyCollection<string> Exclusions => ExclusionList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets a value indicating whether to restrict the search to constant bit rate files.
        /// </summary>
        public bool IsCBR { get; }

        /// <summary>
        ///     Gets a value indicating whether to restrict the search to variable bit rate files.
        /// </summary>
        public bool IsVBR { get; }

        /// <summary>
        ///     Gets the minimum desired bitrate.
        /// </summary>
        public int? MinimumBitrate { get; }

        /// <summary>
        ///     Gets the minimum desired number of files in the containing folder.
        /// </summary>
        public int? MinimumFilesInFolder { get; }

        /// <summary>
        ///     Gets the minimum desired file size.
        /// </summary>
        public int? MinimumFileSize { get; }

        /// <summary>
        ///     Gets the query text.
        /// </summary>
        public string Query { get; }

        /// <summary>
        ///     Gets the full search text.
        /// </summary>
        public string SearchText => RawSearchText ?? ToString();

        private IEnumerable<string> ExclusionList { get; }
        private string RawSearchText { get; }

        /// <summary>
        ///     Returns a new instance of <see cref="SearchQuery"/> from the specified search text.
        /// </summary>
        /// <param name="searchText">The text from which to create the query.</param>
        /// <returns>The new SearchQuery instance.</returns>
        public static SearchQuery FromText(string searchText) => new SearchQuery(searchText);

        /// <summary>
        ///     Returns the full search text.
        /// </summary>
        /// <returns>The full search text.</returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Query);
            builder.Append(Exclusions.Any() ? " " + string.Join(" ", Exclusions.Select(e => $"-{e}")) : string.Empty);
            builder.Append(MinimumBitrate.HasValue ? $" mbr:{MinimumBitrate.Value}" : string.Empty);
            builder.Append(MinimumFileSize.HasValue ? $" mfs:{MinimumFileSize.Value}" : string.Empty);
            builder.Append(MinimumFilesInFolder.HasValue ? $" mfif:{MinimumFilesInFolder.Value}" : string.Empty);
            builder.Append(IsVBR ? " isvbr" : string.Empty);
            builder.Append(IsCBR ? " iscbr" : string.Empty);

            return builder.ToString();
        }

        private int? GetFilterValue(IEnumerable<string> tokens, params string[] prefixes)
        {
            try
            {
                var firstToken = tokens.FirstOrDefault(token => prefixes.Any(prefix => token.StartsWith(prefix, IgnoreCase)));
                return int.Parse(firstToken.Split(':')[1], CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}