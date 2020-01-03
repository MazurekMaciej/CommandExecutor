using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common.Exceptions
{
    public class InvalidPluginException : Exception
    {
        public string pluginName { get; }

        public InvalidPluginException() { }

        public InvalidPluginException(string message)
        : base(message) { }

        public InvalidPluginException(string message, Exception inner)
      : base(message, inner) { }

        public InvalidPluginException(string message, string _pluginName)
            : this(message)
        {
            pluginName = _pluginName;
        }
    }
}
