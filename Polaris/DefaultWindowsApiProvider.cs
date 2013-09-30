using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polaris
{
    public class DefaultWindowsApiProvider : IApiProvider
    {
        Context _context = null;
        private Dictionary<string, IApiModule> _dicModules = new Dictionary<string, IApiModule>();

        public bool RegisterAvailableModules(Context context)
        {
            _context = context;
            foreach (Type typ in context.Config.ApiModules)
            {
                IApiModule module = Activator.CreateInstance(typ) as IApiModule;
                _dicModules.Add(module.ModuleName, module);
            }

            return true;
        }

    }
}
