using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using CefSharp.WinForms;

namespace Polaris
{
    public interface IApplicationHost
    {
        bool ChangeView(string relativeUrl);
        WebView View { get; }
    }
}
