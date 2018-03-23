using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ACME.ViewModel
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

        public override void DoCommand(object parameter)
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
