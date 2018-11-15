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

namespace Soulseek.NET.Tcp
{
    using System;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;

    internal interface IMessageConnection : IConnection, IDisposable
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        string Username { get; }
        ConnectionType Type { get; }
        Action<IMessageConnection> ConnectHandler { get; set; }
        Action<IMessageConnection> DisconnectHandler { get; set; }
        Action<IMessageConnection, Message> MessageHandler { get; set; }

        Task SendAsync(Message message, bool suppressCodeNormalization = false);
    }
}