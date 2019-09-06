// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.UI.WPF
{
    public partial class EditLabelsHost : Form
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public System.Windows.Point TopLeft { get; set; }
        public string Cml { get; set; }
        public List<string> Used1D { get; set; }
        public string Message { get; set; }

        private bool _closedInCode = false;

        public EditLabelsHost()
        {
            InitializeComponent();
        }

        private void OnWpfButtonClick(object sender, EventArgs e)
        {
            WpfEventArgs args = (WpfEventArgs)e;
            switch (args.Button.ToLower())
            {
                case "ok":
                case "save":
                    DialogResult = DialogResult.OK;
                    _closedInCode = true;
                    if (elementHost1.Child is LabelsEditor labelsEditor)
                    {
                        Cml = args.OutputValue;
                        Hide();
                    }
                    break;

                case "cancel":
                    DialogResult = DialogResult.Cancel;
                    _closedInCode = true;
                    Hide();
                    break;
            }
        }

        private void EditLabelsHost_Load(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Left = (int)TopLeft.X;
                Top = (int)TopLeft.Y;

                if (elementHost1.Child is LabelsEditor labelsEditor)
                {
                    labelsEditor.TopLeft = TopLeft;
                    labelsEditor.Used1D = Used1D;
                    labelsEditor.PopulateTreeView(Cml);
                    labelsEditor.Message = Message;
                    labelsEditor.OnButtonClick += OnWpfButtonClick;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }
        }

        private void EditLabelsHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closedInCode)
            {
                if (elementHost1.Child is LabelsEditor labelsEditor)
                {
                    if (labelsEditor.IsDirty)
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
                                var cmlConvertor = new CMLConverter();
                                Cml = cmlConvertor.Export(labelsEditor.SubModel);
                                DialogResult = DialogResult.OK;
                                break;

                            case DialogResult.No:
                                DialogResult = DialogResult.Cancel;
                                break;
                        }
                    }
                }
            }
        }
    }
}