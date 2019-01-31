// <copyright file="Extensions.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit
{
    using System.Linq;
    using Soulseek.NET.Messaging;

    public static class TestExtensions
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