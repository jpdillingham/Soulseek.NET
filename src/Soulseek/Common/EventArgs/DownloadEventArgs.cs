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

namespace Soulseek
{
    using System;
    using System.Net;

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
            AverageSpeed = download.AverageSpeed;
            BytesDownloaded = download.BytesDownloaded;
            BytesRemaining = download.BytesRemaining;
            Data = download.Data;
            ElapsedTime = download.ElapsedTime;
            EndTime = download.EndTime;
            Filename = download.Filename;
            IPAddress = download.IPAddress;
            PercentComplete = download.PercentComplete;
            Port = download.Connection?.Port;
            RemainingTime = download.RemainingTime;
            RemoteToken = download.RemoteToken;
            Size = download.Size;
            StartTime = download.StartTime;
            State = download.State;
            Token = download.Token;
            Username = download.Username;
            Options = download.Options;
        }

        /// <summary>
        ///     Gets the current average download speed.
        /// </summary>
        public double AverageSpeed { get; }

        /// <summary>
        ///     Gets the total number of bytes downloaded.
        /// </summary>
        public long BytesDownloaded { get; }

        /// <summary>
        ///     Gets the number of remaining bytes to be downloaded.
        /// </summary>
        public long BytesRemaining { get; }

        /// <summary>
        ///     Gets the data downloaded.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        ///     Gets the current duration of the download, if it has been started.
        /// </summary>
        public TimeSpan? ElapsedTime { get; }

        /// <summary>
        ///     Gets the time at which the download transitioned into the <see cref="DownloadStates.Completed"/> state.
        /// </summary>
        public DateTime? EndTime { get; }

        /// <summary>
        ///     Gets the filename of the file to be downloaded.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the ip address of the remote transfer connection, if one has been established.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the options for the transfer.
        /// </summary>
        public DownloadOptions Options { get; }

        /// <summary>
        ///     Gets the current progress in percent.
        /// </summary>
        public double PercentComplete { get; }

        /// <summary>
        ///     Gets the port of the remote transfer connection, if one has been established.
        /// </summary>
        public int? Port { get; }

        /// <summary>
        ///     Gets the projected remaining duration of the download.
        /// </summary>
        public TimeSpan? RemainingTime { get; }

        /// <summary>
        ///     Gets the remote unique token for the transfer.
        /// </summary>
        public int RemoteToken { get; }

        /// <summary>
        ///     Gets the size of the file to be downloaded, in bytes.
        /// </summary>
        public long Size { get; }

        /// <summary>
        ///     Gets the time at which the download transitioned into the <see cref="DownloadStates.InProgress"/> state.
        /// </summary>
        public DateTime? StartTime { get; }

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
    public sealed class DownloadProgressUpdatedEventArgs : DownloadEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadProgressUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="previousBytesDownloaded">The previous total number of bytes downloaded.</param>
        /// <param name="download">The download which raised the event.</param>
        internal DownloadProgressUpdatedEventArgs(long previousBytesDownloaded, Download download)
            : base(download)
        {
            PreviousBytesDownloaded = previousBytesDownloaded;
        }

        /// <summary>
        ///     Gets the total number of bytes read.
        /// </summary>
        public long PreviousBytesDownloaded { get; }
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