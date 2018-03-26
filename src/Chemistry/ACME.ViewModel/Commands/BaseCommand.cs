using System;
using System.Windows.Input;

namespace ACME.ViewModel.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public ViewModel MyViewModel { get; set; }

        #region ICommand Implementation

        public abstract bool CanExecute(object parameter);


        public abstract void Execute(object parameter);

      

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
