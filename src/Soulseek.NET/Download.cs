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
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     A single file download.
    /// </summary>
    internal class Download
    {
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
        ///     Gets or sets the connection used for the transfer.
        /// </summary>
        public IConnection Connection { get; set; }

        /// <summary>
        ///     Gets or sets the data downloaded.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        ///     Gets the filename of the file to be downloaded.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets or sets the remote unique token for the transfer.
        /// </summary>
        public int RemoteToken { get; set; }

        /// <summary>
        ///     Gets or sets the size of the file to be downloaded, in bytes.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        ///     Gets or sets the state of the download.
        /// </summary>
        public DownloadStates State { get; set; } = DownloadStates.None;

        /// <summary>
        ///     Gets the unique token for thr transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer from which the file is to be downloaded.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets tue unique wait key for the download.
        /// </summary>
        public WaitKey WaitKey => new WaitKey(Username, Filename, Token);
    }
}