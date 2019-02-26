// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Chem4Word.ACME.Controls
{
    public class EditorCanvas : ChemistryCanvas
    {
        protected override Size MeasureOverride(Size constraint)
        {
            return DesiredSize;
        }
    }
}
