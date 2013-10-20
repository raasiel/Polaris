using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Polaris.Api
{
    public class ApiFile :IApiModule
    {
        public string ModuleName
        {
            get { return "file"; }
        }

        private Context _context = null;
        public bool Initialize(Context context)
        {
            _context = context;
            return true;
        }

        [ApiCall(false)]
        public bool isExists(string filename)
        {
            return System.IO.File.Exists(filename);
        }

        [ApiCall(false)]
        public string whatIsMyName(string name)
        {
            return string.Format("My name is {0}", name);
        }


        [ApiCall(true)]
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
