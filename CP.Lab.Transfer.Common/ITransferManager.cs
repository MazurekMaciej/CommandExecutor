using CP.Lab.Transfer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common
{
    public interface ITransferManager : IHelper, IDisposable
    {
        void Init(string parameters);
        void Init(IDictionary<string, string> parameters);
        TransferResult Transfer(IInputPlugin inputPlugin, IOutputPlugin outputPlugin);
    }
}
