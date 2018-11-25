namespace Soulseek.NET
{
    using System;

    [Flags]
    public enum DownloadState
    {
        Queued = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 4,
        TimedOut = 8,
    }
}
