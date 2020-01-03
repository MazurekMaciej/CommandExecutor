using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common
{
    public interface IOutputPlugin : IHelper, IDisposable
    {
        void Init(string outputParameters);
        void Init(IDictionary<string, string> parameters);
        void Write(IDictionary<string, object> item);
    }
}
