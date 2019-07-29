// <copyright file="IDistributedConnectionManager.cs" company="JP Dillingham">
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

namespace Soulseek.Network
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    internal interface IDistributedConnectionManager : IDisposable, IDiagnosticGenerator
    {
        IReadOnlyDictionary<int, string> PendingSolicitations { get; }

        Task AddChildConnectionAsync(string username, ITcpClient tcpClient);

        Task AddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse);

        void AddOrUpdateBranchLevel(string username, int level);

        void AddOrUpdateBranchRoot(string username, string root);

        Task AddParentConnectionAsync(IEnumerable<(string Username, IPAddress IPAddress, int Port)> parentCandidates);

        Task BroadcastMessageAsync(byte[] bytes);

        /// <summary>
        ///     Removes and disposes the parent and all child connections.
        /// </summary>
        void RemoveAndDisposeAll();
    }
}