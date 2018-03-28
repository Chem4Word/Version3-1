// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows.Input;

namespace Chem4Word.ViewModel.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public EditViewModel MyEditViewModel { get; set; }

        #region ICommand Implementation

        public abstract bool CanExecute(object parameter);

        public abstract void Execute(object parameter);

        public abstract event EventHandler CanExecuteChanged;

        #endregion ICommand Implementation

        #region Constructors

        protected BaseCommand(EditViewModel vm)
        {
            MyEditViewModel = vm;
        }

        #endregion Constructors
    }
}