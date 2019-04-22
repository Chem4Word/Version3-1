// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Behaviors
{
    public class LassoBehavior : BaseEditBehavior
    {
        //private bool _lassoVisible;
        private PointCollection _mouseTrack;

        private Point _startpoint;
        private Window _parent;
        private bool _flag;
        private LassoAdorner _lassoAdorner;
        //private MoleculeSelectionAdorner _molAdorner;
        private const string DefaultText = "Click to select; [Shift]-click to multselect; drag to select range; double-click to select molecule.";
        private const string ActiveSelText = "Set atoms/bonds using selectors; [Delete] to remove.";

        protected override void OnAttached()
        {
            base.OnAttached();

            _parent = Application.Current.MainWindow;

            CurrentEditor = (EditorCanvas)AssociatedObject;

            CurrentEditor.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
            CurrentEditor.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
            CurrentEditor.MouseRightButtonDown += AssociatedObjectOnMouseRightButtonDown;
            CurrentEditor.MouseRightButtonUp += AssociatedObjectOnMouseRightButtonUp;

            CurrentEditor.IsHitTestVisible = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            }

            
            CurrentStatus = DefaultText;
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                EditViewModel.SelectedItems.Clear();
            }

            _mouseTrack = new PointCollection();
            _startpoint = Mouse.GetPosition(CurrentEditor);
            _flag = true;

            Mouse.Capture(CurrentEditor);
            _mouseTrack.Add(_startpoint);

            if (e.ClickCount == 2 & EditViewModel.SelectionType == EditViewModel.SelectionTypeCode.Molecule)
            {
                DoMolSelect(e);
                e.Handled = true;
            }
            if (e.ClickCount == 2)
            {
                DoMolSelect(e);
                e.Handled = true;
            }

            if (e.ClickCount == 1) //single click
            {
                DoSingleSelect(e);
                //e.Handled = true;
            }
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.ReleaseMouseCapture();
            _flag = false;

            if (_lassoAdorner != null)
            {
                DisposeLasso();
            }

            if (EditViewModel.SelectedItems.Any())
            {
                CurrentStatus = ActiveSelText;
            }
        }

        private void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Dragging(e))
            {
                var pos = Mouse.GetPosition(CurrentEditor);
                _mouseTrack.Add(pos);
                var outline = GetPolyGeometry();

                if (_lassoAdorner == null)
                {
                    _lassoAdorner = new LassoAdorner(CurrentEditor, outline);
                }

                _lassoAdorner.Outline = outline;

                ModifySelection(outline);
            }
        }

        private void AssociatedObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                //MessageBox.Show("Trapped the double click");
            }
        }

        private void AssociatedObjectOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("AssociatedObjectOnMouseRightButtonUp");
            CurrentEditor.ReleaseMouseCapture();
        }

        private void AssociatedObjectOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("AssociatedObjectOnMouseRightButtonDown");
            Mouse.Capture(CurrentEditor);
            if (e.ClickCount == 1)
            {
                var hitTestResult = GetTarget(e);
                if (hitTestResult.VisualHit is AtomVisual)
                {
                    var atom = (AtomVisual)hitTestResult.VisualHit;
                    Debug.WriteLine($"Right Click Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");
                }
                else if (hitTestResult.VisualHit is BondVisual)
                {
                    var bond = (BondVisual)hitTestResult.VisualHit;
                    var pos = e.GetPosition(CurrentEditor);
                    Debug.WriteLine($"Right Click Bond {bond.ParentBond.Id} at ({pos.X},{pos.Y})");
                }
            }
        }

        private void DisposeLasso()
        {
            RemoveAdorner(_lassoAdorner);
            _lassoAdorner = null;
        }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & _flag;
        }

        private void ModifySelection(StreamGeometry outline)
        {
            VisualTreeHelper.HitTest(CurrentEditor, null, HitTestCallback, new GeometryHitTestParameters(outline));
        }

        private void RemoveAdorner(Adorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(adorner);
        }

        private StreamGeometry GetPolyGeometry()
        {
            if (_mouseTrack != null)
            {
                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext context = geo.Open())
                {
                    context.BeginFigure(_mouseTrack[0], true, true);

                    // Add the points after the first one.
                    context.PolyLineTo(_mouseTrack.Skip(1).ToArray(), true, false);
                }
                return geo;
            }
            else
            {
                return null;
            }
        }

        private void DoMolSelect(MouseButtonEventArgs e)
        {
            var activeVisual = CurrentEditor.ActiveVisual;

            switch (activeVisual)
            {
                case AtomVisual av:
                    {
                        var atom = av.ParentAtom;
                        //MessageBox.Show($"Hit Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");

                        EditViewModel.AddToSelection(atom);
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                case BondVisual bv:
                    {
                        var bond = bv.ParentBond;
                        //MessageBox.Show($"Hit Bond {bond.ParentBond.Id} at ({e.GetPosition(AssociatedObject).X},{e.GetPosition(AssociatedObject).Y})");

                        EditViewModel.AddToSelection(bond);
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                default:
                    EditViewModel.SelectedItems.Clear();
                    CurrentStatus = DefaultText;
                    break;
            }
        }

        private void DoSingleSelect(MouseButtonEventArgs e)
        {
            var activeVisual = CurrentEditor.ActiveVisual;

            switch (activeVisual)
            {
                case AtomVisual av:
                    {
                        var atom = av.ParentAtom;
                        //MessageBox.Show($"Hit Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");

                        EditViewModel.AddToSelection(atom);
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                case BondVisual bv:
                    {
                        var bond = bv.ParentBond;
                        //MessageBox.Show($"Hit Bond {bond.ParentBond.Id} at ({e.GetPosition(CurrentEditor).X},{e.GetPosition(AssociatedObject).Y})");

                        EditViewModel.AddToSelection(bond);
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                default:
                    EditViewModel.SelectedItems.Clear();
                    CurrentStatus = DefaultText;
                    break;
            }
        }

        private HitTestResult GetTarget(MouseButtonEventArgs e)
        {
            return VisualTreeHelper.HitTest(CurrentEditor, e.GetPosition(CurrentEditor));
        }

        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            var id = ((GeometryHitTestResult)result).IntersectionDetail;

            var myShape = (result.VisualHit);
            if (myShape != null && (myShape is AtomVisual | myShape is BondVisual))
            {
                switch (id)
                {
                    case IntersectionDetail.FullyContains:
                    case IntersectionDetail.Intersects:
                    case IntersectionDetail.FullyInside:
                        var selAtom = ((myShape as AtomVisual)?.ParentAtom);
                        var selBond = ((myShape as BondVisual)?.ParentBond);

                        if (!(EditViewModel.SelectedItems.Contains(selAtom) || EditViewModel.SelectedItems.Contains(selBond)))
                        {
                            if (selAtom != null)
                            {
                                EditViewModel.AddToSelection(selAtom);
                            }
                            if (selBond != null)
                            {
                                EditViewModel.AddToSelection(selBond);
                            }
                        }
                        return HitTestResultBehavior.Continue;

                    case IntersectionDetail.Empty:
                        selAtom = ((myShape as AtomVisual)?.ParentAtom);
                        selBond = ((myShape as BondVisual)?.ParentBond);

                        if ((EditViewModel.SelectedItems.Contains(selAtom) || EditViewModel.SelectedItems.Contains(selBond)))
                        {
                            if (selAtom != null)
                            {
                                EditViewModel.RemoveFromSelection(selAtom);
                            }
                            if (selBond != null)
                            {
                                EditViewModel.RemoveFromSelection(selBond);
                            }
                        }
                        return HitTestResultBehavior.Continue;

                    default:
                        return HitTestResultBehavior.Stop;
                }
            }
            return HitTestResultBehavior.Continue;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            _parent = Application.Current.MainWindow;

            CurrentEditor.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonDown -= AssociatedObject_PreviewMouseLeftButtonDown;
            CurrentEditor.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
            CurrentEditor.MouseRightButtonDown -= AssociatedObjectOnMouseRightButtonDown;
            CurrentEditor.MouseRightButtonUp -= AssociatedObjectOnMouseRightButtonUp;

            //AssociatedObject.IsHitTestVisible = false;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            }
            _lassoAdorner = null;
        }
    }
}