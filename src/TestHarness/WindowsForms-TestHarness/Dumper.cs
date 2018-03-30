using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsTestHarness
{
    public partial class Dumper : Form
    {
        public Dumper(string data)
        {
            InitializeComponent();
            Dump.Text = data;
            Dump.SelectionStart = 0;
            Dump.SelectionLength = 0;
        }
    }
}
