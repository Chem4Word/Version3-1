// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;

namespace Wpf.TestHarness.Model2
{
    public class NullToBoolConverter:IValueConverter
    {
      

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}

