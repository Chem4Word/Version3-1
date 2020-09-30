// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
//
// Created by Gergely István Oroszi - 13-Aug-2013

using System;
using System.Globalization;

namespace Chem4Word.Core.Helpers
{
    public class SafeDouble
    {
        public static double Parse(string source)
        {
            var separatorChar = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            var fixedSource = source.Replace(separatorChar, '.');
            var result = Convert.ToDouble(fixedSource, CultureInfo.InvariantCulture);

            return result;
        }

        public static string Duration(double duration)
        {
            return AsString(duration);
        }

        public static string AsString(double value)
        {
            return value.ToString("#,##0.00", CultureInfo.InvariantCulture);
        }

        public static string AsString4(double value)
        {
            return value.ToString("#,##0.0000", CultureInfo.InvariantCulture);
        }

        public static string AsString0(double value)
        {
            return value.ToString("#,##0", CultureInfo.InvariantCulture);
        }
    }
}