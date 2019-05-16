// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Annotations;
using Chem4Word.ACME.Models;
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
        private bool _closing;
        private EditViewModel _editViewModel;

        public AtomPropertiesModel Model
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        public EditViewModel EditViewModel
        {
            get
            {
                return _editViewModel;
            }
            set
            {
                _editViewModel = value;
                OnPropertyChanged();
            }
        }

        public AtomPropertyEditor()
        {
            InitializeComponent();
        }

        public AtomPropertyEditor(AtomPropertiesModel model, EditViewModel evm) : this()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                EditViewModel = evm;
                Model = model;
                DataContext = Model;
                AtomPath.Text = Model.Path;

                Closing += OnClosing;
                ContentRendered += AtomPropertyEditor_ContentRendered;
#if DEBUG
#else
                Deactivated += OnDeactivated;
#endif
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

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _closing = true;
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            if (!_closing)
            {
                Model.Save = false;
                Close();
            }
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (ValidateModel())
            {
                Model.Save = true;
                _closing = true;
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

            return b1  && b3;
        }

        private void Dialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = Model.Centre.X - ActualWidth / 2;
            Top = Model.Centre.Y - ActualHeight / 2;
        }

        private void AtomTable_OnElementSelected(object sender, VisualPeriodicTable.ElementEventArgs e)
        {
            var addition = e.SelectedElement as Element;
            Model.Element = addition;
            PeriodicTableExpander.IsExpanded = false;
            EditViewModel.LoadAtomOptions(addition);
            var atomPickerSelectedItem = EditViewModel.AtomOptions.FirstOrDefault(ao => ao.Element.Equals(addition));
            AtomPicker.SelectedItem = atomPickerSelectedItem;
        }

    }
}