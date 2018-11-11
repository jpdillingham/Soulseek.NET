// <copyright file="PierceFirewallRequest.cs" company="JP Dillingham">
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
    public class PierceFirewallRequest
    {
        #region Public Constructors

        public PierceFirewallRequest(int token)
        {
            Token = token;
        }

        #endregion Public Constructors

        #region Public Properties

        public int Token { get; set; }

        #endregion Public Properties

        #region Public Methods

        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .Code((byte)0x0)
                .WriteInteger(Token)
                .Build()
                .ToByteArray();
        }

        #endregion Public Methods
    }
}