// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    ///     Interaction logic for BondPropertyEditor.xaml
    /// </summary>
    public partial class BondPropertyEditor : Window
    {
        private readonly BondPropertiesModel _bondPropertiesModel;

        public BondPropertyEditor()
        {
            InitializeComponent();
        }

        public BondPropertyEditor(BondPropertiesModel model) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _bondPropertiesModel = model;
                DataContext = _bondPropertiesModel;
                BondPath.Text = _bondPropertiesModel.Path;
            }
        }

        private void BondPropertyEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            var point = UIUtils.GetOffScreenPoint();
            Left = point.X;
            Top = point.Y;
        }

        private void BondPropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            var point = UIUtils.GetOnScreenPoint(_bondPropertiesModel.Centre, ActualWidth, ActualHeight);
            Left = point.X;
            Top = point.Y;

            InvalidateArrange();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _bondPropertiesModel.Save = true;
            Close();
        }
    }
}