using System;
using System.Collections.Generic;
using System.IO;
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

        [ApiCall(false)]
        public bool isExists(string filename)
        {
            return System.IO.File.Exists(filename);
        }

        [ApiCall(false)]
        public string[] getFiles(string path, string pattern)
        {
            List<string> ret = new List<string>();
            DirectoryInfo dir = new DirectoryInfo (path);
            foreach (FileInfo file in dir.GetFiles(pattern))
            {
                ret.Add(file.FullName);
            }
            return ret.ToArray();
        }

    }
}
