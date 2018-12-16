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
    using System.Collections.Generic;

    public class DownloadEventArgs : EventArgs
    {
        internal DownloadEventArgs(Download download)
        {
            Username = download.Username;
            Filename = download.Filename;
            Token = download.Token;
            Size = download.Size;
        }

        public string Username { get; private set; }
        public string Filename { get; private set; }
        public int Token { get; private set; }
        public int Size { get; private set; }
    }

    public class DownloadProgressEventArgs : DownloadEventArgs
    {
        internal DownloadProgressEventArgs(Download download, int bytesDownloaded)
            : base(download)
        {
            BytesDownloaded = bytesDownloaded;
        }

        public int BytesDownloaded { get; private set; }
        public double PercentComplete => (BytesDownloaded / (double)Size) * 100;
    }

    public class DownloadStateChangedEventArgs : DownloadEventArgs
    {
        internal DownloadStateChangedEventArgs(Download download)
            : base(download)
        {
            State = download.State;
            PlaceInQueue = download.PlaceInQueue;
            Data = download.Data;
        }

        public DownloadState State { get; private set; }
        public int PlaceInQueue { get; internal set; }
        public IEnumerable<byte> Data { get; internal set; }
    }
}
