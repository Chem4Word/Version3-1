﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;


namespace Chem4Word.ACME.Commands
{
    public class FlipHorizontalCommand : ACME.Commands.FlipCommand
    {
        public FlipHorizontalCommand(EditViewModel vm) : base(vm)
        {
        }

        public override void Execute(object parameter)
        {
            var selMolecule = EditViewModel.SelectedItems[0] as Molecule;
            EditViewModel.FlipMolecule(selMolecule, false, false);
        }
    }
}