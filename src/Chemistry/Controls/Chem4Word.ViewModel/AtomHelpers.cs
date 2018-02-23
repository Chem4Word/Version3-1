// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.View
{
    public static class AtomHelpers
    {
        public static string GetSubText(int hCount = 0)
        {
            string mult = "";
            int i;
            if ((i = Math.Abs(hCount)) > 1)
            {
                mult = i.ToString();
            }

            return mult;
        }

        /// <summary>
        /// gets the charge annotation string for an atom symbol
        /// </summary>
        /// <param name="charge">Int contaioning the charge value</param>
        /// <returns></returns>
        public static string GetChargeString(int? charge)
        {
            string chargestring = "";

            if ((charge ?? 0) > 0)
            {
                chargestring = "+";
            }
            if ((charge ?? 0) < 0)
            {
                chargestring = "-";
            }
            int abscharge = 0;
            if ((abscharge = Math.Abs(charge ?? 0)) > 1)
            {
                chargestring = abscharge.ToString() + chargestring;
            }
            return chargestring;
        }
    }
}