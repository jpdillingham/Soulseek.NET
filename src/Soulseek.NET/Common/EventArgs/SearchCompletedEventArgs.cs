using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET
{
    public class SearchCompletedEventArgs : EventArgs
    {
        public Search Search { get; set; }
    }
}
