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

namespace Polaris
{
    public partial class DefaultApplicationHost : Form, IApplicationHost
    {
        public DefaultApplicationHost()
        {
            InitializeComponent();
            wbrMain.DocumentTitleChanged += wbrMain_DocumentTitleChanged;
        }

        void wbrMain_DocumentTitleChanged(object sender, EventArgs e)
        {
            this.Text = wbrMain.Document.Title;
        }

        public bool Initialize(Context context)
        {
            return false;
        }

        public bool ChangeView(string relativeUrl)
        {
            wbrMain.Navigate ( relativeUrl);    
            return true;
        }

    }
}
