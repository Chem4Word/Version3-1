using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chem4Word.ViewModel.Commands
{
    public class CutCommand : BaseCommand
    {

        public CutCommand(EditViewModel vm) : base(vm)
        {

        }

        public override bool CanExecute(object parameter)
        {
            return MyEditViewModel.SelectionType != EditViewModel.SelectionTypeCode.None;
        }

        public override void Execute(object parameter)
        {
            MyEditViewModel.CopySelection();
        }

        public override void RaiseCanExecChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged.Invoke(this, new EventArgs());
        }


        public override event EventHandler CanExecuteChanged;
    }
}
