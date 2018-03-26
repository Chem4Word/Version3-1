using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACME.ViewModel
{
    public class RedoCommand :BaseCommand
    {
        public RedoCommand(ViewModel vm) : base(vm)
        {
     
        }

        public override bool CanExecute(object parameter)
        {
            return MyViewModel.UndoManager.CanRedo;
        }

        public override void Execute(object parameter)
        {
            DoCommand(parameter);
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public override event EventHandler CanExecuteChanged;
        public override void DoCommand(object parameter)
        {
            MyViewModel.UndoManager.Undo();
        }
    }
}
