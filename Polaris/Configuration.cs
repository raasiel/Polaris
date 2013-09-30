using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris
{
    public class Configuration
    {
        public string AppBase { get; internal set; }
        public string StartPage { get; internal set; }
        public Type AppControllerType { get; internal set; }
        public Type ApiProviderType { get; internal set; }
        public Type HostType { get; internal set; }
        public Type[] ApiModules { get; internal set; }
    }
}
