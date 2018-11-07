using Soulseek.NET.Tcp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    public class ConnectionStateChangedEventArgs : NetworkEventArgs
    {
        public ConnectionState State { get; set; }
        public string Message { get; set; }
    }
}
