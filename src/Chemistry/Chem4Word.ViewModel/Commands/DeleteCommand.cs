// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using Chem4Word.Model;

namespace Chem4Word.ViewModel.Commands
{
    public class DeleteCommand : BaseCommand
    {
        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            return MyEditViewModel.SelectedItems.Any();
        }

        public override void Execute(object parameter)
        {
            MyEditViewModel.UndoManager.BeginUndoBlock();
            //first do the astom and associated bonds
            if (((MyEditViewModel.SelectionType & EditViewModel.SelectionTypeCode.Atom) == EditViewModel.SelectionTypeCode.Atom))
            {

                var atoms = MyEditViewModel.SelectedItems.OfType<Atom>().ToList();
                foreach (Atom atom in atoms )
                {
                    MyEditViewModel.DeleteAtom(atom);
                }
               
            }

            //now do any bonds remaing:  this is important if only bonds have been selected


            if (((MyEditViewModel.SelectionType & EditViewModel.SelectionTypeCode.Bond) ==
                 EditViewModel.SelectionTypeCode.Bond))
            {
                var bonds = MyEditViewModel.SelectedItems.OfType<Bond>().ToList();

                foreach (Bond bond in bonds)
                {

                    MyEditViewModel.DeleteBond(bond);
                }
            }

            MyEditViewModel.UndoManager.EndUndoBlock();
        }

        public override event EventHandler CanExecuteChanged;

        public DeleteCommand(EditViewModel vm) : base(vm)
        {

        }


        #endregion ICommand Implementation

        public override void RaiseCanExecChanged()
        {
            var args = new EventArgs();

            CanExecuteChanged?.Invoke(this, args);
        }
    }
}