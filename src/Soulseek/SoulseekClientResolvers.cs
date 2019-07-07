// <copyright file="SoulseekClientResolvers.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Soulseek.Messaging.Messages;

    public class SoulseekClientResolvers
    {
        private readonly Func<string, IPAddress, int, string, int, SearchResponse> defaultSearchResponse =
            (u, i, p, s, t) => new SearchResponse(u, t, 0, 0, 0, 0);

        private readonly Func<string, IPAddress, int, BrowseResponse> defaultBrowseResponse =
            (u, i, p) => new BrowseResponse(0, new List<Directory>());

        private readonly Func<string, IPAddress, int, UserInfoResponse> defaultUserInfoResponse =
            (u, i, p) => new UserInfoResponse(string.Empty, false, null, 0, 0, false);

        private readonly Func<string, IPAddress, int, string, (bool Allowed, string Message)> defaultQueueDownloadResponse =
            (u, i, p, f) => (true, null);

        public SoulseekClientResolvers(
            Func<string, IPAddress, int, string, int, SearchResponse> searchResponse = null,
            Func<string, IPAddress, int, BrowseResponse> browseResponse = null,
            Func<string, IPAddress, int, UserInfoResponse> userInfoResponse = null,
            Func<string, IPAddress, int, string, (bool Allowed, string Message)> queueDownloadResponse = null)
        {
            SearchResponse = searchResponse ?? defaultSearchResponse;
            BrowseResponse = browseResponse ?? defaultBrowseResponse;
            UserInfoResponse = userInfoResponse ?? defaultUserInfoResponse;
            QueueDownloadResponse = queueDownloadResponse ?? defaultQueueDownloadResponse;
        }

        public Func<string, IPAddress, int, string, int, SearchResponse> SearchResponse { get; }
        public Func<string, IPAddress, int, BrowseResponse> BrowseResponse { get; }
        public Func<string, IPAddress, int, UserInfoResponse> UserInfoResponse { get; }
        public Func<string, IPAddress, int, string, (bool Allowed, string Message)> QueueDownloadResponse { get; }
    }
}