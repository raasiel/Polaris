using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

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

        public object ProcessApiCall(Dispatch task)
        {
            IApiModule module = _dicModules[task.Module];
            Type modType = module.GetType();
            MethodInfo method = modType.GetMethod(task.Method);
            object ret = method.Invoke (module, task.Parameters );
            return ret;
        }

    }
}
