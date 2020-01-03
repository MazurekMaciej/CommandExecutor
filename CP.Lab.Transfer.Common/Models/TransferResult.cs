using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CP.Lab.Transfer.Common.Models
{
    public class TransferResult
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public int ItemsTransfered { get; set; }
    }
}
