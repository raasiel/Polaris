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
            _context.Host.ChangeView(Helper.TranslateFilePath(_context.Config.StartPage, _context.Config));
            this.HookIntoView();

            hostForm.Show();
            hostForm.Focus();
            Application.Run();
        }

        private void HookIntoView()
        {
            ScriptingContext sc = new ScriptingContext();
            sc.Host = _context.Host;
            _context.Host.View.ObjectForScripting = sc;
            
        }
    }
}
