// <copyright file="CharacterEncoding.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Character encodings.
    /// </summary>
    public class CharacterEncoding
    {
        private CharacterEncoding(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Gets "ISO-8859-1" encoding.
        /// </summary>
        public static CharacterEncoding ISO88591 => new CharacterEncoding("ISO-8859-1");

        /// <summary>
        ///     Gets "UTF-8" encoding.
        /// </summary>
        public static CharacterEncoding UTF8 => new CharacterEncoding("UTF-8");

        private string Name { get; }

        public static implicit operator string(CharacterEncoding encoding) => encoding.Name;

        /// <summary>
        ///     Gets the string representation of this encoding.
        /// </summary>
        /// <returns>The string representation of this encoding.</returns>
        public override string ToString() => Name;
    }
}