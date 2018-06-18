// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;

namespace Chem4Word.UI.WPF
{
    public partial class WpfLoader : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public DialogResult Result = DialogResult.Cancel;
        public string OutputValue { get; set; }

        public WpfLoader()
        {
            InitializeComponent();
        }

        public WpfLoader(string wpfControl)
        {
            InitializeComponent();
            if (wpfControl.Equals("Settings"))
            {
                SettingsControl sc = new SettingsControl();
                sc.InitializeComponent();
                elementHost1.Child = sc;
                sc.OnButtonClick += OnWpfButtonClick;
                Text = "Chem4Word Settings";
            }
        }

        private void OnWpfButtonClick(object sender, EventArgs e)
        {
            WpfEventArgs args = (WpfEventArgs)e;
            if (args.Button.Equals("Ok"))
            {
                Result = DialogResult.OK;
                OutputValue = args.OutputValue;
                Hide();
            }
        }

        public System.Windows.Point TopLeft { get; set; }

        private void WpfLoader_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (TopLeft.X != 0 && TopLeft.Y != 0)
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                }

                // Do something here ...
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }

        }
    }
}
