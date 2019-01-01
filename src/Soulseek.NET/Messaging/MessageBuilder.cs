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
        private List<byte> PayloadBytes { get; set; } = new List<byte>();
        private bool Initialized { get; set; } = false;

        /// <summary>
        ///     Builds the message.
        /// </summary>
        /// <returns>The built message.</returns>
        public Message Build()
        {
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
            if (Initialized)
            {
                throw new MessageBuildException($"The Message Code may only be set once.");
            }

            Initialized = true;

            CodeBytes = BitConverter.GetBytes((int)code).ToList();
            return this;
        }

        /// <summary>
        ///     Sets the single-byte message code.
        /// </summary>
        /// <param name="code">The desired single-byte message code.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder Code(byte code)
        {
            if (Initialized)
            {
                throw new MessageBuildException($"The Message Code may only be set once.");
            }

            Initialized = true;

            CodeBytes = new[] { code }.ToList();
            return this;
        }

        /// <summary>
        ///     Writes the specified byte <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteByte(byte value)
        {
            EnsureInitialized();

            PayloadBytes.Add(value);
            return this;
        }

        /// <summary>
        ///     Writes the specified <paramref name="bytes"/> to the message.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteBytes(byte[] bytes)
        {
            EnsureInitialized();

            PayloadBytes.AddRange(bytes);
            return this;
        }

        /// <summary>
        ///     Writes the specified integer <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteInteger(int value)
        {
            return WriteBytes(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///     Writes the specified long <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteLong(long value)
        {
            return WriteBytes(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///     Writes the specified string <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteString(string value)
        {
            EnsureInitialized();

            PayloadBytes.AddRange(BitConverter.GetBytes(value.Length));
            PayloadBytes.AddRange(Encoding.ASCII.GetBytes(value));
            return this;
        }

        public MessageBuilder Compress()
        {
            byte[] compressedBytes;

            try
            {
                Compress(PayloadBytes.ToArray(), out compressedBytes);
            }
            catch (Exception ex)
            {
                throw new MessageBuildException($"Failed to compress message payload.", ex);
            }

            PayloadBytes = compressedBytes.ToList();

            return this;
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

            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                copyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }

        private void EnsureInitialized()
        {
            if (!Initialized)
            {
                throw new MessageBuildException($"The Message must be initialized with a Code prior to writing data.");
            }
        }
    }
}