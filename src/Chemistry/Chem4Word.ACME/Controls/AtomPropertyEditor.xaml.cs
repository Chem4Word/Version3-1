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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Resources;
using Chem4Word.ACME.Utils;
using Chem4Word.Core;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AtomPropertyEditor.xaml
    /// </summary>
    public partial class AtomPropertyEditor : Window, INotifyPropertyChanged
    {
        private bool _closedByUser;
        private bool IsDirty { get; set; }

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

        private void AtomPropertyEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            // This moves the window off screen while it renders
            var point = UIUtils.GetOffScreenPoint();
            Left = point.X;
            Top = point.Y;

            LoadAtomItems();
            LoadFunctionalGroups();
            ShowPreview();
        }

        private void AtomPropertyEditor_OnContentRendered(object sender, EventArgs e)
        {
            // This moves the window to the correct position
            var point = UIUtils.GetOnScreenPoint(_atomPropertiesModel.Centre, ActualWidth, ActualHeight);
            Left = point.X;
            Top = point.Y;

            InvalidateArrange();
            IsDirty = false;
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
            _closedByUser = true;
            Close();
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
            IsDirty = true;
        }

        private void AtomPropertyEditor_OnClosing(object sender, CancelEventArgs e)
        {
            if (!_closedByUser && IsDirty)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Do you wish to save your changes?");
                sb.AppendLine("  Click 'Yes' to save your changes and exit.");
                sb.AppendLine("  Click 'No' to discard your changes and exit.");
                sb.AppendLine("  Click 'Cancel' to return to the form.");

                DialogResult dr = UserInteractions.AskUserYesNoCancel(sb.ToString());
                switch (dr)
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        _atomPropertiesModel.Save = true;
                        break;

                    case System.Windows.Forms.DialogResult.No:
                        _atomPropertiesModel.Save = false;
                        break;

                    case System.Windows.Forms.DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}