// <copyright file="ConnectionKey.cs" company="JP Dillingham">
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

    internal class ConnectionKey : IEquatable<ConnectionKey>
    {
        public string Username { get; set; }
        public IPAddress IPAddress { get; set; }
        public int Port { get; set; }
        public MessageConnectionType Type { get; set; }

        public bool Equals(ConnectionKey other)
        {
            return Username == other.Username && IPAddress.ToString() == other.IPAddress.ToString() && Port == other.Port && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            try
            {
                return Equals((ConnectionKey)obj);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var u = Username?.GetHashCode() ?? 0;
            var i = IPAddress?.ToString().GetHashCode() ?? 0;
            return u ^ i ^ Port.GetHashCode() ^ Type.GetHashCode();
        }

        public override string ToString()
        {
            return $"Username: {Username}, IPAddress: {IPAddress}, Port: {Port}, Type: {Type}, HashCode: {GetHashCode()}";
        }
    }
}
