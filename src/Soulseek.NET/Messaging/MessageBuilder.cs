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
    using System.Linq;
    using System.Text;

    public class MessageBuilder
    {
        public MessageBuilder()
        {
        }

        private List<byte> Bytes { get; set; } = new List<byte>();
        private bool Initialized { get; set; } = false;

        public Message Build()
        {
            var withLength = new List<byte>(BitConverter.GetBytes(Bytes.Count()));
            withLength.AddRange(Bytes);
            return new Message(withLength.ToArray());
        }

        public MessageBuilder Code(MessageCode code)
        {
            if (Initialized)
            {
                throw new MessageBuildException($"The Message Code may only be set once.");
            }

            Initialized = true;

            Bytes.AddRange(BitConverter.GetBytes((int)code));
            return this;
        }

        public MessageBuilder Code(byte code)
        {
            if (Initialized)
            {
                throw new MessageBuildException($"The Message Code may only be set once.");
            }

            Initialized = true;

            Bytes.Add(code);
            return this;
        }

        public MessageBuilder WriteByte(byte value)
        {
            EnsureInitialized();

            Bytes.Add(value);
            return this;
        }

        public MessageBuilder WriteBytes(byte[] values)
        {
            EnsureInitialized();

            Bytes.AddRange(values);
            return this;
        }

        public MessageBuilder WriteInteger(int value)
        {
            EnsureInitialized();

            Bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public MessageBuilder WriteLong(long value)
        {
            EnsureInitialized();

            Bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public MessageBuilder WriteString(string value)
        {
            EnsureInitialized();

            Bytes.AddRange(BitConverter.GetBytes(value.Length));
            Bytes.AddRange(Encoding.ASCII.GetBytes(value));
            return this;
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