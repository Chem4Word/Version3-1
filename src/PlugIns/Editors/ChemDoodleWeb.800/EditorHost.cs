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

namespace Chem4Word.Editor.ChemDoodleWeb800
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

            WpfChemDoodle editor = elementHost1.Child as WpfChemDoodle;
            if (editor != null)
            {
                editor.Cml = cml;
                editor.OnButtonClick += OnWpfButtonClick;
            }
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
