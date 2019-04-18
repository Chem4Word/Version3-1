// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.ComponentModel;
using System.Windows;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AtomPropertyEditor.xaml
    /// </summary>
    public partial class AtomPropertyEditor : Window
    {
        private AtomPropertiesModel _model;

        public AtomPropertyEditor()
        {
            InitializeComponent();
        }

        public AtomPropertyEditor(AtomPropertiesModel model)
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _model = model;
                DataContext = _model;
                Title = _model.Title;
            }
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (VaidateModel())
            {
                _model.Save = true;
                Close();
            }
        }

        private bool VaidateModel()
        {
            ElementBase eb;
            bool b1 = AtomHelpers.TryParse(_model.Symbol, out eb);

            int n;
            bool b2 = string.IsNullOrEmpty(_model.Charge);
            if (!b2)
            {
                b2 = int.TryParse(_model.Charge, out n);
            }

            bool b3 = string.IsNullOrEmpty(_model.Isotope);
            if (!b3)
            {
                b3 = int.TryParse(_model.Isotope, out n);
            }

            return b1 && b2 && b3;
        }

        private void Dialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = _model.Centre.X - ActualWidth / 2;
            Top = _model.Centre.Y - ActualHeight / 2;
        }
    }
}