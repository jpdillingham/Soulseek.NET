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

    internal class Download
    {
        internal Download(string username, string filename, int token, int size = 0)
        {
            Username = username;
            Filename = filename;
            Token = token;
            Size = size;
        }

        public string Username { get; private set; }
        public string Filename { get; private set; }
        public int Token { get; private set; }
        public int RemoteToken { get; set; }
        public int Size { get; set; }
        public int PlaceInQueue { get; set; }
        public DownloadStates State { get; set; } = DownloadStates.Queued;
        public byte[] Data { get; set; }
        public IConnection Connection { get; set; }
        public string WaitKey => $"{Username}:{Filename}:{Token}";
    }
}
