using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common.Exceptions
{
    public class InputParametersNotFoundException : Exception
    {
        public string parameterName { get; }

        public InputParametersNotFoundException() { }

        public InputParametersNotFoundException(string message)
        : base(message) { }

        public InputParametersNotFoundException(string message, Exception inner)
      : base(message, inner) { }

        public InputParametersNotFoundException(string message, string _parameterName)
            : this(message)
        {
            parameterName = _parameterName;
        }
    }
}
