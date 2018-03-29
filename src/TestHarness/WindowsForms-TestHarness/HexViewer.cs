using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsTestHarness
{
    public partial class HexViewer : Form
    {
        public HexViewer(string filename)
        {
            InitializeComponent();
            ByteViewer bv = new ByteViewer();
            bv.SetFile(filename);
            bv.Dock = DockStyle.Fill;
            bv.SetDisplayMode(DisplayMode.Hexdump);
            Controls.Add(bv);
        }
    }
}
