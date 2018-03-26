using System;

namespace ACME.ViewModel.Commands
{
    public class AddAtomCommand : BaseCommand
    {

        #region ICommand Implementation

        public override bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public override void Execute(object parameter)
        {
            throw new NotImplementedException();
        }

       

        public override event EventHandler CanExecuteChanged;

        #endregion
        #region Constructors

        
        public AddAtomCommand(ViewModel vm) :base(vm)
        {
           
        }
        #endregion
    }
}
