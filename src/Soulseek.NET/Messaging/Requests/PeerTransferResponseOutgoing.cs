// <copyright file="PeerTransferResponseOutgoing.cs" company="JP Dillingham">
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
    public class PeerTransferResponseOutgoing
    {
        #region Public Constructors

        public PeerTransferResponseOutgoing(int token, bool allowed, int fileSize, string message)
        {
            Token = token;
            Allowed = allowed;
            FileSize = fileSize;
            Message = message;
        }

        #endregion Public Constructors

        #region Public Properties

        public int Token { get; private set; }
        public bool Allowed { get; private set; }
        public int FileSize { get; private set; }
        public string Message { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.PeerTransferResponse)
                .WriteInteger(Token)
                .WriteByte((byte)(Allowed ? 1 : 0))
                .WriteInteger(FileSize)
                .Build();
        }

        #endregion Public Methods
    }
}