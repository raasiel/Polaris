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

        private string GetModuleLoaderCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("window.loadModules = function() {\n");
            foreach (string key in _dicModules.Keys)
            {
                IApiModule module = _dicModules[key];
                string modName = module.ModuleName;
                sb.AppendFormat("window.polaris.{0} = new {0}();", modName);
            }
            sb.Append("\n}");
            return sb.ToString();

        }

        public string GetModuleCode()
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (string key in _dicModules.Keys)
            {
                IApiModule module = _dicModules[key];
                string modName = module.ModuleName;
                BuildModuleCode(module, sb);
            }

            sb.Append(GetModuleLoaderCode());
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
                ApiCallAttribute attr = null;
                try
                {
                    object[] attrs = mi.GetCustomAttributes(typeof(ApiCallAttribute), true);
                    if (attrs.Length > 0)
                    {
                        attr = attrs[0] as ApiCallAttribute;
                    }
                }
                catch (Exception ex)
                {
                }

                string methodName = mi.Name;
                if (attr != null)
                {
                    sb.AppendFormat("{0}.prototype.{1} = function (", modName, methodName);
                    ParameterInfo[] pis = mi.GetParameters();
                    string parmString = "";
                    StringBuilder sbParm = new StringBuilder();
                    foreach (ParameterInfo pi in pis)
                    {
                        parmString = parmString + string.Format("{0},", pi.Name);
                        sbParm.AppendFormat("\tparms.push({0});\n",pi.Name);
                    }
                    if (parmString.Length>0)
                    {
                        parmString = parmString.Substring(0, parmString.Length - 1);                        
                    }
                    sb.AppendFormat("{0},retFunc)", parmString);
                    sb.Append("{\n");
                    sb.Append("\tparms=[];\n");
                    sb.Append(sbParm.ToString());
                    

                    sb.Append("\tparmJson = JSON.stringify(parms);\n");
                    sb.Append("\nalert(parmJson);\n");
                    sb.AppendFormat("\n\twindow.polaris.dispatch(\"{0}\",\"{1}\",parmJson,retFunc);\n", modName, methodName);
                    sb.Append("}\n");
                }
            }
            
        }
    }
}
