// <copyright file="DistributedConnectionManager.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Soulseek.Network;

    internal interface IDistributedConnectionManager : IDisposable, IDiagnosticGenerator
    {
        void UpdateParentPool(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents);
    }

    internal sealed class DistributedConnectionManager : IDistributedConnectionManager
    {
        public DistributedConnectionManager(ISoulseekClient soulseekClient)
        {
            SoulseekClient = (SoulseekClient)soulseekClient;
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        private SoulseekClient SoulseekClient { get; }
        private ConcurrentDictionary<string, IMessageConnection> ChildConnections { get; } = new ConcurrentDictionary<string, IMessageConnection>();
        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private IMessageConnection ParentConnection { get; set; }
        private ConcurrentBag<(string Username, IPAddress IPAddress, int Port)> ParentPool { get; set; } = new ConcurrentBag<(string Username, IPAddress IPAddress, int Port)>();

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IPeerConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Removes and disposes all active and queued connections.
        /// </summary>
        public void RemoveAndDisposeAll()
        {
            ParentConnection?.Dispose();

            while (!ChildConnections.IsEmpty)
            {
                if (ChildConnections.TryRemove(ChildConnections.Keys.First(), out var value))
                {
                    value?.Dispose();
                }
            }
        }

        public void UpdateParentPool(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            ParentPool = new ConcurrentBag<(string Username, IPAddress IPAddress, int Port)>(parents);

            // todo: check if parent is connected and connect if not
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    RemoveAndDisposeAll();
                }

                Disposed = true;
            }
        }
    }
}