using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common.Exceptions
{
    public class FilePathException : Exception
    {
        public string filePath { get; }

        public FilePathException() { }

        public FilePathException(string message)
        : base(message) { }

        public FilePathException(string message, Exception inner)
      : base(message, inner) { }

        public FilePathException(string message, string _filePath)
            : this(message)
        {
            filePath = _filePath;
        }
    }
}
