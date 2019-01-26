using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    internal interface IDiagnosticMessageFactory
    {
        void Error(string message, Exception exception = null);
        void Warning(string message, Exception exception = null);
        void Info(string message);
        void Debug(string message);
    }
}
