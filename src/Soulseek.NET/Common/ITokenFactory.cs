// <copyright file="ITokenFactory.cs" company="JP Dillingham">
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
    using System;

    /// <summary>
    ///     Generates unique tokens for network operations.
    /// </summary>
    public interface ITokenFactory
    {
        /// <summary>
        ///     Gets a new unique token.
        /// </summary>
        /// <returns>The new unique token.</returns>
        int GetToken();

        /// <summary>
        ///     Gets a new unique token after checking for collisions using the specified <paramref name="collisionCheck"/>.
        /// </summary>
        /// <param name="collisionCheck">The function used to check for token collisions.</param>
        /// <returns>The new unique token.</returns>
        int GetToken(Func<int, bool> collisionCheck);

        /// <summary>
        ///     Gets a new unique token after checking for collisions using the specified <paramref name="collisionCheck"/>.
        /// </summary>
        /// <param name="collisionCheck">The function used to check for token collisions.</param>
        /// <param name="token">The new unique token.</param>
        /// <returns>A value indicating whether the creation was successful.</returns>
        bool TryGetToken(Func<int, bool> collisionCheck, out int? token);
    }
}
