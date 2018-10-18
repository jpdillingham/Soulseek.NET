namespace Soulseek.NET.Tcp
{
    using System;

    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data;
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState State { get; set; }
        public string Message { get; set; }
    }
}
