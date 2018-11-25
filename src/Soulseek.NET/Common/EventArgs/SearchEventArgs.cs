// <copyright file="SearchEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek.NET
{
    using System;
    using Soulseek.NET.Messaging.Responses;

    public class SearchEventArgs : EventArgs
    {
        internal SearchEventArgs(Search search)
        {
            SearchText = search.SearchText;
            Token = search.Token;
        }

        public string SearchText { get; private set; }
        public int Token { get; private set; }
    }

    public class SearchResponseReceivedEventArgs : SearchEventArgs
    {
        internal SearchResponseReceivedEventArgs(Search search, SearchResponse response)
            : base(search)
        {
            Response = response;
        }

        public SearchResponse Response { get; private set; }
    }

    public class SearchStateChangedEventArgs : SearchEventArgs
    {
        internal SearchStateChangedEventArgs(Search search)
            : base(search)
        {
            State = search.State;
        }

        public SearchState State { get; private set; }
    }
}
