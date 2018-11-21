// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;

namespace Chem4Word.ViewModel.Commands
{
    public class FlipVerticalCommand : FlipCommand
    {
        public FlipVerticalCommand(EditViewModel vm) : base(vm)
        {
        }

        public override void Execute(object parameter)
        {
            var selMolecule = MyEditViewModel.SelectedItems[0] as Molecule;
            MyEditViewModel.FlipMolecule(selMolecule, true, false);
        }
    }
}