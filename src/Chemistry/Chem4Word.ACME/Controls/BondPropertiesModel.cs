// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Controls
{
    public class BondPropertiesModel : BaseDialogModel
    {
        public string Order { get; set; }
        public string Stereo { get; set; }
        public string Placement { get; set; }
        public double Angle { get; set; }
        public bool IsDouble { get; set; }
    }
}