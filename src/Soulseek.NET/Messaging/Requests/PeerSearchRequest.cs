// <copyright file="PeerSearchRequest.cs" company="JP Dillingham">
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
    public class PeerSearchRequest
    {
        #region Public Constructors

        public PeerSearchRequest(int token, string searchText)
        {
            Token = token;
            SearchText = searchText;
        }

        #endregion Public Constructors

        #region Public Properties

        public string SearchText { get; set; }
        public int Token { get; set; }

        #endregion Public Properties

        #region Public Methods

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.PeerSearchRequest)
                .WriteInteger(Token)
                .WriteString(SearchText)
                .Build();
        }

        #endregion Public Methods
    }
}