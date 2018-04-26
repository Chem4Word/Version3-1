// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Model
{
    public static class Globals
    {
        public static PeriodicTable PeriodicTable = new PeriodicTable();
        public const double VectorTolerance = 0.01d;

        // LineThickness of Bond if all else fails
        public const double DefaultBondLineThickness = 2.5;
        // Font Size to use if all else fails
        public const double DefaultFontSize = 20.0d;
        // Imaginary Bond Size for Single Atom
        public const double SingleAtomPseudoBondLength = 40.0d;
        // Calculate Font size as bond length * FontSizePercentageBond
        public const double FontSizePercentageBond = 0.5d;
        // Double Bond Offset as %age of bond length
        public const double BondOffsetPecentage = 0.1d;
        // How much to magnify CML by for rendering
        public const double ScaleFactorForXaml = 5.0d;
    }
}