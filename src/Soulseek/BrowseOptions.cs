// <copyright file="BrowseOptions.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Options for the browse operation.
    /// </summary>
    public class BrowseOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseOptions"/> class.
        /// </summary>
        /// <param name="responseTimeout">The response timeout value, in seconds, for the start of the response data transfer.</param>
        public BrowseOptions(int responseTimeout = 60)
        {
            ResponseTimeout = responseTimeout;
        }

        /// <summary>
        ///     Gets the response timeout value, in seconds, for the start of the response data transfer.
        /// </summary>
        public int ResponseTimeout { get; }
    }
}