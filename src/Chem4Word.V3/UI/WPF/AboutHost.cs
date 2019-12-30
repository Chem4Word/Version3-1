// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Windows.Forms;
using Chem4Word.Core.UI.Forms;

namespace Chem4Word.UI.WPF
{
    public partial class AboutHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string VersionString { get; set; }
        public System.Windows.Point TopLeft { get; set; }
        public bool AutoClose { get; set; }

        public AboutHost()
        {
            InitializeComponent();
        }

        private void AboutHost_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
                aboutControl1.TopLeft = TopLeft;
                aboutControl1.AutoClose = AutoClose;
                if (AutoClose)
                {
                    ShowInTaskbar = false;
                    aboutControl1.OnPreloadComplete += OnPreloadCompleted;
                }
                else
                {
                    aboutControl1.VersionString = VersionString;
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        private void OnPreloadCompleted(object sender, EventArgs e)
        {
            Close();
        }
    }
}