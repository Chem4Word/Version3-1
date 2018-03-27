// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ViewModel.Commands;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;

namespace Chem4Word.ViewModel
{
    public class ViewModel
    {
        public enum SelectionType
        {
            None = 0,
            Atom = 1,
            Bond = 2,
            Molecule = 4
        }

        #region Properties

        public CompositeCollection AllObjects { get; set; }
        public ObservableCollection<object> SelectedItems { get; }

        public Model.Model Model { get; set; }

        public Rect BoundingBox
        {
            get;
        }

        public static double FontSize { get; set; }
        public UndoManager UndoManager { get; }

        #endregion Properties

        #region Commands

        public DeleteCommand DeleteCommand { get; }

        public AddAtomCommand AddAtomCommand { get; }

        #endregion Commands

        #region constructors

        public ViewModel()
        {
            SelectedItems = new ObservableCollection<object>();

            UndoManager = new UndoManager(this);

            DeleteCommand = new DeleteCommand(this);
            AddAtomCommand = new AddAtomCommand(this);

            BoundingBox = new Rect();
        }

        #endregion constructors

    }
}