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

        public string GetModuleCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string key in _dicModules.Keys)
            {
                BuildModuleCode(_dicModules[key], sb);
            }
            return sb.ToString() ;
        }

        private void BuildModuleCode(IApiModule module, StringBuilder sb)
        {
            string modName = module.ModuleName;
            sb.AppendFormat("function {0}() ", modName);
            sb.Append(" {  };\n");
            Type typ = module.GetType();
            MethodInfo[] mis = typ.GetMethods();
            foreach (MethodInfo mi in mis)
            {
                string methodName = mi.Name;
                if ((!methodName.StartsWith("get_")) && (!methodName.StartsWith("set_")) 
                    && (!methodName.Equals("ToString"))&& (!methodName.Equals("Equals"))
                    && (!methodName.Equals("GetHashCode")) && (!methodName.Equals("GetType")))
                {
                    sb.AppendFormat("{0}.prototype.{1} = function (", modName, methodName);
                    ParameterInfo[] pis = mi.GetParameters();
                    string parmString = "";
                    foreach (ParameterInfo pi in pis)
                    {
                        parmString = parmString + string.Format("{0},", pi.Name);
                    }
                    if (parmString.Length>0)
                    {
                        parmString = parmString.Substring(0, parmString.Length - 1);
                    }
                    sb.AppendFormat("{0},retFunc)", parmString);
                    sb.Append("{\n");
                    sb.AppendFormat("\twindow.polaris.dispatch(\"{0}\",\"{1}\",{2},retFunc);\n", modName, methodName, parmString);
                    sb.Append("}\n");
                }
            }

            sb.AppendFormat("polaris.prototype.{0} = new {0}();", modName);
        }
    }
}
