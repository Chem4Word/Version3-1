// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Chem4Word.ACME.Utils;

using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using DrawBondAdorner = Chem4Word.ACME.Adorners.DrawBondAdorner;
using static Chem4Word.Model2.Helpers.Globals;

namespace Chem4Word.ACME.Behaviors
{
    public class DrawBehavior : BaseEditBehavior
    {
        private readonly TranslateTransform _transform = new TranslateTransform();
        private AtomVisual _currentAtomVisual;
        private bool _flag;
        private SnapGeometry _angleSnapper;
        //private Window _parent;

        private DrawBondAdorner _adorner;
        private Point _lastPos;
        private AtomVisual _lastAtomVisual;
        //private Atom _lastAtom;

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
            //if (_parent != null)
            //{
            //    _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            //}
        }

        private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_currentAtomVisual != null)
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

                        var angleBetween = Vector.AngleBetween((_lastAtomVisual?.ParentAtom?.BalancingVector) ?? BasicGeometry.ScreenNorth, BasicGeometry.ScreenNorth);

                        lastPos = _angleSnapper.SnapBond(lastPos, e, angleBetween);
                    }

                    if (_adorner == null)
                    {
                        _adorner = new DrawBondAdorner(AssociatedObject, Globals.BondThickness)
                        {
                            Stereo = ViewModel.CurrentStereo,
                            BondOrder = ViewModel.CurrentBondOrder
                        };
                    }
                    _adorner.StartPoint = _currentAtomVisual.Position;
                    _adorner.EndPoint = lastPos;
                    _lastPos = lastPos;
                }
            }
        }

        private AtomVisual GetAtomUnderCursor(MouseEventArgs e)
        {
            var result = GetTarget(e.GetPosition(AssociatedObject));
            return (result?.VisualHit as AtomVisual);
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AssociatedObject.ReleaseMouseCapture();

            var landedAtomVisual = GetAtomUnderCursor(e);
            var landedBondVisual = GetBondUnderCursor(e);

            if (landedBondVisual != null)
            {
                if (landedBondVisual.ParentBond.Stereo == Globals.BondStereo.Hatch & ViewModel.CurrentStereo == Globals.BondStereo.Hatch |
                    landedBondVisual.ParentBond.Stereo == Globals.BondStereo.Wedge & ViewModel.CurrentStereo == Globals.BondStereo.Wedge)
                {
                    ViewModel.SwapBondDirection(landedBondVisual.ParentBond);
                }
                else
                {
                    ViewModel.SetBondAttributes(landedBondVisual.ParentBond);
                }
            }
            else
            {
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (landedAtomVisual == null)  //no atom hit
                {
                    if (_currentAtomVisual != null)
                    {
                        ViewModel.AddAtomChain(_currentAtomVisual.ParentAtom, _angleSnapper.SnapBond(e.GetPosition(AssociatedObject), e), Globals.ClockDirections.Two);
                    }
                    else
                    {
                        ViewModel.AddAtomChain(null, e.GetPosition(AssociatedObject), Globals.ClockDirections.Two);
                    }
                }
                else if (landedAtomVisual == _currentAtomVisual) //both are the same atom
                {
                    var atomMetrics = GetNewChainEndPos(landedAtomVisual);

                    ViewModel.AddAtomChain(landedAtomVisual.ParentAtom, atomMetrics.NewPos, atomMetrics.sproutDir);
                }
                else //we must have hit a different atom altogether
                {
                    //already has a bond to the target atom
                    var existingBond = _currentAtomVisual.ParentAtom.BondBetween(landedAtomVisual.ParentAtom);
                    if (existingBond != null)
                    {
                        ViewModel.IncreaseBondOrder(existingBond);
                    }
                    else //doesn't have a bond to the target atom
                    {
                        ViewModel.AddNewBond(_currentAtomVisual.ParentAtom, landedAtomVisual.ParentAtom,
                            _currentAtomVisual.ParentAtom.Parent);
                    }
                }
            }

            if (_adorner != null)
            {
                RemoveAdorner(ref _adorner);
            }

            _flag = false;
        }

        private void RemoveAdorner(ref DrawBondAdorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);

            layer.Remove(adorner);
            adorner = null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="lastAtomVisual"></param>
        /// <returns></returns>
        private (Point NewPos, ClockDirections sproutDir) GetNewChainEndPos(AtomVisual lastAtomVisual)
        {
            ClockDirections GetGeneralDir(Vector bondVector)
            {
                double bondAngle = Vector.AngleBetween(BasicGeometry.ScreenNorth, bondVector);

                ClockDirections hour = (ClockDirections)BasicGeometry.SnapToClock(bondAngle);
                return hour;
            }

            var lastAtom = lastAtomVisual.ParentAtom;
            Vector newDirection;

            ClockDirections newTag;

            if (lastAtom.Degree == 0) //isolated atom
            {
                newDirection = ClockDirections.Two.ToVector() * ViewModel.Model.XamlBondLength;
                newTag = ClockDirections.Two;
            }
            else if (lastAtom.Degree == 1)
            {
                Vector bondVector = lastAtom.Position - lastAtom.Neighbours.First().Position;

                var hour = GetGeneralDir(bondVector);

                if (VirginAtom(lastAtom)) //it hasn't yet sprouted
                {
                    //Tag is used to store the direction the atom sprouted from its previous atom
                    newTag = GetNewSproutDirection(hour);
                    newDirection = newTag.ToVector() * ViewModel.Model.XamlBondLength;
                }
                else //it has sprouted, so where to put the new branch?
                {
                    var vecA = ((ClockDirections)lastAtom.Tag).ToVector();
                    vecA.Normalize();
                    var vecB = -bondVector;
                    vecB.Normalize();

                    var balancingvector = -(vecA + vecB);
                    balancingvector.Normalize();
                    newTag = GetGeneralDir(balancingvector);
                    newDirection = balancingvector * ViewModel.Model.XamlBondLength;
                }
            }
            else
            {
                newDirection = lastAtom.BalancingVector * ViewModel.Model.XamlBondLength;
                newTag = GetGeneralDir(newDirection);
            }
            return (newDirection + lastAtom.Position, newTag);
        }

        private bool VirginAtom(Atom lastAtom)
        {
            return lastAtom.Tag == null;
        }

        private static ClockDirections GetNewSproutDirection(ClockDirections hour)
        {
            ClockDirections newTag;
            switch (hour)
            {
                case ClockDirections.One:
                    newTag = ClockDirections.Four;
                    break;

                case ClockDirections.Two:
                    newTag = ClockDirections.Four;
                    break;

                case ClockDirections.Three:
                    newTag = ClockDirections.Two;
                    break;

                case ClockDirections.Four:
                    newTag = ClockDirections.Two;
                    break;

                case ClockDirections.Five:
                    newTag = ClockDirections.Two;
                    break;

                case ClockDirections.Six:
                    newTag = ClockDirections.Eight;
                    break;

                case ClockDirections.Seven:
                    newTag = ClockDirections.Nine;
                    break;

                case ClockDirections.Eight:
                    newTag = ClockDirections.Ten;
                    break;

                case ClockDirections.Nine:
                    newTag = ClockDirections.Ten;
                    break;

                case ClockDirections.Ten:
                    newTag = ClockDirections.Eight;
                    break;

                case ClockDirections.Twelve:
                    newTag = ClockDirections.Two;
                    break;

                default:
                    newTag = ClockDirections.Two;
                    break;
            }
            return newTag;
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _currentAtomVisual = GetAtomUnderCursor(e);
            if (_currentAtomVisual == null)
            {
                _angleSnapper = new SnapGeometry(e.GetPosition(relativeTo: AssociatedObject), ViewModel);
            }
            else
            {
                Mouse.Capture(AssociatedObject);
                _angleSnapper = new SnapGeometry(_currentAtomVisual.ParentAtom.Position, ViewModel);
                _lastAtomVisual = _currentAtomVisual;
            }
            _flag = true;
        }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & _flag;
        }

        private AtomVisual GetAtomUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        {
            var result = GetTarget(mouseButtonEventArgs.GetPosition(AssociatedObject));
            return (result?.VisualHit as AtomVisual);
        }

        private BondVisual GetBondUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        {
            var result = GetTarget(mouseButtonEventArgs.GetPosition(AssociatedObject));
            return (result?.VisualHit as BondVisual);
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
            //AssociatedObject.IsHitTestVisible = false;
            //if (_parent != null)
            //{
            //    _parent.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            //}
        }
    }
}