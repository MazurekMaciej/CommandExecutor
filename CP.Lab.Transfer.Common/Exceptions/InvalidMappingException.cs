using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common.Exceptions
{
    public class InvalidMappingException : Exception
    {
        public string mappingParameter { get; }

        public InvalidMappingException() { }

        public InvalidMappingException(string message)
        : base(message) { }

        public InvalidMappingException(string message, Exception inner)
      : base(message, inner) { }

        public InvalidMappingException(string message, string _mappingParameter)
            : this(message)
        {
            mappingParameter = _mappingParameter;
        }
    }
}
