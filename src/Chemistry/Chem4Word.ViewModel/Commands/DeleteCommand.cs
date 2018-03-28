// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;

namespace Chem4Word.ViewModel.Commands
{
    public class DeleteCommand : BaseCommand
    {
        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            return MyEditViewModel.SelectedItems.Any();
        }

        public override void Execute(object parameter)
        {
            MyEditViewModel.UndoManager.Commit();
        }

        public override event EventHandler CanExecuteChanged;

        public DeleteCommand(EditViewModel vm) : base(vm)
        {
        }

        #endregion ICommand Implementation
    }
}