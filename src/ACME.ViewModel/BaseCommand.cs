using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ACME.ViewModel
{
    public abstract class BaseCommand : ICommand
    {
        public ViewModel MyViewModel { get; set; }

        #region ICommand Implementation

        public abstract bool CanExecute(object parameter);


        public abstract void Execute(object parameter);

        public abstract void DoCommand(object parameter);
      

        public abstract event EventHandler CanExecuteChanged;

        #endregion
        #region Constructors

        protected BaseCommand(ViewModel vm)
        {
            MyViewModel = vm;
        }
        #endregion
    }
}
