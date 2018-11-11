// <copyright file="PeerTransferRequest.cs" company="JP Dillingham">
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
    public class PeerTransferRequest
    {
        #region Public Constructors

        public PeerTransferRequest(TransferDirection direction, int token, string filename, int size = 0)
        {
            Direction = direction;
            Token = token;
            Filename = filename;
            Size = size;
        }

        #endregion Public Constructors

        #region Public Properties

        public TransferDirection Direction { get; set; }
        public string Filename { get; set; }
        public int Size { get; set; }
        public int Token { get; set; }

        #endregion Public Properties

        #region Public Methods

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.PeerTransferRequest)
                .WriteInteger((int)Direction)
                .WriteInteger(Token)
                .WriteString(Filename)
                .WriteInteger(Size)
                .Build();
        }

        #endregion Public Methods
    }
}