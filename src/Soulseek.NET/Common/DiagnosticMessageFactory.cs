using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    internal class DiagnosticMessageFactory : IDiagnosticMessageFactory
    {
        private object Source { get; }
        private EventHandler<DiagnosticMessageGeneratedEventArgs> EventHandler { get; }

        public DiagnosticMessageFactory(object source, EventHandler<DiagnosticMessageGeneratedEventArgs> eventHandler)
        {
            Source = source;
            EventHandler = eventHandler;
        }

        public void Debug(string message)
        {
            RaiseEvent(DiagnosticMessageLevel.Debug, message);
        }

        public void Error(string message, Exception exception = null)
        {
            RaiseEvent(DiagnosticMessageLevel.Error, message, exception);
        }

        public void Info(string message)
        {
            RaiseEvent(DiagnosticMessageLevel.Info, message);
        }

        public void Warning(string message, Exception exception = null)
        {
            RaiseEvent(DiagnosticMessageLevel.Warning, message, exception);
        }

        private void RaiseEvent(DiagnosticMessageLevel level, string message, Exception exception = null)
        {
            var e = new DiagnosticMessageGeneratedEventArgs(level, message, exception);
            EventHandler?.Invoke(Source, e);
        }
    }
}
