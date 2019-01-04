// <copyright file="MessageBuilder.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Zlib;

    /// <summary>
    ///     Builds a message.
    /// </summary>
    public class MessageBuilder
    {
        private List<byte> CodeBytes { get; set; } = new List<byte>();
        private bool Compressed { get; set; } = false;
        private List<byte> PayloadBytes { get; set; } = new List<byte>();

        /// <summary>
        ///     Builds the message.
        /// </summary>
        /// <returns>The built message.</returns>
        public Message Build()
        {
            if (CodeBytes.Count == 0)
            {
                throw new InvalidOperationException($"Unable to build the message without having set the message Code.");
            }

            var withLength = new List<byte>(BitConverter.GetBytes(CodeBytes.Count + PayloadBytes.Count));
            withLength.AddRange(CodeBytes);
            withLength.AddRange(PayloadBytes);
            return new Message(withLength.ToArray());
        }

        /// <summary>
        ///     Sets the message code.
        /// </summary>
        /// <param name="code">The desired message code.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder Code(MessageCode code)
        {
            CodeBytes = BitConverter.GetBytes((int)code).ToList();
            return this;
        }

        /// <summary>
        ///     Compresses the message payload.
        /// </summary>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to compress an empty message.</exception>
        /// <exception cref="MessageCompressionException">Thrown when an error is encountered while compressing the message payload.</exception>
        public MessageBuilder Compress()
        {
            if (PayloadBytes.Count == 0)
            {
                throw new InvalidOperationException($"Unable to compress an empty message.");
            }

            if (Compressed)
            {
                throw new InvalidOperationException("The message has already been compressed.");
            }

            byte[] compressedBytes;

            Compress(PayloadBytes.ToArray(), out compressedBytes);

            PayloadBytes = compressedBytes.ToList();
            Compressed = true;

            return this;
        }

        /// <summary>
        ///     Writes the specified byte <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to write additional data to a message that has been compressed.</exception>
        public MessageBuilder WriteByte(byte value)
        {
            return WriteBytes(new[] { value });
        }

        /// <summary>
        ///     Writes the specified <paramref name="bytes"/> to the message.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to write additional data to a message that has been compressed.</exception>
        public MessageBuilder WriteBytes(byte[] bytes)
        {
            if (Compressed)
            {
                throw new InvalidOperationException("Unable to write additional data after message has been compressed.");
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), "The specified byte array is null.");
            }

            PayloadBytes.AddRange(bytes);
            return this;
        }

        /// <summary>
        ///     Writes the specified integer <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to write additional data to a message that has been compressed.</exception>
        public MessageBuilder WriteInteger(int value)
        {
            return WriteBytes(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///     Writes the specified long <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to write additional data to a message that has been compressed.</exception>
        public MessageBuilder WriteLong(long value)
        {
            return WriteBytes(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///     Writes the specified string <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to write additional data to a message that has been compressed.</exception>
        public MessageBuilder WriteString(string value)
        {
            return WriteBytes(BitConverter.GetBytes(value.Length))
                .WriteBytes(Encoding.ASCII.GetBytes(value));
        }

        private void Compress(byte[] inData, out byte[] outData)
        {
            void copyStream(Stream input, Stream output)
            {
                byte[] buffer = new byte[2000];
                int len;

                while ((len = input.Read(buffer, 0, 2000)) > 0)
                {
                    output.Write(buffer, 0, len);
                }

                output.Flush();
            }

            try
            {
                using (MemoryStream outMemoryStream = new MemoryStream())
                using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
                using (Stream inMemoryStream = new MemoryStream(inData))
                {
                    copyStream(inMemoryStream, outZStream);
                    outZStream.finish();
                    outData = outMemoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new MessageCompressionException($"Failed to compress the message payload.", ex);
            }
        }
    }
}