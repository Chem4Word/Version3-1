// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.ViewModel
{
    public class UndoManager
    {
        private EditViewModel _editViewModel;

        private Stack<Model.Model> _undoStack;
        private Stack<Model.Model> _redoStack;

        public UndoManager(EditViewModel vm)
        {
            _editViewModel = vm;
        }

        public bool CanRedo => _redoStack.Any();

        public bool CanUndo => _undoStack.Any();

        public void Initialize(Chem4Word.Model.Model model)
        {
            _undoStack = new Stack<Model.Model>();
            _redoStack = new Stack<Model.Model>();
        }

        public void Commit()
        {
            _undoStack.Push(_editViewModel.Model.Clone());
        }

        public void Undo()
        {
            _editViewModel.Model = _undoStack.Pop();
            _redoStack.Push(_editViewModel.Model.Clone());
        }

        public void Redo()
        {
            _editViewModel.Model = _redoStack.Pop();
            _undoStack.Push(_editViewModel.Model.Clone());
        }
    }
}