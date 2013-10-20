using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polaris
{
    interface  IApiModule
    {
        bool Initialize(Context context);
        string ModuleName { get; }
    }
}
