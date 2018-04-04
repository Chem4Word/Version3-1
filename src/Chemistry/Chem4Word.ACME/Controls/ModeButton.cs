// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Chem4Word.ACME.Controls
{
    public class ModeButton :RadioButton
    {



        public Behavior<FrameworkElement> ActiveModeBehavior
        {
            get { return (Behavior<FrameworkElement>)GetValue(ActiveModeBehaviorProperty); }
            set { SetValue(ActiveModeBehaviorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveModeBehavior.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveModeBehaviorProperty =
            DependencyProperty.Register("ActiveModeBehavior", typeof(Behavior<FrameworkElement>), typeof(ModeButton), new PropertyMetadata(null));





    }
}
