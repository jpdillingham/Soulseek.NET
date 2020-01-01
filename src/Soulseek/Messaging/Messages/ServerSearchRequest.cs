// <copyright file="ServerSearchRequest.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using Soulseek.Exceptions;

    /// <summary>
    ///     A server file search request.
    /// </summary>
    internal sealed class ServerSearchRequest
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerSearchRequest"/> class.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The unique token for the request.</param>
        /// <param name="query">The search query.</param>
        public ServerSearchRequest(string username, int token, string query)
        {
            Username = username;
            Token = token;
            Query = query;
        }

        /// <summary>
        ///     Gets the search query.
        /// </summary>
        public string Query { get; }

        /// <summary>
        ///     Gets the unique token for the request.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the requesting user.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="ServerSearchRequest"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static ServerSearchRequest FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.FileSearch)
            {
                throw new MessageException($"Message Code mismatch creating Server Search Request (expected: {(int)MessageCode.Server.FileSearch}, received: {(int)code}.");
            }

            var username = reader.ReadString();
            var token = reader.ReadInteger();
            var query = reader.ReadString();

            return new ServerSearchRequest(username, token, query);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.FileSearch)
                .WriteInteger(Token)
                .WriteString(Query)
                .Build();
        }
    }
}