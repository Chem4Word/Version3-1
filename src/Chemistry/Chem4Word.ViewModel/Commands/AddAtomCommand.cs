// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.ViewModel.Commands
{
    public class AddAtomCommand : BaseCommand
    {
        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public override void Execute(object parameter)
        {
            throw new NotImplementedException();
        }

        public override event EventHandler CanExecuteChanged;

        #endregion ICommand Implementation

        #region Constructors

        public AddAtomCommand(EditViewModel vm) : base(vm)
        {
        }

        #endregion Constructors
    }
}