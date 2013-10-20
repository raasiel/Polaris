using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polaris.Api
{
    public class ApiSystem : IApiModule
    {

        public string ModuleName
        {
            get { return "system"; }
        }

        private Context _context = null;
        public bool Initialize(Context context)
        {
            _context = context;
            return true;
        }

        [ApiCall(true)]
        public bool quit(bool blah)
        {
            _context.Controller.Quit();
            return true;
        }
    }
}
