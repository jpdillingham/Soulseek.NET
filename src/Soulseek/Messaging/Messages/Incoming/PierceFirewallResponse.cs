// <copyright file="PierceFirewallResponse.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    /// <summary>
    ///     Pierces the local firewall to initiate a connection.
    /// </summary>
    internal sealed class PierceFirewallResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PierceFirewallResponse"/> class.
        /// </summary>
        /// <param name="token">The unique token for the connection.</param>
        public PierceFirewallResponse(int token)
        {
            Token = token;
        }

        /// <summary>
        ///     Gets the unique token for the connection.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PierceFirewallResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <param name="response">The parsed instance.</param>
        /// <returns>A value indicating whether the message was successfully parsed.</returns>
        public static bool TryFromByteArray(byte[] bytes, out PierceFirewallResponse response)
        {
            response = null;

            try
            {
                var reader = new MessageReader<MessageCode.Initialization>(bytes);

                if (reader.ReadCode() != MessageCode.Initialization.PierceFirewall)
                {
                    return false;
                }

                var token = reader.ReadInteger();

                response = new PierceFirewallResponse(token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
