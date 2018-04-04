// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Chem4Word.ViewModel.Commands;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using Chem4Word.Model;

namespace Chem4Word.ViewModel 
{
    public class EditViewModel :DisplayViewModel
    {
        public enum SelectionTypeCode
        {
            None = 0,
            Atom = 1,
            Bond = 2,
            Molecule = 4
        }

        
        #region Properties

  
        public ObservableCollection<object> SelectedItems { get; }

        public UndoManager UndoManager { get; }

        private BondOption _selectedBondOption;
        public BondOption SelectedBondOption
        {
            get { return _selectedBondOption; }
            set { _selectedBondOption = value; }
        }

        private AtomOption _selectedAtomOption;
        public AtomOption SelectedAtomOption
        {
            get { return _selectedAtomOption; }

            set { _selectedAtomOption = value; } 
        }
        /// <summary>
        /// returns a distinct list of selected elements
        /// </summary>
        public List<ElementBase> SelectedElementType
        {
            get { return SelectedItems.OfType<ElementBase>().Distinct().ToList(); }
        }


        public List<Bond> SelectedBondType
        {
            get { return SelectedItems.OfType<Bond>().Distinct().ToList(); }
        }

        public Canvas DrawingSurface { get; set; }
        #endregion Properties




        private Behavior _activeMode;

        public Behavior ActiveMode
        {
            get { return _activeMode; }
            set
            {
                if (_activeMode != null)
                {
                    _activeMode.Detach();
                }
                _activeMode = value;
                _activeMode.Attach(DrawingSurface);
            }
        }





        #region Commands

        public DeleteCommand DeleteCommand { get; }

        public AddAtomCommand AddAtomCommand { get; }

        public  UndoCommand UndoCommand { get; }

        public RedoCommand RedoCommand { get; }



        #endregion Commands

        #region constructors

        public EditViewModel(Model.Model model):base(model)
        {
            RedoCommand = new RedoCommand(this);
            UndoCommand = new UndoCommand(this);

            SelectedItems = new ObservableCollection<object>();
            SelectedItems.CollectionChanged += SelectedItemsOnCollectionChanged;

            UndoManager = new UndoManager(this);

            DeleteCommand = new DeleteCommand(this);
            AddAtomCommand = new AddAtomCommand(this);

        }

        #endregion constructors
        private void SelectedItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var newObjects = e.NewItems;
            var oldObject = e.OldItems;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddSelectionAdorners(newObjects);
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Remove:
                    RemoveSelectionAdorners(oldObject);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
            }
            OnPropertyChanged(nameof(SelectedAtomOption));
            OnPropertyChanged(nameof(SelectedBondOption));
        }

        private void RemoveSelectionAdorners(IList oldObject)
        {
            
        }

        private void AddSelectionAdorners(IList newObjects)
        {
            
        }
    }
}