// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Utils;
using System.Windows;
using System.Windows.Input;

namespace Chem4Word.ACME.Behaviors
{
    public class DeleteBehavior : BaseEditBehavior
    {
        //private bool _lassoVisible;
        //private PointCollection _mouseTrack;
        //private Point _startpoint;
        private Window _parent;

        private Cursor _cursor;

        //private bool _flag;
        //private LassoAdorner _lassoAdorner;
        //private MoleculeSelectionAdorner _molAdorner;

        protected override void OnAttached()
        {
            base.OnAttached();

            _parent = Application.Current.MainWindow;
            CurrentEditor = (EditorCanvas)AssociatedObject;

            _cursor = CurrentEditor.Cursor;

            CurrentEditor.Cursor = CursorUtils.Eraser;
            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;

            CurrentEditor.IsHitTestVisible = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }
            //clear the current selection
            EditViewModel.SelectedItems.Clear();
            CurrentStatus = "Click to remove an atom or bond.";
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hitTestResult = CurrentEditor.ActiveVisual;
            if (hitTestResult is AtomVisual atomVisual)
            {
                var atom = atomVisual.ParentAtom;
                this.EditViewModel.DeleteAtoms(new[] { atom });
                CurrentStatus = "Atom deleted.";
            }
            else if (hitTestResult is BondVisual bondVisual)
            {
                var bond = bondVisual.ParentBond;
                this.EditViewModel.DeleteBonds(new[] { bond });
                CurrentStatus = "Bond deleted";
            }
            EditViewModel.SelectedItems.Clear();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (CurrentEditor != null)
            {
                CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
                CurrentEditor.IsHitTestVisible = false;
                CurrentEditor.Cursor = _cursor;
                CurrentStatus = "";
            }
        }
    }
}