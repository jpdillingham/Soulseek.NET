// <copyright file="SearchResponder.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Responds to search requests.
    /// </summary>
    internal class SearchResponder : ISearchResponder
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponder"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public SearchResponder(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Responds to the given search request, if a response could be resolved and matche(s) were found.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The token for the search request.</param>
        /// <param name="query">The search query.</param>
        /// <returns>The operation context, including a value indicating whether a response was successfully sent.</returns>
        public async Task<bool> TrySendSearchResponse(string username, int token, string query)
        {
            if (SoulseekClient.Options.SearchResponseResolver == default)
            {
                return false;
            }

            SearchResponse searchResponse = null;

            try
            {
                searchResponse = await SoulseekClient.Options.SearchResponseResolver(username, token, SearchQuery.FromText(query)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error resolving search response for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
                return false;
            }

            if (searchResponse == null || searchResponse.FileCount + searchResponse.LockedFileCount <= 0)
            {
                return false;
            }

            try
            {
                Diagnostic.Debug($"Resolved {searchResponse.FileCount} files for query '{query}' with token {token} from {username}");

                var endpoint = await SoulseekClient.GetUserEndPointAsync(username).ConfigureAwait(false);

                var peerConnection = await SoulseekClient.PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, CancellationToken.None).ConfigureAwait(false);
                await peerConnection.WriteAsync(searchResponse.ToByteArray()).ConfigureAwait(false);

                Diagnostic.Debug($"Sent response containing {searchResponse.FileCount} files to {username} for query '{query}' with token {token}");

                return true;
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Failed to send search response for {query} to {username}: {ex.Message}", ex);
            }

            return false;
        }
    }
}