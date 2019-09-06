// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Entities;
using Chem4Word.ACME.Models;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for MoleculePropertyEditor.xaml
    /// </summary>
    public partial class MoleculePropertyEditor : Window, INotifyPropertyChanged
    {
        private MoleculePropertiesModel _moleculePropertiesModel;

        public Model MicroModel { get; set; }

        public MoleculePropertiesModel MpeModel
        {
            get
            {
                return _moleculePropertiesModel;
            }
            set
            {
                _moleculePropertiesModel = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MoleculePropertyEditor()
        {
            InitializeComponent();
        }

        public MoleculePropertyEditor(MoleculePropertiesModel model)
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                MpeModel = model;
                DataContext = model;
                MoleculePath.Text = MpeModel.Path;
            }
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _moleculePropertiesModel.Save = true;

            // Get data from labels editor
            _moleculePropertiesModel.SubModel = LabelsEditor.SubModel;

            // Merge in data from the first tab
            var thisMolecule = _moleculePropertiesModel.SubModel.Molecules.First().Value;

            thisMolecule.Count = null;
            if (!string.IsNullOrEmpty(CountSpinner.Text))
            {
                int value;
                if (int.TryParse(CountSpinner.Text, out value))
                {
                    if (value > 0 && value <= 99)
                    {
                        thisMolecule.Count = value;
                    }
                }
            }

            thisMolecule.FormalCharge = null;
            if (ChargeValues.SelectedItem is ChargeValue charge)
            {
                if (charge.Value != 0)
                {
                    thisMolecule.FormalCharge = charge.Value;
                }
            }

            thisMolecule.SpinMultiplicity = null;
            if (SpinMultiplicityValues.SelectedItem is ChargeValue spin)
            {
                if (spin.Value != 0)
                {
                    thisMolecule.SpinMultiplicity = spin.Value;
                }
            }

            thisMolecule.ShowMoleculeBrackets = null;
            if (ShowBracketsValue.IsChecked != null)
            {
                thisMolecule.ShowMoleculeBrackets = ShowBracketsValue.IsChecked;
            }

            Close();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MoleculePropertyEditor_OnLoaded(object sender, RoutedEventArgs e)
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

            PopulateTabOne();
            PopulateTabTwo();
        }

        private void PopulateTabOne()
        {
            Preview.Chemistry = _moleculePropertiesModel.SubModel;
        }

        private void PopulateTabTwo()
        {
            if (!LabelsEditor.IsInitialised)
            {
                CMLConverter cc = new CMLConverter();
                LabelsEditor.Used1D = _moleculePropertiesModel.Used1DProperties;
                LabelsEditor.PopulateTreeView(cc.Export(_moleculePropertiesModel.SubModel));
            }
        }

        private void MoleculePropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            Left = MpeModel.Centre.X - ActualWidth / 2;
            Top = MpeModel.Centre.Y - ActualHeight / 2;

            InvalidateArrange();
        }

        private void CountSpinner_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void CountSpinnerIncreaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CountSpinner.Text))
            {
                CountSpinner.Text = "1";
            }
            else
            {
                int count;
                if (int.TryParse(CountSpinner.Text, out count))
                {
                    if (count < 100)
                    {
                        count++;
                        CountSpinner.Text = count.ToString();
                    }
                }
            }
        }

        private void CountSpinnerDecreaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            int count;
            if (int.TryParse(CountSpinner.Text, out count))
            {
                if (count > 1)
                {
                    count--;
                    CountSpinner.Text = count.ToString();
                }
                else
                {
                    CountSpinner.Text = "";
                }
            }
        }
    }
}