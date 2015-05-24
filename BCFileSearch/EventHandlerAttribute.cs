using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCFileSearch
{
    class EventHandlerAttribute : Attribute
    {
        public bool IgnoreCancelled { get; private set; }
        public EventHandlerAttribute(bool IgnoreCancelled=false)
        {
            this.IgnoreCancelled = IgnoreCancelled;
        }
    }
}
