using System;

namespace ACME.ViewModel.Commands
{
    public class UndoCommand :BaseCommand
    {
        public UndoCommand(ViewModel vm) : base(vm)
        {
            
        }

        public override bool CanExecute(object parameter)
        {
            return MyViewModel.UndoManager.CanUndo;
        }

        public override void Execute(object parameter)
        {
            
        }

        public override event EventHandler CanExecuteChanged;
       
    }
}
