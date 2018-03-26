using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using Chem4Word.Model;

namespace ACME.ViewModel
{
    public class UndoManager
    {
        private ViewModel _viewModel;

        private Stack<Model> _undoStack;
        private Stack<Model> _redoStack;

        public UndoManager(ViewModel vm)
        {
            _viewModel = vm;
        }

        public void Initialize(Chem4Word.Model.Model model)
        {
            _undoStack = new Stack<Model>();
            _redoStack = new Stack<Model>();
        }
        public void Commit()
        {
            _undoStack.Push(_viewModel.Model.Clone());
        }

        public void Undo()
        {
            _viewModel.Model = _undoStack.Pop();
            _redoStack.Push(_viewModel.Model.Clone());
        }

        public void Redo()
        {
            
        }

    }
}
