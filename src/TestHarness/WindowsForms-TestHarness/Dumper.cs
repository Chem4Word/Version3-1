﻿// ---------------------------------------------------------------------------
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

namespace WinFormsTestHarness
{
    public partial class Dumper : Form
    {
        public Dumper(string data)
        {
            InitializeComponent();
            Dump.Text = data;
            Dump.SelectionStart = 0;
            Dump.SelectionLength = 0;
        }
    }
}