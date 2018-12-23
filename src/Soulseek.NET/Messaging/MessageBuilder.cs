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
    using Soulseek.NET.Exceptions;

    /// <summary>
    ///     Builds a message.
    /// </summary>
    public class MessageBuilder
    {
        private List<byte> Bytes { get; set; } = new List<byte>();
        private bool Initialized { get; set; } = false;

        /// <summary>
        ///     Builds the message.
        /// </summary>
        /// <returns>The built message.</returns>
        public Message Build()
        {
            var withLength = new List<byte>(BitConverter.GetBytes(Bytes.Count));
            withLength.AddRange(Bytes);
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

            Bytes.AddRange(BitConverter.GetBytes((int)code));
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

            Bytes.Add(code);
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

            Bytes.Add(value);
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

            Bytes.AddRange(bytes);
            return this;
        }

        /// <summary>
        ///     Writes the specified integer <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteInteger(int value)
        {
            return WriteLong(value);
        }

        /// <summary>
        ///     Writes the specified long <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteLong(long value)
        {
            EnsureInitialized();

            Bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>
        ///     Writes the specified string <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
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