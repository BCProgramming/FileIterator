using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCFileSearch
{
    public interface ICancellable
    {
        bool Cancelled { get; set; }
    }
}
