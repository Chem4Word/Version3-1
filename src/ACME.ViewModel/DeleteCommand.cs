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
            
            DoCommand(parameter);
            MyViewModel.UndoManager.Commit();
            
        }

        public override event EventHandler CanExecuteChanged;

        /// <summary>
        /// This does the work of the command distinct from the interface implementation
        /// </summary>
        /// <param name="parameter"></param>
        public override void DoCommand(object parameter)
        {
            
        }

        public DeleteCommand(ViewModel vm) : base(vm)
        {
        }


        #endregion

        #region Constructors

        #endregion
    }
}
