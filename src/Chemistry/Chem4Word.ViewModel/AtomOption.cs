// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ViewModel
{
    public class AtomOption : ComboBoxItem
    {
        public ElementBase Element
        {
            get => (ElementBase)GetValue(ElementProperty);
            set => SetValue(ElementProperty, value);
        }

        // Using a DependencyProperty as the backing store for Element.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register("Element", typeof(ElementBase), typeof(AtomOption), new PropertyMetadata(default(ElementBase)));

        public Style DisplayStyle
        {
            get => (Style)GetValue(DisplayStyleProperty);
            set => SetValue(DisplayStyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for DisplayStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayStyleProperty =
            DependencyProperty.Register("DisplayStyle", typeof(Style), typeof(AtomOption), new PropertyMetadata(default(Style)));
    }
}