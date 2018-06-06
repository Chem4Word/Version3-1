// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Windows.Forms;
using Chem4Word.ACME;

namespace Chem4Word.Editor.ACME
{
    public partial class EditorHost : Form
    {
        public System.Windows.Point TopLeft { get; set; }

        public Size FormSize { get; set; }

        public DialogResult Result = DialogResult.Cancel;

        public string OutputValue { get; set; }

        public EditorHost(string cml)
        {
            InitializeComponent();

            this.MinimumSize = new Size(300, 200);

            Chem4Word.ACME.Editor ec = new Chem4Word.ACME.Editor(cml);
            ec.InitializeComponent();
            elementHost1.Child = ec;
            ec.OnOkButtonClick += OnWpfOkButtonClick;
        }

        private void OnWpfOkButtonClick(object sender, EventArgs e)
        {
            WpfEventArgs args = (WpfEventArgs)e;
            if (args.Button.Equals("SAVE"))
            {
                Result = DialogResult.OK;
                OutputValue = args.OutputValue;
                Hide();
            }
        }

        private void EditorHost_Load(object sender, EventArgs e)
        {
            if (TopLeft.X != 0 && TopLeft.Y != 0)
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;
            }
            if (FormSize.Width != 0 && FormSize.Height != 0)
            {
                Width = FormSize.Width;
                Height = FormSize.Height;
            }
        }

        private void EditorHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            Chem4Word.ACME.Editor ec = elementHost1.Child as Chem4Word.ACME.Editor;
            if (ec.Dirty)
            {
                e.Cancel = true;
            }
            else
            {
                ec.OnOkButtonClick -= OnWpfOkButtonClick;
                ec = null;
            }
        }
    }
}