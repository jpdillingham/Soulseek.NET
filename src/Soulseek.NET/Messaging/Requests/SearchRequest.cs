// <copyright file="SearchRequest.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging.Requests
{
    public class SearchRequest
    {
        public SearchRequest(string searchText, int ticket)
        {
            SearchText = searchText;
            Ticket = ticket;
        }

        public string SearchText { get; set; }
        public int Ticket { get; set; }

        internal Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerFileSearch)
                .WriteInteger(Ticket)
                .WriteString(SearchText)
                .Build();
        }
    }
}
