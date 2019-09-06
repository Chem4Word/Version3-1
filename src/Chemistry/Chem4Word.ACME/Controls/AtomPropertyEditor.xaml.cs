// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Resources;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AtomPropertyEditor.xaml
    /// </summary>
    public partial class AtomPropertyEditor : Window, INotifyPropertyChanged
    {
        private AtomPropertiesModel _atomPropertiesModel;

        public AtomPropertiesModel AtomPropertiesModel
        {
            get
            {
                return _atomPropertiesModel;
            }
            set
            {
                _atomPropertiesModel = value;
                OnPropertyChanged();
            }
        }

        public AtomPropertyEditor()
        {
            InitializeComponent();
        }

        public AtomPropertyEditor(AtomPropertiesModel model) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                AtomPropertiesModel = model;
                DataContext = AtomPropertiesModel;
                AtomPath.Text = AtomPropertiesModel.Path;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _atomPropertiesModel.Save = true;
            Close();
        }

        private void AtomPropertyEditor_OnLoaded(object sender, RoutedEventArgs e)
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

            LoadAtomItems();
            LoadFunctionalGroups();
            ShowPreview();
        }

        private void AtomPropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            Left = AtomPropertiesModel.Centre.X - ActualWidth / 2;
            Top = AtomPropertiesModel.Centre.Y - ActualHeight / 2;

            InvalidateArrange();
        }

        private void AtomTable_OnElementSelected(object sender, VisualPeriodicTable.ElementEventArgs e)
        {
            AtomOption newOption = null;
            var selElement = e.SelectedElement as Element;
            AtomPropertiesModel.Element = selElement;
            PeriodicTableExpander.IsExpanded = false;
            bool found = false;

            foreach (var item in AtomPicker.Items)
            {
                var option = (AtomOption)item;
                if (option.Element is Element el)
                {
                    if (el == selElement)
                    {
                        found = true;
                        newOption = option;
                        break;
                    }
                }

                if (option.Element is FunctionalGroup fg)
                {
                    // Ignore any Functional Groups in the picker (if present)
                }
            }

            if (!found)
            {
                newOption = new AtomOption(selElement);
                AtomPicker.Items.Add(newOption);
                AtomPropertiesModel.AddedElement = selElement;
            }

            var atomPickerSelectedItem = newOption;
            AtomPicker.SelectedItem = atomPickerSelectedItem;
            ShowPreview();
        }

        private void AtomPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = AtomPicker.SelectedItem as AtomOption;
            AtomPropertiesModel.AddedElement = option?.Element;
            ShowPreview();
        }

        private void FunctionalGroupPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = FunctionalGroupPicker.SelectedItem as AtomOption;
            AtomPropertiesModel.AddedElement = option?.Element;
            ShowPreview();
        }

        private void ChargeCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowPreview();
        }

        private void IsotopePicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowPreview();
        }

        private void ExplicitCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            ShowPreview();
        }

        private void LoadAtomItems()
        {
            AtomPicker.Items.Clear();
            foreach (var item in Constants.StandardAtoms)
            {
                AtomPicker.Items.Add(new AtomOption(Globals.PeriodicTable.Elements[item]));
            }

            if (AtomPropertiesModel.Element is Element el)
            {
                if (!Constants.StandardAtoms.Contains(el.Symbol))
                {
                    AtomPicker.Items.Add(new AtomOption(Globals.PeriodicTable.Elements[el.Symbol]));
                }

                AtomPicker.SelectedItem = new AtomOption(AtomPropertiesModel.Element as Element);
            }
        }

        private void LoadFunctionalGroups()
        {
            FunctionalGroupPicker.Items.Clear();
            foreach (var item in Globals.FunctionalGroupsDictionary)
            {
                FunctionalGroupPicker.Items.Add(new AtomOption(item.Value));
            }

            if (AtomPropertiesModel.IsFunctionalGroup)
            {
                FunctionalGroupPicker.SelectedItem = new AtomOption(AtomPropertiesModel.Element as FunctionalGroup);
            }
        }

        private void ShowPreview()
        {
            var atoms = AtomPropertiesModel.MicroModel.GetAllAtoms();
            var atom = atoms[0];

            if (AtomPropertiesModel.IsElement)
            {
                atom.Element = AtomPropertiesModel.Element;
                atom.FormalCharge = AtomPropertiesModel.Charge;
                atom.ShowSymbol = AtomPropertiesModel.ShowSymbol;
                if (string.IsNullOrEmpty(AtomPropertiesModel.Isotope))
                {
                    atom.IsotopeNumber = null;
                }
                else
                {
                    atom.IsotopeNumber = int.Parse(AtomPropertiesModel.Isotope);
                }
            }

            if (AtomPropertiesModel.IsFunctionalGroup)
            {
                atom.Element = AtomPropertiesModel.Element;
                atom.FormalCharge = null;
                atom.ShowSymbol = null;
                atom.IsotopeNumber = null;
            }

            Preview.Chemistry = AtomPropertiesModel.MicroModel.Copy();
        }
    }
}