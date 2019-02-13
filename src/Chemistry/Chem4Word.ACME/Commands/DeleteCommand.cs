// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using Chem4Word.Model2;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Commands
{
    public class DeleteCommand : ACME.Commands.BaseCommand
    {
        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            return MyEditViewModel.SelectedItems.Any();
        }

        public override void Execute(object parameter)
        {
            var atoms = MyEditViewModel.SelectedItems.OfType<Atom>().ToList();
            var bonds = MyEditViewModel.SelectedItems.OfType<Bond>().ToList();
            var mols = MyEditViewModel.SelectedItems.OfType<Molecule>().ToList();
            if (atoms.Any() | bonds.Any())
            {
                MyEditViewModel.UndoManager.BeginUndoBlock();
                //do any bonds remaining:  this is important if only bonds have been selected

                if (mols.Any())
                {
                    foreach (Molecule mol in mols)
                    {
                        MyEditViewModel.DeleteMolecule(mol);
                    }
                }
                else
                {
                    if (bonds.Any())
                    {
                        foreach (Bond bond in bonds)
                        {
                            MyEditViewModel.DeleteBond(bond);
                        }
                    }

                    //do the atom and any remaining associated bonds
                    if (atoms.Any())
                    {
                        foreach (Atom atom in atoms)
                        {
                            MyEditViewModel.DeleteAtom(atom);
                        }
                    }
                }
                MyEditViewModel.UndoManager.EndUndoBlock();
                MyEditViewModel.SelectedItems.Clear();
            }
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