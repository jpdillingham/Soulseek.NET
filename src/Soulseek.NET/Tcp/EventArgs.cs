namespace Soulseek.NET.Tcp
{
    using System;

    public class DataReceivedEventArgs : NetworkEventArgs
    {
        public byte[] Data;
    }

    public class ConnectionStateChangedEventArgs : NetworkEventArgs
    {
        public ConnectionState State { get; set; }
        public string Message { get; set; }
    }

    public class NetworkEventArgs : EventArgs
    {
        public string Address;
        public string IPAddress;
        public int Port;
    }
}
