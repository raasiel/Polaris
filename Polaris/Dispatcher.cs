using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Polaris
{
    internal class Dispatcher
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
            _api = Activator.CreateInstance(_context.Config.ApiProviderType) as IApiProvider;
            _api.RegisterAvailableModules(_context);
            this.Start();
        }

        public int WorkerCount { get; set; }

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
                    _api.ProcessApiCall(task);
                    task = null;
                }
                Thread.Sleep(200);
            }
        }
    }
}
