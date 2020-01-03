using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common.Exceptions
{
    public class InvalidConnectionStringException : Exception
    {
        public string conString { get; }

        public InvalidConnectionStringException() { }

        public InvalidConnectionStringException(string message)
        : base(message) { }

        public InvalidConnectionStringException(string message, Exception inner)
      : base(message, inner) { }

        public InvalidConnectionStringException(string message, string _conString)
            : this(message)
        {
            conString = _conString;
        }
    }
}
