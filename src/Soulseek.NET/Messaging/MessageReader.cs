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
    using Soulseek.NET.Zlib;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class MessageReader
    {
        public MessageReader(Message message)
        {
            Message = message;
        }

        public MessageReader(byte[] bytes)
            : this(new Message(bytes))
        {
        }

        public MessageCode Code => Message.Code;
        public byte[] Payload => Message.Payload;
        public int Position { get; private set; } = 0;

        private Message Message { get; set; }

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

        public MessageReader Decompress()
        {
            byte[] decompressedPayload;

            Decompress(Payload, out decompressedPayload);

            Message = new MessageBuilder()
                .Code(Code)
                .WriteBytes(decompressedPayload)
                .Build();

            return this;
        }

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