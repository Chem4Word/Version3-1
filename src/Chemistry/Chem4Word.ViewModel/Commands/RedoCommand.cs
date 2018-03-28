// ---------------------------------------------------------------------------
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
        public RedoCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public override void Execute(object parameter)
        {
            throw new NotImplementedException();
        }

        public override event EventHandler CanExecuteChanged;
    }
}