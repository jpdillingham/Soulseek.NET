// <copyright file="DistributedServerSearchRequest.cs" company="JP Dillingham">
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
    /// <summary>
    ///     A distributed file search request.
    /// </summary>
    /// <remarks>
    ///     This is an odd message; according to the documentation and the code, it is a server message, however i've only seen it
    ///     come from a distributed parent. It isn't clear whether this can ever come from the server; perhaps if we are connected
    ///     directly, but no idea how to do that or why we'd want to.
    /// </remarks>
    internal sealed class DistributedServerSearchRequest : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedServerSearchRequest"/> class.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The unique token for the request.</param>
        /// <param name="query">The search query.</param>
        public DistributedServerSearchRequest(string username, int token, string query)
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
        ///     Creates a new instance of <see cref="DistributedServerSearchRequest"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static DistributedServerSearchRequest FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Distributed>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Distributed.ServerSearchRequest)
            {
                throw new MessageException($"Message Code mismatch creating Distributed Search Request (expected: {(int)MessageCode.Distributed.ServerSearchRequest}, received: {(int)code})");
            }

            // nobody knows what this is.  always 0000000331000000 in hex.
            reader.ReadBytes(8);

            var username = reader.ReadString();
            var token = reader.ReadInteger();
            var query = reader.ReadString();

            return new DistributedServerSearchRequest(username, token, query);
        }
    }
}