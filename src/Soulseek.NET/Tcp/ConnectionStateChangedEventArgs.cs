namespace Soulseek.NET.Tcp
{
    using System;

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState State { get; set; }
    }
}
