// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.ACME
{
    public class ViewModel
    {


        public ViewModel(Model2.Model chemistryModel)
        {
            Model = chemistryModel;
        }
        #region Properties

        public  Model2.Model Model { get; }

        #endregion
    }
}
