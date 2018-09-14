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
        public System.Windows.Point TopLeft { get; set; }

        public DialogResult Result = DialogResult.Cancel;

        public IChem4WordTelemetry Telemetry { get; set; }

        public string ProductAppDataPath { get; set; }

        public Options UserOptions { get; set; }

        public string OutputValue { get; set; }

        public EditorHost(string cml)
        {
            InitializeComponent();

            WpfChemDoodle editor = new WpfChemDoodle(Telemetry, UserOptions, cml);
            editor.InitializeComponent();
            elementHost1.Child = editor;
            editor.OnButtonClick += OnWpfButtonClick;
        }

        private void OnWpfButtonClick(object sender, EventArgs e)
        {
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
