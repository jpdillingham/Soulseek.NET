// <copyright file="PeerQueueFailedResponse.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging.Responses
{
    using Soulseek.NET.Exceptions;

    public sealed class PeerQueueFailedResponse
    {
        #region Private Constructors

        private PeerQueueFailedResponse()
        {
        }

        #endregion Private Constructors

        #region Public Properties

        public string Filename { get; private set; }
        public string Message { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public static PeerQueueFailedResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerQueueFailed)
            {
                throw new MessageException($"Message Code mismatch creating Peer Queue Failed Response (expected: {(int)MessageCode.PeerQueueFailed}, received: {(int)reader.Code}.");
            }

            var response = new PeerQueueFailedResponse()
            {
                Filename = reader.ReadString(),
                Message = reader.ReadString(),
            };

            return response;
        }

        #endregion Public Methods
    }
}