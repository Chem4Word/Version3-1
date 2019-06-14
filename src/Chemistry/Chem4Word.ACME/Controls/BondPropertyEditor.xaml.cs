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

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    ///     Interaction logic for BondPropertyEditor.xaml
    /// </summary>
    public partial class BondPropertyEditor : Window
    {
        private readonly BondPropertiesModel _model;

        public BondPropertyEditor()
        {
            InitializeComponent();
        }

        public BondPropertyEditor(BondPropertiesModel model) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _model = model;
                DataContext = _model;
                BondPath.Text = _model.Path;
            }
        }

        private void BondPropertyEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            // This gets it close to the final position
            Left = _model.Centre.X - ActualWidth / 2;
            Top = _model.Centre.Y - ActualHeight / 2;
        }

        private void BondPropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves it to the correct position
            Left = _model.Centre.X - ActualWidth / 2;
            Top = _model.Centre.Y - ActualHeight / 2;

            InvalidateArrange();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (ValidateModel())
            {
                _model.Save = true;
                Close();
            }
        }

        private bool ValidateModel() => true;
    }
}