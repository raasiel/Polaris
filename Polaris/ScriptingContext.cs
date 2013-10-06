using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Polaris
{
    public class ScriptingContext
    {
        public delegate void NotifyTaskArrival(Dispatch task);

        public NotifyTaskArrival OnTaskReceive { get; set; }
        public NotifyTaskArrival OnSyncTask { get; set; }


        public string SendMessageSync(string module, string method, string parameters)
        {
            Dispatch task = new Dispatch();
            task.Module = module;
            task.Method = method;

            JArray jparms = JsonConvert.DeserializeObject(parameters) as JArray;
            List<object> parms = new List<object>();
            foreach (JValue jitem in jparms)
            {
                parms.Add(jitem.Value);
            }

            task.Parameters = parms.ToArray();

            if (this.OnSyncTask != null)
            {
                this.OnSyncTask(task);
            }
            return JsonConvert.SerializeObject(task.Result);
        }


        public void SendMessage(string module, string method, string parameters, int callId)
        {
            //MessageBox.Show(module);
            Dispatch task = new Dispatch();
            task.CallId = callId;
            task.Module = module;
            task.Method = method;

            JArray jparms = JsonConvert.DeserializeObject(parameters) as JArray;
            List<object> parms = new List<object>();
            foreach (JValue jitem in jparms)
            {
                parms.Add(jitem.Value);
            }

            task.Parameters = parms.ToArray();

            if (this.OnTaskReceive != null)
            {
                this.OnTaskReceive(task);
            }
        }
    }
}
