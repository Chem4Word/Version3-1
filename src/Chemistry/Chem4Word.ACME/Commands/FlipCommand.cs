// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;

using Chem4Word.Model2;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Commands
{
    public class FlipCommand : ACME.Commands.BaseCommand
    {
        public FlipCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return MyEditViewModel.SingleMolSelected;
        }

        public override void Execute(object parameter)
        {
            Debug.Assert(MyEditViewModel.SelectedItems[0] is Molecule);
            var selMolecule = MyEditViewModel.SelectedItems[0] as Molecule;

            MyEditViewModel.FlipMolecule(selMolecule, false, false);
        }
    }
}