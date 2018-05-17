// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Utils;
using Chem4Word.Model;
using Chem4Word.View;
using Chem4Word.ViewModel.Adorners;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Behaviors
{
    public class DrawBehavior : BaseEditBehavior
    {
        private readonly TranslateTransform _transform = new TranslateTransform();
        private AtomShape _currentAtomShape;
        private bool _flag;
        private SnapGeometry _angleSnapper;
        private Window _parent;

        private DrawBondAdorner _dba;

        public DrawBehavior()
        {
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            ViewModel.SelectedItems?.Clear();

          

            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;

            AssociatedObject.IsHitTestVisible = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            }
        }

        private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_currentAtomShape != null)
            {
                Point lastPos;

                if (Dragging(e))
                {
                    var atomUnderCursor = GetAtomUnderCursor(e);
                    if (atomUnderCursor != null)
                    {
                        lastPos = atomUnderCursor.Position;
                    }
                    else
                    {
                        lastPos = e.GetPosition(AssociatedObject);
                    }

                    if (_dba == null)
                    {
                        _dba = new DrawBondAdorner(AssociatedObject)
                        {
                            Stereo = ViewModel.CurrentStereo,
                            BondOrder = ViewModel.CurrentBondOrder
                        };
                    }

                    _dba.StartPoint = _currentAtomShape.Position;
                    _dba.EndPoint = lastPos;
                }
            }
        }

        private AtomShape GetAtomUnderCursor(MouseEventArgs e)
        {
            var result = GetTarget(e.GetPosition(AssociatedObject));
            return (result?.VisualHit as AtomShape);
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AssociatedObject.ReleaseMouseCapture();

            var landedAtomShape = GetAtomUnderCursor(e);

            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (landedAtomShape == null)  //no atom hit
            {
                ViewModel.AddAtomChain(_currentAtomShape?.ParentAtom, e.GetPosition(AssociatedObject));
            }
            else if (landedAtomShape == _currentAtomShape) //both are the same atom
            {
                Point newAtomPos = GetNewChainEndPos(landedAtomShape);

                ViewModel.AddAtomChain(landedAtomShape.ParentAtom, newAtomPos);
            }
            else //we must have hit a different atom altogether
            {
                //already has a bond to the target atom
                var existingBond = _currentAtomShape.ParentAtom.BondBetween(landedAtomShape.ParentAtom);
                if (existingBond != null)
                {
                    ViewModel.IncreaseBondOrder(existingBond);
                }
                else //doesn't have a bond to the target atom
                {
                    ViewModel.AddNewBond(_currentAtomShape.ParentAtom, landedAtomShape.ParentAtom,
                    _currentAtomShape.ParentAtom.Parent);
                }
            }

            if (_dba != null)
            {
                RemoveAdorner(ref _dba);
            }

            _flag = false;
        }

        private void RemoveAdorner(ref DrawBondAdorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);

            layer.Remove(adorner);
            adorner = null;
        }

        private Point GetNewChainEndPos(AtomShape lastAtomShape)
        {
            Atom lastAtom = lastAtomShape.ParentAtom;
            Vector newDirection = lastAtom.BalancingVector * ViewModel.Model.XamlBondLength;
            return lastAtom.Position + newDirection;
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _currentAtomShape = GetAtomUnderCursor(e);
            if (_currentAtomShape != null)
            {
                Mouse.Capture(AssociatedObject);
            }

            _flag = true;

            _angleSnapper = new SnapGeometry(e.GetPosition(relativeTo: AssociatedObject));
        }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & _flag;
        }

        private AtomShape GetAtomUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        {
            var result = GetTarget(mouseButtonEventArgs.GetPosition(AssociatedObject));
            return (result?.VisualHit as AtomShape);
        }

        private HitTestResult GetTarget(Point p)
        {
            return VisualTreeHelper.HitTest(AssociatedObject, p);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
            AssociatedObject.IsHitTestVisible = false;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            }
        }
    }
}