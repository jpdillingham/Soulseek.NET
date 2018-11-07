// <copyright file="Extensions.cs" company="JP Dillingham">
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
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    ///     Extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Continue a task and report an Exception if one is raised.
        /// </summary>
        /// <param name="task">The task to continue.</param>
        public static void Forget(this Task task)
        {
            task.ContinueWith(t => { throw new Exception($"Thread Error: {t.Exception.Message}", t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        ///     Reset a timer.
        /// </summary>
        /// <param name="timer">The timer to reset.</param>
        public static void Reset(this Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        /// <summary>
        ///     Returns the MD5 hash of a string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>The MD5 hash of the input string.</returns>
        public static string ToMD5Hash(this string str)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(str);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Encoding.ASCII.GetString(hashBytes);
            }
        }
    }
}