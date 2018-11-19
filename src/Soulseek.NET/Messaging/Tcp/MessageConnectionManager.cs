// <copyright file="MessageConnectionManager.cs" company="JP Dillingham">
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
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;

    internal class MessageConnectionManager : ConnectionManager<IMessageConnection>, IDisposable
    {
        internal MessageConnectionManager(int concurrentConnections)
            : base(concurrentConnections)
        {
        }

        internal IMessageConnection GetServerConnectionAsync(string address, int port, ConnectionOptions options = null)
        {
            options = options ?? new ConnectionOptions();
            return new MessageConnection(ConnectionType.Server, address, port, options);
        }

        internal async Task<IMessageConnection> GetUnsolicitedPeerConnectionAsync(string localUsername, ConnectionKey key, ConnectionOptions options = null)
        {
            options = options ?? new ConnectionOptions();

            var connection = new MessageConnection(ConnectionType.Peer, key.Username, key.IPAddress.ToString(), key.Port, options);
            connection.ConnectHandler = async (conn) =>
            {
                var token = new Random().Next(1, 2147483647);
                await connection.SendMessageAsync(new PeerInitRequest(localUsername, "P", token).ToMessage(), suppressCodeNormalization: true);
            };
            connection.DisconnectHandler = async (conn, message) => await Remove(connection);

            await Add(connection);
            return connection;
        }

        internal async Task<IMessageConnection> GetSolicitedPeerConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options = null)
        {
            options = options ?? new ConnectionOptions();

            var connection = new MessageConnection(ConnectionType.Peer, connectToPeerResponse.Username, connectToPeerResponse.IPAddress.ToString(), connectToPeerResponse.Port, options)
            {
                Context = connectToPeerResponse,
            };
            connection.ConnectHandler = async (conn) =>
            {
                var context = (ConnectToPeerResponse)conn.Context;
                var request = new PierceFirewallRequest(context.Token).ToMessage();
                await connection.SendMessageAsync(request, suppressCodeNormalization: true);
            };
            connection.DisconnectHandler = async (conn, message) => await Remove(connection);

            await Add(connection);
            return connection;
        }
    }
}