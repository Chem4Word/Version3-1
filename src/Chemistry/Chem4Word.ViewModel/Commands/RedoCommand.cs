﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.ViewModel.Commands
{
    public class RedoCommand : BaseCommand
    {
        private ViewModel.EditViewModel _currentVM;

        public RedoCommand(EditViewModel vm) : base(vm)
        {
            _currentVM = vm;
        }

        public override bool CanExecute(object parameter)
        {
            return _currentVM.UndoManager.CanRedo;
        }

        public override void Execute(object parameter)
        {
            _currentVM.UndoManager.Redo();
        }

        public void RaiseCanExecChanged()
        {
            var args = new EventArgs();

            CanExecuteChanged.Invoke(this, args);
        }

        public override event EventHandler CanExecuteChanged;
    }
}