// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Resources;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AtomPropertyEditor.xaml
    /// </summary>
    public partial class AtomPropertyEditor : Window, INotifyPropertyChanged
    {
        private AtomPropertiesModel _apeModel;

        public AtomPropertiesModel ApeModel
        {
            get { return _apeModel; }
            set
            {
                _apeModel = value;
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
                ApeModel = model;
                DataContext = ApeModel;
                AtomPath.Text = ApeModel.Path;
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
            if (ValidateModel())
            {
                ApeModel.Save = true;
                Close();
            }
        }

        private void Dialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = ApeModel.Centre.X - ActualWidth / 2;
            Top = ApeModel.Centre.Y - ActualHeight / 2;

            LoadAtomItems();
            LoadFunctionalGroups();
            ShowPreview();
        }

        private void AtomTable_OnElementSelected(object sender, VisualPeriodicTable.ElementEventArgs e)
        {
            AtomOption newOption = null;
            var selElement = e.SelectedElement as Element;
            ApeModel.Element = selElement;
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
                ApeModel.AddedElement = selElement;
            }

            var atomPickerSelectedItem = newOption;
            AtomPicker.SelectedItem = atomPickerSelectedItem;
            ShowPreview();
        }

        private void AtomPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = AtomPicker.SelectedItem as AtomOption;
            ApeModel.AddedElement = option?.Element;
            ShowPreview();
        }

        private void FunctionalGroupPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtomOption option = FunctionalGroupPicker.SelectedItem as AtomOption;
            ApeModel.AddedElement = option?.Element;
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

            if (ApeModel.Element is Element el)
            {
                if (!Constants.StandardAtoms.Contains(el.Symbol))
                {
                    AtomPicker.Items.Add(new AtomOption(Globals.PeriodicTable.Elements[el.Symbol]));
                }

                AtomPicker.SelectedItem = new AtomOption(ApeModel.Element as Element);
            }
        }

        private void LoadFunctionalGroups()
        {
            FunctionalGroupPicker.Items.Clear();
            foreach (var item in Globals.FunctionalGroupsDictionary)
            {
                FunctionalGroupPicker.Items.Add(new AtomOption(item.Value));
            }

            if (ApeModel.IsFunctionalGroup)
            {
                FunctionalGroupPicker.SelectedItem = new AtomOption(ApeModel.Element as FunctionalGroup);
            }
        }

        private bool ValidateModel()
        {
            // There are no properties from user typed entries, so all are good
            return true;
        }

        private void ShowPreview()
        {
            var atoms = ApeModel.MicroModel.GetAllAtoms();
            var atom = atoms[0];

            if (ApeModel.IsElement)
            {
                atom.Element = ApeModel.Element;
                atom.FormalCharge = ApeModel.Charge;
                atom.ShowSymbol = ApeModel.ShowSymbol;
                if (string.IsNullOrEmpty(ApeModel.Isotope))
                {
                    atom.IsotopeNumber = null;
                }
                else
                {
                    atom.IsotopeNumber = int.Parse(ApeModel.Isotope);
                }
            }

            if (ApeModel.IsFunctionalGroup)
            {
                atom.Element = ApeModel.Element;
                atom.FormalCharge = null;
                atom.ShowSymbol = null;
                atom.IsotopeNumber = null;
            }

            Preview.Chemistry = ApeModel.MicroModel.Copy();
        }
    }
}