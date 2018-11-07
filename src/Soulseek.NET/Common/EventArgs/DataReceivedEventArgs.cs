using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    public class DataReceivedEventArgs : NetworkEventArgs
    {
        public byte[] Data;
    }
}
