// <copyright file="Message.cs" company="JP Dillingham">
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
    using System.Linq;
    using Soulseek.NET.Exceptions;

    /// <summary>
    ///     A message.
    /// </summary>
    public class Message
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Message"/> class from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array with which to initialize the message.</param>
        public Message(byte[] bytes)
        {
            Bytes = bytes;
        }

        /// <summary>
        ///     Gets the message code.
        /// </summary>
        public MessageCode Code => GetCode();

        /// <summary>
        ///     Gets the message length.
        /// </summary>
        public int Length => GetLength();

        /// <summary>
        ///     Gets the message payload.
        /// </summary>
        public byte[] Payload => GetPayload();

        private byte[] Bytes { get; set; }

        /// <summary>
        ///     Returns the message as a byte array.
        /// </summary>
        /// <returns>The message as a byte array.</returns>
        public byte[] ToByteArray()
        {
            return Bytes;
        }

        /// <summary>
        ///     Returns a <see cref="MessageReader"/> for the message.
        /// </summary>
        /// <returns>The MessageReader for the message.</returns>
        public MessageReader ToReader()
        {
            return new MessageReader(this);
        }

        private MessageCode GetCode()
        {
            try
            {
                var retVal = BitConverter.ToInt32(Bytes, 4);
                return (MessageCode)retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read the message code of the message.", ex);
            }
        }

        private int GetLength()
        {
            try
            {
                var retVal = BitConverter.ToInt32(Bytes, 0);
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read the message length.", ex);
            }
        }

        private byte[] GetPayload()
        {
            return Bytes.Skip(8).ToArray();
        }
    }
}