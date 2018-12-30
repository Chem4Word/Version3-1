// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using System;
using System.Drawing;
using System.Windows.Forms;
using Chem4Word.Core.UI.Wpf;

namespace WinFormsTestHarness
{
    public partial class EditorHost : Form
    {
        public DialogResult Result = DialogResult.Cancel;

        public string OutputValue { get; set; }

        public EditorHost(string cml, string type)
        {
            InitializeComponent();
            this.MinimumSize = new Size(300, 200);

            if (type.Equals("ACME"))
            {
                Editor ec = new Editor(cml);
                ec.InitializeComponent();
                elementHost1.Child = ec;
                ec.ShowSave = true;
                ec.OnOkButtonClick += OnWpfButtonClick;
            }
            else
            {
                CmlEditor ec = new CmlEditor(cml);
                ec.InitializeComponent();
                elementHost1.Child = ec;
                ec.OnButtonClick += OnWpfButtonClick;
            }
        }

        private void EditorHost_Load(object sender, EventArgs e)
        {

        }

        private void OnWpfButtonClick(object sender, EventArgs e)
        {
            WpfEventArgs args = (WpfEventArgs)e;
            if (args.Button.Equals("OK") || args.Button.Equals("SAVE"))
            {
                Result = DialogResult.OK;
                OutputValue = args.OutputValue;
                Hide();
            }
        }
    }
}