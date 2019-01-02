﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model.Converters;
using Chem4Word.Model.Converters.CML;

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
            if (Result != DialogResult.OK && e.CloseReason == CloseReason.UserClosing)
            {
                Chem4Word.ACME.Editor ec = elementHost1.Child as Chem4Word.ACME.Editor;
                if (ec != null)
                {
                    if (ec.Dirty)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Do you wish to save your changes?");
                        sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                        sb.AppendLine("  Click 'No' to discard your changes and exit.");
                        sb.AppendLine("  Click 'Cancel' to return to the form.");
                        DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                        switch (dr)
                        {
                            case DialogResult.Cancel:
                                e.Cancel = true;
                                break;

                            case DialogResult.Yes:
                                Result = DialogResult.OK;
                                CMLConverter cc = new CMLConverter();
                                OutputValue = cc.Export(ec.Data);
                                Hide();
                                ec.OnOkButtonClick -= OnWpfOkButtonClick;
                                ec = null;
                                break;

                            case DialogResult.No:
                                ec = null;
                                break;
                        }
                    }
                }
            }
        }
    }
}