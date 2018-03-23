using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;

namespace ACME.ViewModel
{
    public class UndoManager
    {
        private ViewModel _viewModel;

        public UndoManager(ViewModel vm)
        {
            _viewModel = vm;
        }

        public void Initialize(Chem4Word.Model.Model model)
        {
            
        }
        public void Commit()
        {
        }

        public void Undo()
        {
            
        }

        public void Redo()
        {
            
        }

    }
}
