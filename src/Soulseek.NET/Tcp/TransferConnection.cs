// <copyright file="TransferConnection.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tcp
{
    using System;
    using System.Threading.Tasks;

    internal class TransferConnection : Connection, ITransferConnection
    {
        internal TransferConnection(string address, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
            : base(address, port, options, tcpClient)
        {
        }

        public event EventHandler<DataSentEventArgs> DataSent;
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public async Task SendAsync(byte[] bytes)
        {
            await base.SendAsync(bytes);
        }

        public async Task<byte[]> ReadAsync(long count)
        {
            try
            {
                var intCount = (int)count;
                return await ReadAsync(intCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"adsfasfdsa");
                throw new NotImplementedException($"File sizes exceeding ~2gb are not yet supported.");
            }
        }

        public async Task<byte[]> ReadAsync(int count)
        {
            return await base.ReadAsync(count);
        }

        protected override void DataSentHandler(byte[] data)
        {
            Task.Run(() => DataSent?.Invoke(this, new DataSentEventArgs(NetworkEventArgs) { Data = data }));
        }

        protected override void DataReceivedHandler(byte[] data)
        {
            Task.Run(() => DataReceived?.Invoke(this, new DataReceivedEventArgs(NetworkEventArgs) { Data = data }));
        }
    }
}