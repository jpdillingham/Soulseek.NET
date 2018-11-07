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

    public class Message
    {
        public Message(byte[] bytes)
        {
            Bytes = bytes;
        }

        public MessageCode Code => GetCode();
        public int Length => GetLength();
        public byte[] Payload => GetPayload();
        private byte[] Bytes { get; set; }

        public byte[] ToByteArray()
        {
            return Bytes;
        }

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