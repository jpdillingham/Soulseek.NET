// <copyright file="TcpListenerAdapter.cs" company="JP Dillingham">
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

namespace Soulseek.Tcp
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides a listener for TCP network services.
    /// </summary>
    /// <remarks>
    ///     This is a pass-through implementation of <see cref="ITcpListener"/> over <see cref="TcpListener"/> intended to enable
    ///     dependency injection.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal sealed class TcpListenerAdapter : ITcpListener
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpListenerAdapter"/> class with an optional <paramref name="tcpListener"/>.
        /// </summary>
        /// <param name="tcpListener">The optional TcpListener to wrap.</param>
        internal TcpListenerAdapter(TcpListener tcpListener = null)
        {
            TcpListener = tcpListener ?? new TcpListener(IPAddress.Parse("0.0.0.0"), 1);
        }

        private TcpListener TcpListener { get; set; }

        public bool Pending()
        {
            return TcpListener.Pending();
        }

        public void Start()
        {
            TcpListener.Start();
        }

        public void Stop()
        {
            TcpListener.Stop();
        }

        public Task<TcpClient> AcceptTcpClientAsync()
        {
            return TcpListener.AcceptTcpClientAsync();
        }
    }
}