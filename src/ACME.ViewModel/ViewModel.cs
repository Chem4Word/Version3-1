using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Chem4Word.Model;

namespace ACME.ViewModel
{
    public class ViewModel
    {
        #region Properties
        public CompositeCollection AllObjects { get; set; }
        public ObservableCollection<object> SelectedItems { get;}

        public Model Model { get; set; }
        #endregion
        #region Commands
        public DeleteCommand DeleteCommand { get; }

        #region constructors
        public ViewModel()
        {
            SelectedItems = new ObservableCollection<object>();

            DeleteCommand = new DeleteCommand(this);
        }
        #endregion

        
    }

    
}
