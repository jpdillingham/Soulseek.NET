// <copyright file="UserInfoResponse.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using Soulseek.Exceptions;

    /// <summary>
    ///     The response to a user info request.
    /// </summary>
    public sealed class UserInfoResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfoResponse"/> class.
        /// </summary>
        /// <param name="description">The peer's description.</param>
        /// <param name="hasPicture">A value indicating whether a picture has been configured.</param>
        /// <param name="picture">If configured, the picture data.</param>
        /// <param name="uploadSlots">The number of configured upload slots.</param>
        /// <param name="queueLength">The current queue length.</param>
        /// <param name="hasFreeUploadSlot">A value indicating whether an upload slot is free.</param>
        internal UserInfoResponse(string description, bool hasPicture, byte[] picture, int uploadSlots, int queueLength, bool hasFreeUploadSlot)
        {
            Description = description;
            HasPicture = hasPicture;
            Picture = picture;
            UploadSlots = uploadSlots;
            QueueLength = queueLength;
            HasFreeUploadSlot = hasFreeUploadSlot;
        }

        /// <summary>
        ///     Gets the user's description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     Gets a value indicating whether a picture has been configured.
        /// </summary>
        public bool HasPicture { get; }

        /// <summary>
        ///     Gets the picture data, if configured.
        /// </summary>
        public byte[] Picture { get; }

        /// <summary>
        ///     Gets the number of configured upload slots.
        /// </summary>
        public int UploadSlots { get; }

        /// <summary>
        ///     Gets the current queue length.
        /// </summary>
        public int QueueLength { get; }

        /// <summary>
        ///     Gets a value indicating whether an upload slot is free.
        /// </summary>
        public bool HasFreeUploadSlot { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="UserInfoResponse"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static UserInfoResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerInfoResponse)
            {
                throw new MessageException($"Message Code mismatch creating Peer Info Response (expected: {(int)MessageCode.PeerInfoResponse}, received: {(int)reader.Code}.");
            }

            var description = reader.ReadString();
            var hasPicture = reader.ReadByte() > 0;
            byte[] picture = null;

            if (hasPicture)
            {
                var pictureLen = reader.ReadInteger();
                picture = reader.ReadBytes(pictureLen);
            }

            var uploadSlots = reader.ReadInteger();
            var queueLength = reader.ReadInteger();
            var hasFreeUploadSlot = reader.ReadByte() > 0;

            return new UserInfoResponse(description, hasPicture, picture, uploadSlots, queueLength, hasFreeUploadSlot);
        }
    }
}