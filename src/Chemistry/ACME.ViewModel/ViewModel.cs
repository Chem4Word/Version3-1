using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ACME.ViewModel.Commands;
using Chem4Word.Model;

namespace ACME.ViewModel
{
    public class ViewModel
    {
        public enum SelectionType
        {
            None = 0,
            Atom = 1,
            Bond = 2,
            Molecule = 4
        }
        #region Properties
        public CompositeCollection AllObjects { get; set; }
        public ObservableCollection<object> SelectedItems { get;}

        public Model Model { get; set; }

        public Rect BoundingBox
        {
            get;
        }

        public double FontSize { get; set; }
        public UndoManager UndoManager { get; }
        #endregion
        #region Commands
        public DeleteCommand DeleteCommand { get; }

        public AddAtomCommand AddAtomCommand { get;  }
        #endregion
        #region constructors
        public ViewModel()
        {
            SelectedItems = new ObservableCollection<object>();

            UndoManager = new UndoManager(this);

            DeleteCommand = new DeleteCommand(this);
            AddAtomCommand = new AddAtomCommand(this);

            BoundingBox = new Rect();
        }
        #endregion   
    }    
}
