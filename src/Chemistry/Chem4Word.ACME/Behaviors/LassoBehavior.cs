// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.View;
using Chem4Word.ViewModel.Adorners;
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

        protected override void OnAttached()
        {
            base.OnAttached();

            _parent = Application.Current.MainWindow;

            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
            AssociatedObject.MouseRightButtonDown += AssociatedObjectOnMouseRightButtonDown;
            AssociatedObject.MouseRightButtonUp += AssociatedObjectOnMouseRightButtonUp;

            AssociatedObject.IsHitTestVisible = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            }
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                ViewModel.SelectedItems.Clear();
            }

            _mouseTrack = new PointCollection();
            _startpoint = Mouse.GetPosition(AssociatedObject);
            _flag = true;

            Mouse.Capture(AssociatedObject);
            _mouseTrack.Add(_startpoint);

            if (e.ClickCount == 2)
            {
                DoMolSelect(e);
                e.Handled = true;
            }

            if (e.ClickCount == 1) //single click
            {
                DoSingleSelect(e);
            }
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AssociatedObject.ReleaseMouseCapture();
            _flag = false;

            if (_lassoAdorner != null)
            {
                DisposeLasso();
            }
        }

        private void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Dragging(e))
            {
                var pos = Mouse.GetPosition(AssociatedObject);
                _mouseTrack.Add(pos);
                var outline = GetPolyGeometry();

                if (_lassoAdorner == null)
                {
                    _lassoAdorner = new LassoAdorner(AssociatedObject, outline);
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
            AssociatedObject.ReleaseMouseCapture();
        }

        private void AssociatedObjectOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("AssociatedObjectOnMouseRightButtonDown");
            Mouse.Capture(AssociatedObject);
            if (e.ClickCount == 1)
            {
                var hitTestResult = GetTarget(e);
                if (hitTestResult.VisualHit is AtomShape)
                {
                    var atom = (AtomShape)hitTestResult.VisualHit;
                    Debug.WriteLine($"Right Click Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");
                }
                else if (hitTestResult.VisualHit is BondShape)
                {
                    var bond = (BondShape)hitTestResult.VisualHit;
                    var pos = e.GetPosition(AssociatedObject);
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
            VisualTreeHelper.HitTest(AssociatedObject, null, HitTestCallback, new GeometryHitTestParameters(outline));
        }

        private void RemoveAdorner(Adorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);

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
            var hitTestResult = GetTarget(e);
            Debug.Print(hitTestResult.ToString());

            if (hitTestResult.VisualHit is AtomShape)
            {
                var atom = (AtomShape)hitTestResult.VisualHit;
                //MessageBox.Show($"Hit Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");

                ViewModel.AddToSelection(atom.ParentAtom.Parent);
            }
            else if (hitTestResult.VisualHit is BondShape)
            {
                var bond = (BondShape)hitTestResult.VisualHit;
                //MessageBox.Show($"Hit Bond {bond.ParentBond.Id} at ({e.GetPosition(AssociatedObject).X},{e.GetPosition(AssociatedObject).Y})");

                ViewModel.AddToSelection(bond.ParentBond.Parent);
            }
            else
            {
                ViewModel.SelectedItems.Clear();
            }
        }

        private void DoSingleSelect(MouseButtonEventArgs e)
        {
            var hitTestResult = GetTarget(e);
            Debug.Print(hitTestResult.ToString());

            if (hitTestResult.VisualHit is AtomShape)
            {
                var atom = (AtomShape)hitTestResult.VisualHit;
                //MessageBox.Show($"Hit Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");

                ViewModel.AddToSelection(atom.ParentAtom);
            }
            else if (hitTestResult.VisualHit is BondShape)
            {
                var bond = (BondShape)hitTestResult.VisualHit;
                //MessageBox.Show($"Hit Bond {bond.ParentBond.Id} at ({e.GetPosition(AssociatedObject).X},{e.GetPosition(AssociatedObject).Y})");

                ViewModel.AddToSelection(bond.ParentBond);
            }
            else
            {
                ViewModel.SelectedItems.Clear();
            }
        }

        private HitTestResult GetTarget(MouseButtonEventArgs e)
        {
            return VisualTreeHelper.HitTest(AssociatedObject, e.GetPosition(AssociatedObject));
        }

        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            var id = ((GeometryHitTestResult)result).IntersectionDetail;

            var myShape = (result.VisualHit);
            if (myShape != null && (myShape is AtomShape | myShape is BondShape))
            {
                switch (id)
                {
                    case IntersectionDetail.FullyContains:
                    case IntersectionDetail.Intersects:
                    case IntersectionDetail.FullyInside:
                        var selAtom = ((myShape as AtomShape)?.ParentAtom);
                        var selBond = ((myShape as BondShape)?.ParentBond);

                        if (!(ViewModel.SelectedItems.Contains(selAtom) || ViewModel.SelectedItems.Contains(selBond)))
                        {
                            if (selAtom != null)
                            {
                                ViewModel.AddToSelection(selAtom);
                            }
                            if (selBond != null)
                            {
                                ViewModel.AddToSelection(selBond);
                            }
                        }
                        return HitTestResultBehavior.Continue;

                    case IntersectionDetail.Empty:
                        selAtom = ((myShape as AtomShape)?.ParentAtom);
                        selBond = ((myShape as BondShape)?.ParentBond);

                        if ((ViewModel.SelectedItems.Contains(selAtom) || ViewModel.SelectedItems.Contains(selBond)))
                        {
                            if (selAtom != null)
                            {
                                ViewModel.RemoveFromSelection(selAtom);
                            }
                            if (selBond != null)
                            {
                                ViewModel.RemoveFromSelection(selBond);
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

            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
            AssociatedObject.MouseRightButtonDown -= AssociatedObjectOnMouseRightButtonDown;
            AssociatedObject.MouseRightButtonUp -= AssociatedObjectOnMouseRightButtonUp;

            //AssociatedObject.IsHitTestVisible = false;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            }
            _lassoAdorner = null;
        }
    }
}