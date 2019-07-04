// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System;
using System.Linq;

namespace Chem4Word.ACME.Commands
{
    public class DeleteCommand : ACME.Commands.BaseCommand
    {
        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            return EditViewModel.SelectedItems.Any();
        }

        public override void Execute(object parameter)
        {
            EditViewModel.DeleteSelection();
        }

        public override event EventHandler CanExecuteChanged;

        public DeleteCommand(EditViewModel vm) : base(vm)
        { }

        #endregion ICommand Implementation

        public override void RaiseCanExecChanged()
        {
            var args = new EventArgs();

            CanExecuteChanged?.Invoke(this, args);
        }
    }
}