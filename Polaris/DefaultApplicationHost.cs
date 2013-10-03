using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp.WinForms;

namespace Polaris
{
    public partial class DefaultApplicationHost : Form, IApplicationHost
    {

        private WebView wbrMain;

        public DefaultApplicationHost()
        {
            InitializeComponent();


            this.wbrMain = new WebView();
            
            // 
            // wbrMain
            // 
            this.wbrMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wbrMain.Location = new System.Drawing.Point(0, 0);
            this.wbrMain.MinimumSize = new System.Drawing.Size(20, 20);
            this.wbrMain.Name = "wbrMain";
            this.wbrMain.Size = new System.Drawing.Size(739, 397);
            this.wbrMain.TabIndex = 0;

            wbrMain.PropertyChanged += wbrMain_PropertyChanged;
            this.Controls.Add(this.wbrMain);
            this.FormClosed += DefaultApplicationHost_FormClosed;
            
        }

        bool _browserReady = false;
        void wbrMain_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.PropertyName);
            if (_browserReady == false)
            {
                if (wbrMain.IsBrowserInitialized == true)
                {
                    if (_context.Dispatcher != null)
                    {
                        _browserReady = true;
                        _context.Dispatcher.ViewReady();
                    }
                }
            }
        }

        void DefaultApplicationHost_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        void wbrMain_DocumentTitleChanged(object sender, EventArgs e)
        {
            //this.Text = wbrMain.Document.Title;
        }

        Context _context = null;
        public bool Initialize(Context context)
        {
            _context = context;
            return true;
        }

        public bool ChangeView(string relativeUrl)
        {
            wbrMain.Address = relativeUrl;
            //wbrMain.Navigate ( relativeUrl);    
            return true;
        }

        public WebView View
        {
            get { return wbrMain; }
        }

    }
}
