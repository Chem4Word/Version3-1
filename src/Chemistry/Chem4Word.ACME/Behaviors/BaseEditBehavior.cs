// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Behaviors
{
    public class BaseEditBehavior : Behavior<Canvas>
    {
        public EditViewModel ViewModel
        {
            get { return (EditViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(EditViewModel), typeof(BaseEditBehavior), new PropertyMetadata(null));
    }
}