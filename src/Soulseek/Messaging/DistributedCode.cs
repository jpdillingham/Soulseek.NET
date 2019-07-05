// <copyright file="DistributedCode.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging
{
    /// <summary>
    ///     Distributed message codes.
    /// </summary>
    public enum DistributedCode : byte
    {
        /// <summary>
        ///     0
        /// </summary>
        DistributedPing = 0,

        /// <summary>
        ///     3
        /// </summary>
        DistributedSearchRequest = 3,

        /// <summary>
        ///     4
        /// </summary>
        DistributedBanchLevel = 4,

        /// <summary>
        ///     5
        /// </summary>
        DistributedBranchRoot = 5,

        /// <summary>
        ///     7
        /// </summary>
        DistributedChildDepth = 7,
    }
}
