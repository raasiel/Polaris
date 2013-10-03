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

            this.Controls.Add(this.wbrMain);

            
        }

        void wbrMain_DocumentTitleChanged(object sender, EventArgs e)
        {
            //this.Text = wbrMain.Document.Title;
        }

        public bool Initialize(Context context)
        {
            return false;
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
