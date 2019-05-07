namespace Soulseek.Messaging.Messages
{
    using Soulseek.Exceptions;

    public sealed class PeerInfoResponse
    {
        internal PeerInfoResponse(string description, bool hasPicture, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            Description = description;
            HasPicture = hasPicture;
            Picture = picture;
            UploadSlots = uploadSlots;
            QueueLength = queueLength;
            HasFreeSlot = hasFreeSlot;
        }

        public string Description { get; }
        public bool HasPicture { get; }
        public byte[] Picture { get; }
        public int UploadSlots { get; }
        public int QueueLength { get; }
        public bool HasFreeSlot { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="PeerInfoResponse"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PeerInfoResponse Parse(Message message)
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
            var hasFreeSlot = reader.ReadByte() > 0;

            return new PeerInfoResponse(description, hasPicture, picture, uploadSlots, queueLength, hasFreeSlot);
        }
    }
}