namespace Soulseek.NET
{
    using System;

    public class ServerStateChangedEventArgs : EventArgs
    {
        public ServerState State { get; set; }
    }
}
