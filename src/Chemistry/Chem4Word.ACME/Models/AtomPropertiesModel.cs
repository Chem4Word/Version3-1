// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME.Models
{
    public class AtomPropertiesModel : BaseDialogModel
    {
        public string Symbol { get; set; }
        public string Charge { get; set; }
        public string Isotope { get; set; }
    }
}