namespace Soulseek.NET
{ 
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DiagnosticEventArgs : EventArgs
    {
    }

    public class DiagnosticMessageGeneratedEventArgs : DiagnosticEventArgs
    {
        public DiagnosticMessageGeneratedEventArgs(DiagnosticMessageLevel level, string message, Exception exception = null)
        {
            Level = level;
            Message = message;
            Exception = exception;
        }

        public DiagnosticMessageLevel Level { get; }
        public string Message { get; }
        public bool IncludesException => Exception == null;
        public Exception Exception { get; }
    }
}
