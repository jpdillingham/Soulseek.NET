// <copyright file="Download.cs" company="JP Dillingham">
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
    using System.Net;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     A single file download.
    /// </summary>
    public sealed class Download
    {
        private DownloadStates state = DownloadStates.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Download"/> class.
        /// </summary>
        /// <param name="username">The username of the peer from which the file is to be downloaded.</param>
        /// <param name="filename">The filename of the file to be downloaded.</param>
        /// <param name="token">The unique token for the transfer.</param>
        internal Download(string username, string filename, int token)
        {
            Username = username;
            Filename = filename;
            Token = token;
        }

        /// <summary>
        ///     Gets the data downloaded.
        /// </summary>
        public byte[] Data { get; internal set; }

        /// <summary>
        ///     Gets the current duration of the download, if it has been started.
        /// </summary>
        public TimeSpan? Duration => (EndTime ?? DateTime.Now) - StartTime;

        /// <summary>
        ///     Gets the time at which the download transitioned into the <see cref="DownloadStates.Completed"/> state.
        /// </summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>
        ///     Gets the filename of the file to be downloaded.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the ip address of the remote transfer connection, if one has been established.
        /// </summary>
        public IPAddress IPAddress => Connection?.IPAddress;

        /// <summary>
        ///     Gets the port of the remote transfer connection, if one has been established.
        /// </summary>
        public int? Port => Connection?.Port;

        /// <summary>
        ///     Gets the remote unique token for the transfer.
        /// </summary>
        public int RemoteToken { get; internal set; }

        /// <summary>
        ///     Gets the size of the file to be downloaded, in bytes.
        /// </summary>
        public int Size { get; internal set; }

        /// <summary>
        ///     Gets the time at which the download transitioned into the <see cref="DownloadStates.InProgress"/> state.
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        ///     Gets the state of the download.
        /// </summary>
        public DownloadStates State
        {
            get
            {
                return state;
            }

            internal set
            {
                if (!state.HasFlag(DownloadStates.InProgress) && value.HasFlag(DownloadStates.InProgress))
                {
                    StartTime = DateTime.Now;
                    EndTime = null;
                }
                else if (!state.HasFlag(DownloadStates.Completed) && value.HasFlag(DownloadStates.Completed))
                {
                    EndTime = DateTime.Now;
                }

                state = value;
            }
        }

        /// <summary>
        ///     Gets the unique token for thr transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer from which the file is to be downloaded.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets or sets the connection used for the transfer.
        /// </summary>
        /// <remarks>Ensure that the reference instance is disposed when the transfer is complete.</remarks>
        internal IConnection Connection { get; set; }

        /// <summary>
        ///     Gets tue unique wait key for the download.
        /// </summary>
        internal WaitKey WaitKey => new WaitKey(Username, Filename, Token);
    }
}