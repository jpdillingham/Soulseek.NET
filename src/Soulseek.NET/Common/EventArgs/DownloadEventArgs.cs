// <copyright file="DownloadEventArgs.cs" company="JP Dillingham">
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
    ///     Generic event arguments for download events.
    /// </summary>
    public class DownloadEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadEventArgs"/> class.
        /// </summary>
        /// <param name="download">The download which raised the event.</param>
        internal DownloadEventArgs(Download download)
        {
            Username = download.Username;
            Filename = download.Filename;
            Token = download.Token;
            Size = download.Size;
            State = download.State;
        }

        /// <summary>
        ///     Gets the filename of the file to be downloaded.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the size of the file to be downloaded, in bytes.
        /// </summary>
        public int Size { get; }

        /// <summary>
        ///     Gets the state of the download.
        /// </summary>
        public DownloadStates State { get; }

        /// <summary>
        ///     Gets the unique token for thr transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer from which the file is to be downloaded.
        /// </summary>
        public string Username { get; }
    }

    /// <summary>
    ///     Event arguments for events raised by an update to download progress.
    /// </summary>
    public sealed class DownloadProgressEventArgs : DownloadEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadProgressEventArgs"/> class.
        /// </summary>
        /// <param name="download">The download which raised the event.</param>
        /// <param name="bytesDownloaded">The total number of bytes read.</param>
        internal DownloadProgressEventArgs(Download download, int bytesDownloaded)
            : base(download)
        {
            BytesDownloaded = bytesDownloaded;
        }

        /// <summary>
        ///     Gets the total number of bytes read.
        /// </summary>
        public int BytesDownloaded { get; }

        /// <summary>
        ///     Gets the current progress in percent.
        /// </summary>
        public double PercentComplete => (BytesDownloaded / (double)Size) * 100;
    }

    /// <summary>
    ///     Event arguments for events raised by a change in download state.
    /// </summary>
    public sealed class DownloadStateChangedEventArgs : DownloadEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the download.</param>
        /// <param name="download">The download which raised the event.</param>
        internal DownloadStateChangedEventArgs(DownloadStates previousState, Download download)
            : base(download)
        {
            PreviousState = previousState;
        }

        /// <summary>
        ///     Gets the previous state of the download.
        /// </summary>
        public DownloadStates PreviousState { get; }
    }
}