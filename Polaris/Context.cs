using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris
{
    public class Context
    {
        public Configuration Config                 { get; internal set; }
        public IApplicationController Controller    { get; internal set; }
        public IApplicationHost Host                { get; internal set; }
        public Dispatcher Dispatcher                { get; internal set; }
    }
}
