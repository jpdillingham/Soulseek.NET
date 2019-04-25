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
            Download = download;
        }

        /// <summary>
        ///     Gets the download associated with the event.
        /// </summary>
        public Download Download { get; }
    }

    /// <summary>
    ///     Event arguments for events raised by an update to download progress.
    /// </summary>
    public sealed class DownloadProgressUpdatedEventArgs : DownloadEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadProgressUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="previousBytesDownloaded">The previous total number of bytes downloaded.</param>
        /// <param name="download">The download which raised the event.</param>
        internal DownloadProgressUpdatedEventArgs(int previousBytesDownloaded, Download download)
            : base(download)
        {
            PreviousBytesDownloaded = previousBytesDownloaded;
        }

        /// <summary>
        ///     Gets the total number of bytes read.
        /// </summary>
        public int PreviousBytesDownloaded { get; }
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