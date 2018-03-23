using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ACME.ViewModel
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
            throw new NotImplementedException();
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
