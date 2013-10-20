using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Polaris
{
    public class DefaultAppController : IApplicationController
    {
        Context _context = new Context();
        public bool Initialize(Context context)
        {
            _context = context;

            // It is the responsibility of the App controller to update context 
            // with the self reference. 
            _context.Controller = this;

            return true;
        }

        public void Run()
        {
            // Load the visual form
            InitAppHost();
        }

        private void InitAppHost()
        {
            _context.Host = Activator.CreateInstance(_context.Config.HostType) as IApplicationHost;
            Form hostForm = _context.Host as Form;
            _context.Host.Initialize(_context);
            _context.Host.ChangeView(Helper.TranslateFilePath(_context.Config.StartPage, _context.Config));
            Dispatcher disp = new Dispatcher();
            _context.Dispatcher = disp;
            hostForm.Show();
            this.HookIntoView(disp);
            hostForm.Focus();
            hostForm.FormClosed += new FormClosedEventHandler(hostForm_FormClosed);
            Application.Run();
            
        }

        void hostForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Quit();
        }

        public void Quit()
        {
            _context.Dispatcher.Stop();
            Application.Exit();

        }


        private void HookIntoView(Dispatcher dispatcher)
        {
            ScriptingContext sc = new ScriptingContext();
            _context.Host.View.RegisterJsObject("polarisConn", sc);
            dispatcher.Initialize(_context, sc);
        }
    }
}
