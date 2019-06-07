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
using Chem4Word.Model2.Converters.CML;

namespace WinForms.TestHarness
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

        private void EditorHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Result != DialogResult.OK && e.CloseReason == CloseReason.UserClosing)
            {
                if (elementHost1.Child is Editor ec && ec.Dirty)
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
                            ec.OnOkButtonClick -= OnWpfButtonClick;
                            break;

                        case DialogResult.No:
                            break;
                    }
                }

                CmlEditor ce = elementHost1.Child as CmlEditor;
                if (ce != null)
                {
                    // We don't care just ignore it
                }
            }
        }
    }
}