// <copyright file="IMessageConnection.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging.Tcp
{
    using System;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Tcp;

    internal interface IMessageConnection : IConnection, IDisposable
    {
        event EventHandler<Message> MessageRead;

        string Username { get; }
        MessageConnectionType Type { get; }

        /// <summary>
        ///     Asynchronously sends the specified <paramref name="message"/> and optionally suppresses code normalization if <paramref name="suppressCodeNormalization"/> is specified.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="suppressCodeNormalization">A value indicating whether normalization of the message code should be suppressed.</param>
        /// <returns>A value indicating whether the write was deferred until the connection is established instead of being sent immediately.</returns>
        Task<bool> SendMessageAsync(Message message, bool suppressCodeNormalization = false);
    }
}