// <copyright file="MessageReader.cs" company="JP Dillingham">
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
    using System.IO;
    using System.Linq;
    using System.Text;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Zlib;

    /// <summary>
    ///     Reads data from a Message payload.
    /// </summary>
    public class MessageReader
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageReader"/> class from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message with which to initialize the reader.</param>
        public MessageReader(Message message)
        {
            Message = message;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageReader"/> class from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array with which to initialize the reader.</param>
        public MessageReader(byte[] bytes)
            : this(new Message(bytes))
        {
        }

        /// <summary>
        ///     Gets the Message Code.
        /// </summary>
        public MessageCode Code => Message.Code;

        /// <summary>
        ///     Gets the Message payload.
        /// </summary>
        public byte[] Payload => Message.Payload;

        /// <summary>
        ///     Gets the current position of the head of the reader.
        /// </summary>
        public int Position { get; private set; } = 0;

        private Message Message { get; set; }

        /// <summary>
        ///     Moves the head of the reader to the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The desired position.</param>
        public void Seek(int position)
        {
            if (position < 0)
            {
                throw new MessageReadException($"Attempt to seek to a negative position.");
            }

            if (position > Payload.Length)
            {
                throw new MessageReadException($"Seek to position {position} would extend beyond the length of the message.");
            }

            Position = position;
        }

        /// <summary>
        ///     Decompresses the message payload.
        /// </summary>
        /// <returns>This MessageReader.</returns>
        public MessageReader Decompress()
        {
            byte[] decompressedPayload;

            try
            {
                Decompress(Payload, out decompressedPayload);
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to decompress message payload.", ex);
            }

            Message = new MessageBuilder()
                .Code(Code)
                .WriteBytes(decompressedPayload)
                .Build();

            return this;
        }

        /// <summary>
        ///     Reads a single byte at the head of the reader.
        /// </summary>
        /// <returns>The read byte.</returns>
        public int ReadByte()
        {
            try
            {
                var retVal = Payload[Position];
                Position += 1;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a byte from position {Position} of the message.", ex);
            }
        }

        /// <summary>
        ///     Reads a byte array of length <paramref name="count"/> at the head of the reader.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The read bytes.</returns>
        public byte[] ReadBytes(int count)
        {
            try
            {
                var retVal = Payload.Skip(Position).Take(count).ToArray();
                Position += count;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a byte from position {Position} of the message.", ex);
            }
        }

        /// <summary>
        ///     Reads an integer at the head of the reader.
        /// </summary>
        /// <returns>The read integer.</returns>
        public int ReadInteger()
        {
            try
            {
                var retVal = BitConverter.ToInt32(Payload, Position);
                Position += 4;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read an integer (4 bytes) from position {Position} of the message.", ex);
            }
        }

        /// <summary>
        ///     Reads a long at the head of the reader.
        /// </summary>
        /// <returns>The read long.</returns>
        public long ReadLong()
        {
            try
            {
                var retVal = BitConverter.ToInt64(Payload, Position);
                Position += 8;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a long integer (8 bytes) from position {Position} of the message.", ex);
            }
        }

        /// <summary>
        ///     Reads a string at the head of the reader.
        /// </summary>
        /// <returns>The read string.</returns>
        public string ReadString()
        {
            var length = 0;

            try
            {
                length = ReadInteger();
                var bytes = Payload.Skip(Position).Take(length).ToArray();
                var retVal = Encoding.ASCII.GetString(bytes);
                Position += length;
                return retVal;
            }
            catch (MessageReadException ex)
            {
                throw new MessageReadException($"Failed to read the length of the requested string from position {Position} of the message.", ex);
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a string of length {length} from position {Position} of the message.", ex);
            }
        }

        /// <summary>
        ///     Returns the head of the reader to the beginning of the message payload.
        /// </summary>
        /// <returns>This MessageReader.</returns>
        public MessageReader Reset()
        {
            Position = 0;
            return this;
        }

        private void Decompress(byte[] inData, out byte[] outData)
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

            using (var outMemoryStream = new MemoryStream())
            using (var outZStream = new ZOutputStream(outMemoryStream))
            using (var inMemoryStream = new MemoryStream(inData))
            {
                copyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }
    }
}