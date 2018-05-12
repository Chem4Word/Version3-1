// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using Chem4Word.Core.Helpers;
using Chem4Word.Model;

namespace Chem4Word.ViewModel
{
    public class BondLengthOption : DependencyObject
    {
        public int ChosenValue
        {
            get { return (int)GetValue(ChosenValueProperty); }
            set { SetValue(ChosenValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Id.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChosenValueProperty =
            DependencyProperty.Register("ChosenValue", typeof(int),
                typeof(BondLengthOption),
                new PropertyMetadata((int)(Constants.StandardBondLength * Globals.ScaleFactorForXaml)));

        public string DisplayAs
        {
            get { return (string)GetValue(DisplayAsProperty); }
            set { SetValue(DisplayAsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Order.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayAsProperty =
            DependencyProperty.Register("DisplayAs", typeof(string),
                typeof(BondLengthOption),
                new PropertyMetadata(Constants.StandardBondLength.ToString("0")));
    }
}