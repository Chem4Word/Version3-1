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

        public const double DefaultFontSize = 20.0d;
        public const double FontSizePercentageBond = 0.5d;

        public const double ScaleFactorForXaml = 5.0d;
        public const double SingleAtomPseudoBondLength = 40.0d;
        public const double BondOffsetPecentage = 0.1d;
    }
}