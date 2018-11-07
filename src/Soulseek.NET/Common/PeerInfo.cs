// <copyright file="PeerInfo.cs" company="JP Dillingham">
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

namespace Soulseek.NET
{
    public class PeerInfo
    {
        internal PeerInfo()
        {
        }

        public int Active { get; internal set; }
        public int Connected { get; internal set; }
        public int Connecting { get; internal set; }
        public int Disconnected { get; internal set; }
        public int Disconnecting { get; internal set; }
        public int Queued { get; internal set; }
    }
}