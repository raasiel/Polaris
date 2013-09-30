using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Polaris
{
    public interface IApiProvider
    {
        bool RegisterAvailableModules(Context context);
    }
}
