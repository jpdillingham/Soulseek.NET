namespace Soulseek.NET.Tests.Unit
{
    using Soulseek.NET.Messaging;
    using System.Linq;

    public static class Extensions
    {
        /// <summary>
        ///     Pads the code of the given message with 3 bytes to make it compatible with <see cref="MessageReader"/>, and returns
        ///     a MessageReader constructed with the resulting message.
        /// </summary>
        /// <remarks>
        ///     This is an edge case seen only in testing; we never need to read outgoing messages. Outgoing peer messages use a
        ///     single byte for the code, whereas server messages use 4 bytes, so we need this method to enable MessageReader to
        ///     work with single-byte codes.
        /// </remarks>
        /// <param name="message">The Message to modify.</param>
        /// <returns>The MessageReader for the given Message.</returns>
        public static MessageReader ToPeerMessageReader(this Message message)
        {
            var bytes = message.ToByteArray().ToList();
            bytes.InsertRange(0, new byte[] { 0x0, 0x0, 0x0 });

            return new MessageReader(bytes.ToArray());
        }
    }
}