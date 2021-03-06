﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CefSharp.WinForms;
using Newtonsoft.Json;

namespace Polaris
{
    public class Dispatcher
    {
        List<Thread> _workerThreads = new List<Thread>();
        Queue<Dispatch> _dispatches = new Queue<Dispatch>();
        private static object _lockHandle = new object();

        private Context _context = null;
        private ScriptingContext _scriptContext = null;
        IApiProvider _api = null;
        private bool _isRunning = false;

        public Dispatcher()
        {
            this.WorkerCount = 1;
        }

        public void Initialize(Context context, ScriptingContext scriptContext)
        {
            _context = context;
            _scriptContext = scriptContext;
            _scriptContext.OnTaskReceive = this.AddTask;
            _scriptContext.OnSyncTask = this.ExecuteTask;
            _scriptContext.OnCodeInject = this.GetCodeToInject;
            _api = Activator.CreateInstance(_context.Config.ApiProviderType) as IApiProvider;
            _api.RegisterAvailableModules(_context);
            this.Start();
        }        

        public int WorkerCount { get; set; }

        private string GetCodeToInject()
        {
            return _api.GetModuleCode();            
        }

        private void Start()
        {
            _isRunning = true;
            for (int i = 0; i <= this.WorkerCount; i++)
            {
                Thread t = new Thread(new ThreadStart(this.ProcessQueue));
                _workerThreads.Add(t);
                t.Start();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _workerThreads.Clear();
        }

        public void ExecuteTask(Dispatch task)
        {
            if (task != null)
            {
                task.Result = _api.ProcessApiCall(task);
            }
        }


        public void AddTask(Dispatch task)
        {
            lock (_lockHandle)
            {
                _dispatches.Enqueue(task);
            }
        }

        public void ProcessQueue()
        {
            while (_isRunning)
            {
                Dispatch task = null;
                if (_dispatches.Count > 0)
                {
                    lock (_lockHandle)
                    {
                        if (_dispatches.Count > 0)
                        {
                            task = _dispatches.Dequeue();
                        }
                    }
                }
                if (task != null)
                {
                    task.Result =_api.ProcessApiCall(task);
                    string resultJson = JsonConvert.SerializeObject(task.Result);
                    string scriptString = "window.polaris.pollMessages(" + task.CallId.ToString() + "," + resultJson + ");";
                    _context.Host.View.ExecuteScript(scriptString);
                    task = null;
                }
                Thread.Sleep(200);
            }
        }
    }
}
