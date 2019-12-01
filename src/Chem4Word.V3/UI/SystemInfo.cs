// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;

namespace Chem4Word.UI
{
    public partial class SystemInfo : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public SystemInfo()
        {
            InitializeComponent();
        }

        private void SystemInfo_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (TopLeft.X != 0 && TopLeft.Y != 0)
                {
                    Left = (int)TopLeft.X;
                    Top = (int)TopLeft.Y;
                }

                StringBuilder sb = new StringBuilder();

                #region Add In Version

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                UpdateHelper.ReadThisVersion(assembly);
                string version = string.Empty;
                if (Globals.Chem4WordV3.ThisVersion != null)
                {
                    string[] parts = Globals.Chem4WordV3.ThisVersion.Root.Element("Number").Value.Split(' ');
                    string temp = Globals.Chem4WordV3.ThisVersion.Root.Element("Number").Value;
                    int idx = temp.IndexOf(" ");
                    version = $"Chem4Word 2020 {temp.Substring(idx + 1)} [{fvi.FileVersion}]";
                }
                else
                {
                    version = $"Chem4Word Version: V{fvi.FileVersion}";
                }

                sb.AppendLine(version);

                #endregion Add In Version

                sb.AppendLine("");
                sb.AppendLine($"MachineId: {Globals.Chem4WordV3.Helper.MachineId}");
                sb.AppendLine($"Operating System: {Globals.Chem4WordV3.Helper.SystemOs}");
                sb.AppendLine($"Word Product: {Globals.Chem4WordV3.Helper.WordProduct}");
                sb.AppendLine($"Internet Explorer Version: {Globals.Chem4WordV3.Helper.BrowserVersion}");
                sb.AppendLine($".Net Framework Runtime: {Globals.Chem4WordV3.Helper.DotNetVersion}");

                sb.AppendLine("");
                sb.AppendLine($"Settings Folder: {Globals.Chem4WordV3.AddInInfo.ProductAppDataPath}");
                sb.AppendLine($"Library Folder: {Globals.Chem4WordV3.AddInInfo.ProgramDataPath}");

                //sb.AppendLine("");

                Information.Text = sb.ToString();
                Information.SelectionStart = Information.Text.Length;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}