using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    public class NetworkEventArgs : EventArgs
    {
        public string Address;
        public string IPAddress;
        public int Port;
    }
}
