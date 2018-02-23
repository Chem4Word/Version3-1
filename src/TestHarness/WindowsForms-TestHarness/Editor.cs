// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Converters;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WinFormsTestHarness
{
    public partial class Editor : Form
    {
        public Editor()
        {
            InitializeComponent();
        }

        private void CancelEdit_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void SaveChanges_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}