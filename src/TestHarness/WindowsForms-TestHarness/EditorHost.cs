// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsTestHarness
{
    public partial class EditorHost : Form
    {
        public DialogResult Result = DialogResult.Cancel;

        public string OutputValue { get; set; }

        public EditorHost(string cml)
        {
            InitializeComponent();
            this.MinimumSize = new Size(300, 200);

            Editor ec = new Editor(cml);
            ec.InitializeComponent();
            elementHost1.Child = ec;
            ec.OnOkButtonClick += OnWpfOkButtonClick;
        }

        private void EditorHost_Load(object sender, EventArgs e)
        {

        }

        private void OnWpfOkButtonClick(object sender, EventArgs e)
        {
            WpfEventArgs args = (WpfEventArgs)e;
            if (args.Button.Equals("OK"))
            {
                Result = DialogResult.OK;
                OutputValue = args.OutputValue;
                Hide();
            }
        }
    }
}