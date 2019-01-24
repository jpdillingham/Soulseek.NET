// <copyright file="HandleDownloadAsyncTests.cs" company="JP Dillingham">
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

using Moq;
using Soulseek.NET.Messaging.Messages;
using Soulseek.NET.Tcp;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Soulseek.NET.Tests.Unit.Client
{
    public class HandleDownloadAsyncTests
    {
        [Trait("Category", "HandleDownloadAsync")]
        [Fact(DisplayName = "Does not throw on download missing from ActiveDownloads")]
        public async Task Does_Not_Throw_On_Download_Missing_From_ActiveDownloads()
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(1)));

            var s = new SoulseekClient("127.0.0.1", 1, null);
            var r = new ConnectToPeerResponse("username", "F", IPAddress.Parse("127.0.0.1"), 1, 1);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object));

            Assert.Null(ex);
        }
    }
}
