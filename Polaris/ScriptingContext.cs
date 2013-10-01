using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Polaris
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)] 
    public class ScriptingContext
    {
        public void SendMessage(string module, string method, string parameters, int callId)
        {
            return module + ":" + method;
        }
    }
}
