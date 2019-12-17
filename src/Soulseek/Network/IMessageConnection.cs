// <copyright file="IMessageConnection.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Network
{
    using System;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Provides client connections to the Soulseek network.
    /// </summary>
    internal interface IMessageConnection : IConnection
    {
        /// <summary>
        ///     Occurs when a new message is read in its entirety.
        /// </summary>
        event EventHandler<MessageReadEventArgs> MessageRead;

        /// <summary>
        ///     Occurs when a new message is received, but before it is read.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Gets a value indicating whether the internal continuous read loop is running.
        /// </summary>
        bool ReadingContinuously { get; }

        /// <summary>
        ///     Gets the username of the peer associated with the connection, if applicable.
        /// </summary>
        string Username { get; }

        /// <summary>
        ///     Begins the internal continuous read loop, if it has not yet started.
        /// </summary>
        /// <remarks>
        ///     This functionality should be used only when an incoming connection has already been established in an IConnection
        ///     instance and with a Connected ITcpClient, and when that IConnection is upgraded to an IMessageConnection, handing
        ///     off the ITcpClient instance without disconnecting it. Normally reading begins when the Connected event is fired,
        ///     but since the connection is already Connected the event will not be fired again. It is important to delay the
        ///     start of the read loop until after the calling code has had the chance to connect an event handler to the
        ///     MessageRead event, which is impossible if we simply start the loop immediately upon instantiation.
        /// </remarks>
        void StartReadingContinuously();
    }
}