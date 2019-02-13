// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Commands
{
    public class UnGroupCommand : ACME.Commands.BaseCommand
    {
        public UnGroupCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return false;
        }

        public override void Execute(object parameter)
        {
            Debugger.Break();
        }
    }
}