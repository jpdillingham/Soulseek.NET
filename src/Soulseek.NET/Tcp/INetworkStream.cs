using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Soulseek.NET.Tcp
{
    interface INetworkStream : IDisposable
    {
        Task WriteAsync(byte[] buffer, int offset, int count);
        Task<int> ReadAsync(byte[] buffer, int offset, int count);
        void Close();
    }
}
