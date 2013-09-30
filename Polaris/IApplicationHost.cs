using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris
{
    public interface IApplicationHost
    {
        bool ChangeView(string relativeUrl);
    }
}
