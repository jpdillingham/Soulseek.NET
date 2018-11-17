// <copyright file="IConnection.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading.Tasks;

    internal interface IConnection : IDisposable
    {
        #region Public Properties

        string Address { get; }
        Action<IConnection> ConnectHandler { get; set; }
        object Context { get; }
        Action<IConnection, byte[]> DataReceivedHandler { get; set; }
        Action<IConnection, byte[]> DataSentHandler { get; set; }
        Action<IConnection, string> DisconnectHandler { get; set; }
        IPAddress IPAddress { get; }
        ConnectionKey Key { get; }
        ConnectionOptions Options { get; }
        int Port { get; }
        ConnectionState State { get; }

        #endregion Public Properties

        #region Public Methods

        Task ConnectAsync();

        void Disconnect(string message = null);

        Task<byte[]> ReadAsync(int count);

        Task<byte[]> ReadAsync(long count);

        Task SendAsync(byte[] bytes);

        #endregion Public Methods
    }
}