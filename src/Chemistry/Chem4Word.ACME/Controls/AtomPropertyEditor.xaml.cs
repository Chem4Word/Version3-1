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
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for AtomPropertyEditor.xaml
    /// </summary>
    public partial class AtomPropertyEditor : Window, INotifyPropertyChanged
    {
        private AtomPropertiesModel _model;

        public AtomPropertiesModel Model
        {
            get { return _model; }
            set
            {
                _model = value;
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
                Model = model;
                DataContext = Model;
                AtomPath.Text = Model.Path;
                LoadAtomItems();
                ContentRendered += AtomPropertyEditor_ContentRendered;
            }
        }

        private void LoadAtomItems()
        {
            foreach (var item in Constants.StandardAtoms)
            {
                AtomPicker.Items.Add(new AtomOption(Globals.PeriodicTable.Elements[item]));
            }

            foreach (var item in Constants.StandardFunctionalGroups)
            {
                AtomPicker.Items.Add(new AtomOption(Globals.FunctionalGroupsDictionary[item]));
            }

            if (Model.Element is Element el)
            {
                if (!Constants.StandardAtoms.Contains(el.Symbol))
                {
                    AtomPicker.Items.Add(new AtomOption(Globals.PeriodicTable.Elements[el.Symbol]));
                }
            }

            if (Model.Element is FunctionalGroup fg)
            {
                if (!Constants.StandardFunctionalGroups.Contains(fg.Symbol))
                {
                    AtomPicker.Items.Add(new AtomOption(Globals.FunctionalGroupsDictionary[fg.Symbol]));
                }
            }
        }

        private void AtomPropertyEditor_ContentRendered(object sender, EventArgs e)
        {
            Activate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (ValidateModel())
            {
                Model.Save = true;
                Close();
            }
        }

        private bool ValidateModel()
        {
            ElementBase eb;
            bool b1 = AtomHelpers.TryParse(Model.Element.Symbol, out eb);

            int n;

            bool b3 = string.IsNullOrEmpty(Model.Isotope);
            if (!b3)
            {
                b3 = int.TryParse(Model.Isotope, out n);
            }

            return b1 && b3;
        }

        private void Dialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = Model.Centre.X - ActualWidth / 2;
            Top = Model.Centre.Y - ActualHeight / 2;
        }

        private void AtomTable_OnElementSelected(object sender, VisualPeriodicTable.ElementEventArgs e)
        {
            AtomOption newOption = null;
            var selElement = e.SelectedElement as Element;
            Model.Element = selElement;
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
                        break;
                    }
                }

                if (option.Element is FunctionalGroup fg)
                {
                    // Ignore any Functional Groups in the picker
                }
            }
            if (!found)
            {
                newOption = new AtomOption(selElement);
                AtomPicker.Items.Add(newOption);
                Model.AddedElement = selElement;
            }
            var atomPickerSelectedItem = newOption;
            AtomPicker.SelectedItem = atomPickerSelectedItem;
        }
    }
}