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
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model.Converters.CML;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.ChemDoodleWeb800
{
    public partial class EditorHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }

        public DialogResult Result = DialogResult.Cancel;

        public IChem4WordTelemetry Telemetry { get; set; }

        public string ProductAppDataPath { get; set; }

        public Options UserOptions { get; set; }

        private string _cml;

        public string OutputValue { get; set; }

        public EditorHost(string cml)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _cml = cml;
            InitializeComponent();
        }

        private void EditorHost_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Cursor.Current = Cursors.WaitCursor;

            if (TopLeft.X != 0 && TopLeft.Y != 0)
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
            }

            this.Show();
            Application.DoEvents();

            WpfChemDoodle editor = new WpfChemDoodle(Telemetry, ProductAppDataPath, UserOptions, _cml);
            editor.InitializeComponent();
            elementHost1.Child = editor;
            editor.OnButtonClick += OnWpfButtonClick;
        }

        private void OnWpfButtonClick(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            WpfEventArgs args = (WpfEventArgs)e;
            if (args.Button.ToUpper().Equals("OK"))
            {
                Result = DialogResult.OK;
                OutputValue = args.OutputValue;
                Hide();
            }
        }
    }
}
