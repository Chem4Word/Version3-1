// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;
using Chem4Word.Model2;

namespace Chem4Word.Model2.Helpers
{
    public static class Globals
    {
        #region Geometry Stuff

        public enum ClockDirections
        {
            Nothing = 0,
            I,
            II,
            III,
            IV,
            V,
            VI,
            VII,
            VIII,
            IX,
            X,
            XI,
            XII
        }

        #endregion Geometry Stuff

        #region Bond Stuff

        public enum BondDirection
        {
            Anticlockwise = -1,
            None = 0,
            Clockwise = 1
        }

        public enum BondStereo
        {
            None,
            Wedge,
            Hatch,
            Indeterminate,
            Cis,
            Trans
        }

        public const string OrderZero = "hbond";

        public const string OrderOther = "other";
        public const string OrderPartial01 = "partial01";
        public const string OrderSingle = "S";
        public const string OrderPartial12 = "partial12";
        public const string OrderAromatic = "A";
        public const string OrderDouble = "D";
        public const string OrderPartial23 = "partial23";
        public const string OrderTriple = "T";
        


        public static string OrderValueToOrder(double val, bool isAromatic = false)
        {
            if (val == 0)
            {
                return OrderZero;
            }
            if (val == 0.5)
            {
                return OrderPartial01;
            }
            if (val == 1)
            {
                return OrderSingle;
            }
            if (val == 1.5)
            {
                if (isAromatic)
                {
                    return OrderAromatic;
                }
                else
                {
                    return OrderPartial12;
                }
            }
            if (val == 2)
            {
                return OrderDouble;
            }
            if (val == 2.5)
            {
                return OrderPartial23;
            }
            if (val == 3)
            {
                return OrderTriple;
            }
            if (val == 4)
            {
                return OrderAromatic;
            }
            return OrderZero;
        }

        public static double? OrderToOrderValue(string order)

        {
            switch (order)
            {
                case OrderZero:
                case OrderOther:
                    return 0;

                case OrderPartial01:
                    return 0.5;

                case OrderSingle:
                    return 1;

                case OrderPartial12:
                    return 1.5;

                case OrderAromatic:
                    return 1.5;

                case OrderDouble:
                    return 2;

                case OrderPartial23:
                    return 2.5;

                case OrderTriple:
                    return 3;

                default:
                    return null;
            }
        }

        #endregion Bond Stuff

        #region Layout Constants

        public const double VectorTolerance = 0.01d;

        // LineThickness of Bond if all else fails
        public const double DefaultBondLineFactor = 1.0;

        // Font Size to use if all else fails
        public const double DefaultFontSize = 20.0d;

        // Imaginary Bond Size for Single Atom
        public const double SingleAtomPseudoBondLength = 40.0d;

        // Calculate Font size as bond length * FontSizePercentageBond
        public const double FontSizePercentageBond = 0.5d;

        // Double Bond Offset as %age of bond length
        public const double BondOffsetPercentage = 0.1d;

        // How much to magnify CML by for rendering in Display or Editor
        public const double ScaleFactorForXaml = 2.0d;

        // Percentage of Average bond length for any added Explicit Hydrogens
        public const double ExplicitHydrogenBondPercentage = 1.0;

        public const double BondThickness = ScaleFactorForXaml * 0.8;
        public const double HoverAdornerThickness = 3.0;
        public static Color HoverAdornerColor => Colors.DarkOrange;
        #endregion Layout Constants

        public static PeriodicTable PeriodicTable = new PeriodicTable();
        public static Dictionary<string, FunctionalGroup> FunctionalGroupsDictionary = FunctionalGroups.ShortcutList;
    }
}