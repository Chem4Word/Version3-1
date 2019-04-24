// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for BondPropertyEditor.xaml
    /// </summary>
    public partial class BondPropertyEditor : Window
    {
        private BondPropertiesModel _model;

        private bool _closing = false;

        public BondPropertyEditor()
        {
            InitializeComponent();
        }

        public BondPropertyEditor(BondPropertiesModel model)
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _model = model;
                DataContext = _model;
                Title = _model.Title;
                Closing += OnClosing;
                Deactivated += OnDeactivated;
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _closing = true;
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            if (!_closing)
            {
                _model.Save = false;
                Close();
            }
        }

        private void Dialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = _model.Centre.X - ActualWidth / 2;
            Top = _model.Centre.Y - ActualHeight / 2;
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (ValidateModel())
            {
                _model.Save = true;
                _closing = true;
                Close();
            }
        }

        private bool ValidateModel()
        {
            return true;
        }
    }
}