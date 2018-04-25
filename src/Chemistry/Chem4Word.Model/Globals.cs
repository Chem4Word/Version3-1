﻿// ---------------------------------------------------------------------------
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
        public const double VectorTolerance = 0.01;

        public const double DefaultFontSize = 20.0d;
        public const double FontSizePercentageBond = 0.5d;

        public const double ScaleFactorForXaml = 10.0;
        public const double EstimatedAverageBondSize = 200.0d;
    }
}