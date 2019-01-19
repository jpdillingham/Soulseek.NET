// <copyright file="TokenFactory.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Common
{
    using System;

    /// <summary>
    ///     Generates unique tokens for network operations.
    /// </summary>
    public static class TokenFactory
    {
        private const int MaxValue = 2147483647;
        private const int MaxIterations = 1000;
        private static readonly Random Random = new Random();

        /// <summary>
        ///     Gets a new unique token.
        /// </summary>
        /// <returns>The new unique token.</returns>
        public static int GetToken()
        {
            return GetToken(s => false);
        }

        /// <summary>
        ///     Gets a new unique token after checking for collisions using the specified <paramref name="collisionCheck"/>.
        /// </summary>
        /// <param name="collisionCheck">The function used to check for token collisions.</param>
        /// <returns>The new unique token.</returns>
        public static int GetToken(Func<int, bool> collisionCheck)
        {
            var iterations = 0;
            int token;

            do
            {
                if (iterations >= MaxIterations)
                {
                    throw new TimeoutException($"Failed to find an unused token after {MaxIterations} attempts.");
                }

                token = Random.Next(1, MaxValue);
                iterations++;
            }
            while (collisionCheck(token));

            return token;
        }
    }
}
