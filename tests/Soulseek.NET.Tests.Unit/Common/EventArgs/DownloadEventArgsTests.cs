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

namespace Soulseek.NET.Tests.Unit.Common.EventArgs
{
    using AutoFixture.Xunit2;
    using Xunit;

    public class DownloadEventArgsTests
    {
        [Trait("Category", "DownloadEventArgs Instantiation")]
        [Theory(DisplayName = "DownloadEventArgs Instantiates with the given data"), AutoData]
        internal void DownloadEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token)
        {
            var dl = new Download(username, filename, token);
            var d = new DownloadEventArgs(dl);

            Assert.Equal(dl.Username, d.Username);
            Assert.Equal(dl.Filename, d.Filename);
            Assert.Equal(dl.Token, d.Token);
            Assert.Equal(dl.Size, d.Size);
        }

        [Trait("Category", "DownloadProgressEventArgs Instantiation")]
        [Theory(DisplayName = "DownloadProgressEventArgs Instantiates with the given data"), AutoData]
        internal void DownloadProgressEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, int size, int bytesDownloaded)
        {
            var dl = new Download(username, filename, token);
            var d = new DownloadProgressEventArgs(dl, bytesDownloaded);

            Assert.Equal(bytesDownloaded, d.BytesDownloaded);
            Assert.Equal((bytesDownloaded / (double)size) * 100, d.PercentComplete);
        }
    }
}
