// <copyright file="PrivilegedUserListReceivedEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System.Collections.Generic;

    /// <summary>
    ///     Event arguments for events raised upon receipt of the list of privileged users.
    /// </summary>
    public class PrivilegedUserListReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivilegedUserListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="usernames">The list usernames of privilegd users.</param>
        public PrivilegedUserListReceivedEventArgs(IReadOnlyCollection<string> usernames)
        {
            Usernames = usernames;
        }

        /// <summary>
        ///     Gets the list of usernames of privileged users.
        /// </summary>
        public IReadOnlyCollection<string> Usernames { get; }
    }
}