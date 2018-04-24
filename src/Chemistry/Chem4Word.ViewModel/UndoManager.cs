// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chem4Word.ViewModel
{
    /// <summary>
    /// Manages undo/redo for a view model
    /// All actions to be recorded should be
    /// packaged as a pair of actions, for both undo and redo
    /// Every set of actions MUST be nested between
    /// BeginTrans() and CommitTrans() calls
    /// 
    /// </summary>
    public class UndoManager
    {

        private struct UndoRecord
        {
            public int Level;
            public string Description;
            public Action UndoAction;
            public Action RedoAction;
            public object[] Params;

            public void Undo()
            { }

            public void Redo()
            { }

            public bool IsBufferRecord()
            {
                return Level != 0;
            }
        }

        //each block of transactions is bracketed by a buffer record at either end
        private readonly UndoRecord _bufferRecord;

        private EditViewModel _editViewModel;

        private Stack<UndoRecord> _undoStack;
        private Stack<UndoRecord> _redoStack;

        private int _transactionLevel = 0;

        public int TransactionLevel => _transactionLevel;

        public UndoManager(EditViewModel vm)
        {
            _editViewModel = vm;

            //set up the buffer record
            _bufferRecord = new UndoRecord
            {
                Description = "#buffer#",
                Level = 0,
                UndoAction = null,
                RedoAction = null,
                Params = null
            };

            Initialize();
        }
            
  
        public bool CanRedo => _redoStack.Any(rr => rr.Level!=0);

        public bool CanUndo => _undoStack.Any(ur => ur.Level != 0);

        public void Initialize()
        {
            _undoStack = new Stack<UndoRecord>();
            _redoStack = new Stack<UndoRecord>();
        }

        public void BeginTrans()
        {
            //push a buffer record onto the stack
            if (_transactionLevel == 0)
            {
                _undoStack.Push(_bufferRecord);
            }
            _transactionLevel++;

        }

        public void RecordAction(string desc, Action undoAction, Action redoAction, params object[] parameters)
        {
            _undoStack.Push(new UndoRecord {Level = _transactionLevel, Description = desc, UndoAction = undoAction, RedoAction = redoAction});
        }


        /// <summary>
        /// Ends a transaction block.  Transactions may be nested
        /// </summary>
        public void CommitTrans()
        {
            _transactionLevel--;
            
            if ( _transactionLevel < 0)
            {
                throw new IndexOutOfRangeException("Attempted to unwind empty undo stack.");
            }

            //we've concluded a transaction block so terminated it
            if (_transactionLevel == 0)
            {
                _undoStack.Push(_bufferRecord);
            }

            _editViewModel.UndoCommand.RaiseCanExecChanged();
            _editViewModel.RedoCommand.RaiseCanExecChanged();
        }

        public void Undo()
        {
            UndoActions();
            _editViewModel.UndoCommand.RaiseCanExecChanged();
            _editViewModel.RedoCommand.RaiseCanExecChanged();
        }

        private void UndoActions()
        {
            //the very first record on the undo stack should be a buffer record
            var br = _undoStack.Pop();
            if (!br.IsBufferRecord())
            {
                throw new InvalidDataException("Undo stack is missing buffer record");
            }
            _redoStack.Push(br);

            do
            {
                br = _undoStack.Pop();
                br.Undo();
                _redoStack.Push(br);
            } while (!br.IsBufferRecord());

        }

        public void Redo()
        {
            RedoActions();
            _editViewModel.UndoCommand.RaiseCanExecChanged();
            _editViewModel.RedoCommand.RaiseCanExecChanged();
        }

        private void RedoActions()
        {
            //the very first record on the redo stack should be a buffer record
            var br = _redoStack.Pop();
            if (!br.IsBufferRecord())
            {
                throw new InvalidDataException("Redo stack is missing buffer record");
            }
            _undoStack.Push(br);

            do
            {
                br = _redoStack.Pop();
                br.Redo();
                _undoStack.Push(br);
            } while (!br.IsBufferRecord());
        }
    }
}