// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Chem4Word.ViewModel.Commands;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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
        #endregion Properties

        #region Commands

        public DeleteCommand DeleteCommand { get; }

        public AddAtomCommand AddAtomCommand { get; }

        #endregion Commands

        #region constructors

        public EditViewModel(Model.Model model):base(model)
        {
            SelectedItems = new ObservableCollection<object>();
            SelectedItems.CollectionChanged += SelectedItemsOnCollectionChanged;

            UndoManager = new UndoManager(this);

            DeleteCommand = new DeleteCommand(this);
            AddAtomCommand = new AddAtomCommand(this);

        }

        #endregion constructors
        private void SelectedItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //switch (e.Action)
            //{
            //    case NotifyCollectionChangedAction.Add:
            //    case NotifyCollectionChangedAction.Move:
            //    case NotifyCollectionChangedAction.Remove:
            //    case NotifyCollectionChangedAction.Replace:
            //        break;
            //}
        }

    


    }
}