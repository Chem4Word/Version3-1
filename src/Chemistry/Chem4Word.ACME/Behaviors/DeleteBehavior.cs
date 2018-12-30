// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.View;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Behaviors
{
    public class DeleteBehavior : BaseEditBehavior
    {
        //private bool _lassoVisible;
        //private PointCollection _mouseTrack;
        //private Point _startpoint;
        private Window _parent;

        //private bool _flag;
        //private LassoAdorner _lassoAdorner;
        //private MoleculeSelectionAdorner _molAdorner;

        protected override void OnAttached()
        {
            base.OnAttached();

            _parent = Application.Current.MainWindow;

            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;

            AssociatedObject.IsHitTestVisible = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            }
            //clear the current selection
            ViewModel.SelectedItems.Clear();
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hitTestResult = GetTarget(e);
            if (hitTestResult.VisualHit is AtomShape)
            {
                var atomShape = (AtomShape)hitTestResult.VisualHit;
                var atom = atomShape.ParentAtom;
                this.ViewModel.DeleteAtom(atom);
            }
            else if (hitTestResult.VisualHit is BondShape)
            {
                var bondShape = (BondShape)hitTestResult.VisualHit;
                var bond = bondShape.ParentBond;
                this.ViewModel.DeleteBond(bond);
            }
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
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
                AssociatedObject.IsHitTestVisible = false;
            }
        }
    }
}