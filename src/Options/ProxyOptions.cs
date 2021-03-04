// <copyright file="ProxyOptions.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Net;

    public class ProxyOptions
    {
        public ProxyOptions(string address, int port, string username = null, string password = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Address must not be a null or empty string, or one consisting only of whitespace", nameof(address));
            }

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"The port must be within the range {IPEndPoint.MinPort}-{IPEndPoint.MaxPort} (specified: {port})");
            }

            if (username == default != (password == default))
            {
                throw new ArgumentException("Username and password must both be specified");
            }

            if (username != default && username.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(username), "The username must be less than or equal to 255 characters");
            }

            if (password != default && password.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(password), "The password must be less than or equal to 255 characters");
            }

            Address = address;
            Port = port;
            Username = username;
            Password = password;
        }

        public string Address { get; }
        public int Port { get; }
        public bool HasCredentuals => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        public string Username { get; }
        public string Password { get; }
    }
}
