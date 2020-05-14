// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME
{
    public class ViewModel
    {
        public ViewModel(Model chemistryModel)
        {
            Model = chemistryModel;

            double xamlBondLength = chemistryModel.XamlBondLength == 0
                ? Globals.DefaultFontSize * 2
                : chemistryModel.XamlBondLength;

            SetTextParams(xamlBondLength);
        }

        public void SetTextParams(double bondLength)
        {
            GlyphText.SymbolSize = bondLength / 2.0d;
            GlyphText.ScriptSize = GlyphText.SymbolSize * 0.6;
            GlyphText.IsotopeSize = GlyphText.SymbolSize * 0.8;
        }

        #region Properties

        public Model Model { get; }

        #endregion Properties
    }
}