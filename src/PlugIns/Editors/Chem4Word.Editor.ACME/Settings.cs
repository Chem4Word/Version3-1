using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Core.UI.Wpf;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.ACME
{
    public partial class Settings : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public Options EditorOptions { get; set; }

        public string SettingsPath { get; set; }

        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            if (TopLeft.X != 0 && TopLeft.Y != 0)
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
            }

            UserSettings us = elementHost1.Child as UserSettings;
            if (us != null)
            {
                us.Options = EditorOptions;
                us.Telemetry = Telemetry;
                us.TopLeft = TopLeft;
                us.OnButtonClick += UsOnOnButtonClick;
            }
        }

        private void UsOnOnButtonClick(object sender, WpfEventArgs e)
        {
            if (e.Button.Equals("CANCEL"))
            {
                Close();
            }

            if (e.Button.Equals("SAVE"))
            {
                Close();
            }
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            UserSettings us = elementHost1.Child as UserSettings;
            if (us != null)
            {
                if (us.Options.Dirty)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Do you wish to save your changes?");
                    sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                    sb.AppendLine("  Click 'No' to discard your changes and exit.");
                    sb.AppendLine("  Click 'Cancel' to return to the form.");

                    DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                    switch (dr)
                    {
                        case DialogResult.Yes:
                            FileUtils.SaveAcmeSettings(us.Options, Telemetry, TopLeft);
                            break;

                        case DialogResult.No:
                            break;

                        case DialogResult.Cancel:
                            e.Cancel = true;
                            break;
                    }

                }
            }
        }
    }
}
