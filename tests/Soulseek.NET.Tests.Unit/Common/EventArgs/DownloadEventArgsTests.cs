// <copyright file="DownloadEventArgsTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit
{
    using System;
    using AutoFixture.Xunit2;
    using Xunit;

    public class DownloadEventArgsTests
    {
        [Trait("Category", "DownloadEventArgs Instantiation")]
        [Theory(DisplayName = "DownloadEventArgs Instantiates with the given data"), AutoData]
        internal void DownloadEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, DownloadOptions options)
        {
            var dl = new Download(username, filename, token, options);
            var d = new DownloadEventArgs(dl);

            Assert.Equal(0, d.AverageSpeed);
            Assert.Equal(0, d.BytesDownloaded);
            Assert.Equal(0, d.BytesRemaining);
            Assert.Equal(default(TimeSpan), d.ElapsedTime);
            Assert.Equal(default(TimeSpan), d.RemainingTime);
            Assert.Null(d.StartTime);
            Assert.Null(d.EndTime);
            Assert.Null(d.IPAddress);
            Assert.Equal(0, d.PercentComplete);
            Assert.Null(d.Port);
            Assert.Equal(0, d.RemoteToken);
            Assert.Equal(0, d.Size);
            Assert.Equal(dl.Username, d.Username);
            Assert.Equal(dl.Filename, d.Filename);
            Assert.Equal(dl.Token, d.Token);
            Assert.Equal(dl.State, d.State);
            Assert.Equal(options, d.Options);
        }

        [Trait("Category", "DownloadProgressUpdatedEventArgs Instantiation")]
        [Theory(DisplayName = "DownloadProgressUpdatedEventArgs Instantiates with the given data"), AutoData]
        internal void DownloadProgressUpdatedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, int size, int bytesDownloaded)
        {
            var dl = new Download(username, filename, token);
            dl.Size = size;

            var d = new DownloadProgressUpdatedEventArgs(bytesDownloaded, dl);

            Assert.Equal(bytesDownloaded, d.PreviousBytesDownloaded);
        }

        [Trait("Category", "DownloadStateChangedEventArgs Instantiation")]
        [Theory(DisplayName = "DownloadStateChangedEventArgs Instantiates with the given data"), AutoData]
        internal void DownloadStateChangedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, DownloadStates downloadStates)
        {
            var dl = new Download(username, filename, token);
            var d = new DownloadStateChangedEventArgs(downloadStates, dl);

            Assert.Equal(downloadStates, d.PreviousState);
        }
    }
}
