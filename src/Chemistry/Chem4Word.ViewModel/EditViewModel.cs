// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using Chem4Word.ViewModel.Commands;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;

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
            
        }


     

    }
}