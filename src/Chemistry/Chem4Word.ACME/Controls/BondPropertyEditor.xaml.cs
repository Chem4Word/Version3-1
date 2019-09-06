// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.ACME.Models;

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
            int maxX = Int32.MinValue;
            int maxY = Int32.MinValue;

            foreach (var screen in Screen.AllScreens)
            {
                maxX = Math.Max(maxX, screen.Bounds.Right);
                maxY = Math.Max(maxY, screen.Bounds.Bottom);
            }

            // This moves the window off screen while it renders
            Left = maxX + 100;
            Top = maxY + 100;
        }

        private void BondPropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            Left = _bondPropertiesModel.Centre.X - ActualWidth / 2;
            Top = _bondPropertiesModel.Centre.Y - ActualHeight / 2;

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