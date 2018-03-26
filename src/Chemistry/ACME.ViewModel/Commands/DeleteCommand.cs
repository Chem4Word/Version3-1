using System;
using System.Linq;

namespace ACME.ViewModel.Commands
{
    public class DeleteCommand : BaseCommand
    {
        
        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            return MyViewModel.SelectedItems.Any();
        }

        public override void Execute(object parameter)
        {
            
            MyViewModel.UndoManager.Commit();
            
        }

        public override event EventHandler CanExecuteChanged;

      

        public DeleteCommand(ViewModel vm) : base(vm)
        {
        }


        #endregion

        #region Constructors

        #endregion
    }
}
