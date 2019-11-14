// <copyright file="UserInfoResponse.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
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
        /// <param name="uploadSlots">The number of configured upload slots.</param>
        /// <param name="queueLength">The current queue length.</param>
        /// <param name="hasFreeUploadSlot">A value indicating whether an upload slot is free.</param>
        public UserInfoResponse(string description, int uploadSlots, int queueLength, bool hasFreeUploadSlot)
            : this(description, false, null, uploadSlots, queueLength, hasFreeUploadSlot)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfoResponse"/> class.
        /// </summary>
        /// <param name="description">The peer's description.</param>
        /// <param name="picture">If configured, the picture data.</param>
        /// <param name="uploadSlots">The number of configured upload slots.</param>
        /// <param name="queueLength">The current queue length.</param>
        /// <param name="hasFreeUploadSlot">A value indicating whether an upload slot is free.</param>
        public UserInfoResponse(string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeUploadSlot)
            : this(description, picture != null, picture, uploadSlots, queueLength, hasFreeUploadSlot)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfoResponse"/> class.
        /// </summary>
        /// <param name="description">The peer's description.</param>
        /// <param name="hasPicture">A value indicating whether a picture has been configured.</param>
        /// <param name="picture">If configured, the picture data.</param>
        /// <param name="uploadSlots">The number of configured upload slots.</param>
        /// <param name="queueLength">The current queue length.</param>
        /// <param name="hasFreeUploadSlot">A value indicating whether an upload slot is free.</param>
        private UserInfoResponse(string description, bool hasPicture, byte[] picture, int uploadSlots, int queueLength, bool hasFreeUploadSlot)
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
        ///     Gets a value indicating whether an upload slot is free.
        /// </summary>
        public bool HasFreeUploadSlot { get; }

        /// <summary>
        ///     Gets a value indicating whether a picture has been configured.
        /// </summary>
        public bool HasPicture { get; }

        /// <summary>
        ///     Gets the picture data, if configured.
        /// </summary>
        public byte[] Picture { get; }

        /// <summary>
        ///     Gets the current queue length.
        /// </summary>
        public int QueueLength { get; }

        /// <summary>
        ///     Gets the number of configured upload slots.
        /// </summary>
        public int UploadSlots { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserInfoResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        internal static UserInfoResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.InfoResponse)
            {
                throw new MessageException($"Message Code mismatch creating Peer Info Response (expected: {(int)MessageCode.Peer.InfoResponse}, received: {(int)code}.");
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

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        internal byte[] ToByteArray()
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(Description)
                .WriteByte((byte)(HasPicture ? 1 : 0));

            if (HasPicture)
            {
                builder
                    .WriteInteger(Picture.Length)
                    .WriteBytes(Picture);
            }

            builder
                .WriteInteger(UploadSlots)
                .WriteInteger(QueueLength)
                .WriteByte((byte)(HasFreeUploadSlot ? 1 : 0));

            return builder.Build();
        }
    }
}