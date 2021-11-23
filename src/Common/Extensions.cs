// <copyright file="Extensions.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    ///     Extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        ///     Dequeues and disposes of all instances within the specified <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The contained type of the queue.</typeparam>
        /// <param name="concurrentQueue">The queue from which to dequeue and dispose.</param>
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
        ///     Continue a task and swallow any Exceptions.
        /// </summary>
        /// <param name="task">The task to continue.</param>
        public static void Forget(this Task task)
        {
            task.ContinueWith(t => { }, TaskContinuationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        ///     Continue a task and report an Exception if one is raised.
        /// </summary>
        /// <typeparam name="T">The type of Exception to throw.</typeparam>
        /// <param name="task">The task to continue.</param>
        public static void ForgetButThrowWhenFaulted<T>(this Task task)
            where T : Exception
        {
            task.ContinueWith(t => { throw (T)Activator.CreateInstance(typeof(T), t.Exception.Message, t.Exception); }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        ///     Removes and disposes of all instances within the specified <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="concurrentDictionary">The dictionary from which to remove.</param>
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

        /// <summary>
        ///     Returns the MD5 hash of a string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>The MD5 hash of the input string.</returns>
        [SuppressMessage("Microsoft.NetCore.CSharp.Analyzers", "CA5351", Justification = "Required by the Soulseek protocol.")]
        public static string ToMD5Hash(this string str)
        {
            using MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sBuilder.ToString();
        }

        /// <summary>
        ///     Enables TCP KeepAlive packets for this Socket.
        /// </summary>
        /// <remarks>
        ///     https://darchuk.net/2019/01/04/c-setting-socket-keep-alive/
        /// </remarks>
        /// <param name="socket">The socket for which to enable KeepAlives</param>
        /// <param name="delay">The delay since last activity before sending a KeepAlive.</param>
        /// <param name="interval">The interval at which to send KeepAlives.</param>
        public static void EnableKeepAlive(this Socket socket, uint delay, uint interval)
        {
            // Get the size of the uint to use to back the byte array
            int size = Marshal.SizeOf(0U);

            // Create the byte array
            byte[] keepAlive = new byte[size * 3];

            // Pack the byte array:
            // Turn keepalive on
            Buffer.BlockCopy(BitConverter.GetBytes(1U), 0, keepAlive, 0, size);

            // Set amount of time without activity before sending a keepalive
            Buffer.BlockCopy(BitConverter.GetBytes(delay), 0, keepAlive, size, size);

            // Set keepalive interval
            Buffer.BlockCopy(BitConverter.GetBytes(interval), 0, keepAlive, size * 2, size);

            // Set the keep-alive settings on the underlying Socket
            socket.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
        }
    }
}