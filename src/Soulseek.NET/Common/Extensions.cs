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
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    ///     Extension methods.
    /// </summary>
    public static class Extensions
    {
        public static void DequeueAndDisposeAll<T>(this ConcurrentQueue<T> concurrentQueue)
                    where T : IDisposable
        {
            while (!concurrentQueue.IsEmpty)
            {
                if (concurrentQueue.TryDequeue(out var value))
                {
                    value.Dispose();
                }
            }
        }

        /// <summary>
        ///     Continue a task and report an Exception if one is raised.
        /// </summary>
        /// <param name="task">The task to continue.</param>
        public static void Forget(this Task task)
        {
            task.ContinueWith(t => { Debug.WriteLine($"Forgotten Thread Error: {t.Exception.Message}", t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void RemoveAll<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> concurrentDictionary)
        {
            while (!concurrentDictionary.IsEmpty)
            {
                concurrentDictionary.TryRemove(concurrentDictionary.Keys.First(), out var _);
            }
        }

        public static void RemoveAndDisposeAll<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> concurrentDictionary)
                    where TValue : IDisposable
        {
            while (!concurrentDictionary.IsEmpty)
            {
                if (concurrentDictionary.TryRemove(concurrentDictionary.Keys.First(), out var value))
                {
                    value.Dispose();
                }
            }
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

        public static IPAddress ResolveIPAddress(this string address)
        {
            if (IPAddress.TryParse(address, out IPAddress ip))
            {
                return ip;
            }
            else
            {
                return Dns.GetHostEntry(address).AddressList[0];
            }
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