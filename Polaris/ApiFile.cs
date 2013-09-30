using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polaris
{
    public class ApiFile :IApiModule
    {
        public string ModuleName
        {
            get { return "file"; }
        }

        public bool FileExists(string filename)
        {
            return System.IO.File.Exists(filename);
        }
    }
}
