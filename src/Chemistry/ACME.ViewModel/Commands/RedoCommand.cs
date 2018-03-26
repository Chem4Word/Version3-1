using System;

namespace ACME.ViewModel.Commands
{
    public class RedoCommand : BaseCommand
    {
        public RedoCommand(ViewModel vm) : base(vm)
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
