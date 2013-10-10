using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris
{
    internal static class Helper
    {
        internal static void VerifyFileExists(string filename, string errorFormat)
        {
            if (File.Exists(filename)) return;
            throw new System.InvalidOperationException(string.Format(errorFormat, filename));
        }

        internal static Type GetType(string typename)
        {
            Type ret = Type.GetType (typename);
            if (ret == null)
            {
                throw new InvalidOperationException(string.Format("Type {0} not found", typename));
            }
            return ret;
        }

        internal static string TranslateFilePath(string path, Configuration config)
        {
            string appdir = AppDomain.CurrentDomain.BaseDirectory;
            if (config != null)
            {
                appdir = config.AppBase;
            }

            return path.Replace("{APPPATH}", appdir);
        }
    }
}
