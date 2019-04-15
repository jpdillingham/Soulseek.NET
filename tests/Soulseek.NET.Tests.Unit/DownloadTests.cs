// <copyright file="DownloadTests.cs" company="JP Dillingham">
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
    using System.Net;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class DownloadTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        internal void Instantiates_With_The_Given_Data(string username, string filename, int token)
        {
            var d = new Download(username, filename, token);

            Assert.Equal(username, d.Username);
            Assert.Equal(filename, d.Filename);
            Assert.Equal(token, d.Token);
        }

        [Trait("Category", "Properties")]
        [Theory(DisplayName = "Properties default to expected values"), AutoData]
        internal void Properties_Default_To_Expected_Values(string username, string filename, int token)
        {
            var d = new Download(username, filename, token);

            Assert.Null(d.Connection);
            Assert.Null(d.IPAddress);
            Assert.Null(d.Port);
            Assert.Null(d.Data);
            Assert.Equal(0, d.RemoteToken);
            Assert.Equal(0, d.Size);
            Assert.Equal(DownloadStates.None, d.State);
        }

        [Trait("Category", "Properties")]
        [Theory(DisplayName = "IPAddress and Port props return Connection props"), AutoData]
        internal void IPAddress_And_Port_Props_Return_Connection_Props(string username, string filename, int token, IPAddress ipAddress, int port)
        {
            var d = new Download(username, filename, token);

            var c = new Mock<IConnection>();
            c.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            c.Setup(m => m.Port)
                .Returns(port);

            d.Connection = c.Object;

            Assert.Equal(ipAddress, d.IPAddress);
            Assert.Equal(port, (int)d.Port);
        }

        [Trait("Category", "Wait Key")]
        [Theory(DisplayName = "Wait key is expected value"), AutoData]
        internal void Wait_Key_Is_Expected_Value(string username, string filename, int token)
        {
            var d = new Download(username, filename, token);

            Assert.Equal(new WaitKey(username, filename, token), d.WaitKey);
        }
    }
}
