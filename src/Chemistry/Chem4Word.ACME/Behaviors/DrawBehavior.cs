﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Chem4Word.Model2.Helpers.Globals;

namespace Chem4Word.ACME.Behaviors
{
    /// <summary>
    /// Does freehand drawing of atoms and bonds
    /// </summary>
    public class DrawBehavior : BaseEditBehavior
    {
        private readonly TranslateTransform _transform = new TranslateTransform();

        private AtomVisual _currentAtomVisual;
        private bool _flag;

        private Snapper _angleSnapper;
        //private Window _parent;

        private DrawBondAdorner _adorner;

        private AtomVisual _lastAtomVisual;

        private const string DefaultText = "Click existing atom to sprout a chain or modify element.";

        protected override void OnAttached()
        {
            base.OnAttached();

            CurrentEditor = (EditorCanvas)AssociatedObject;

            EditViewModel.SelectedItems?.Clear();

            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonUp += CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove += CurrentEditor_PreviewMouseMove;
            CurrentEditor.PreviewMouseRightButtonUp += CurrentEditor_PreviewMouseRightButtonUp;
            CurrentEditor.IsHitTestVisible = true;
            CurrentStatus = DefaultText;
        }

        private void CurrentEditor_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            UIUtils.DoPropertyEdit(e, CurrentEditor);
        }

        ///
        /// what happens when we move the mouse
        ///
        private void CurrentEditor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Bond existingBond = null;

            if (_adorner != null)
            {
                RemoveAdorner(ref _adorner);
            }

            var targetedVisual = CurrentEditor.ActiveVisual;
            //cherck to see if we have already got an atom remembered

            string bondOrder = EditViewModel.CurrentBondOrder;

            if (_currentAtomVisual != null)
            {
                Point lastPos;

                if (Dragging(e))
                {
                    CurrentStatus = "[Shift] to unlock length; [Ctrl] to unlock angle; [Shift][Ctrl] to unlock both.";
                    //are we already on top of an atom?

                    if (targetedVisual is AtomVisual atomUnderCursor)
                    {
                        //if so. snap to the atom's position
                        lastPos = atomUnderCursor.Position;
                        //if we are stroking over an existing bond
                        //then draw a double bond adorner

                        existingBond = _lastAtomVisual.ParentAtom.BondBetween(atomUnderCursor.ParentAtom);
                        if (_lastAtomVisual != null &&
                            existingBond != null)
                        {
                            if (existingBond.Order == OrderSingle)
                            {
                                bondOrder = OrderDouble;
                            }
                            else if (existingBond.Order == OrderDouble)
                            {
                                bondOrder = OrderTriple;
                            }
                            else if (existingBond.Order == OrderTriple)
                            {
                                bondOrder = OrderSingle;
                            }
                        }
                    }
                    else //or dangling over free space?
                    {
                        lastPos = e.GetPosition(CurrentEditor);

                        var angleBetween =
                            Vector.AngleBetween(
                                (_lastAtomVisual?.ParentAtom?.BalancingVector()) ?? BasicGeometry.ScreenNorth,
                                BasicGeometry.ScreenNorth);
                        //snap a bond into position
                        lastPos = _angleSnapper.SnapBond(lastPos, e, angleBetween);
                    }

                    _adorner = new DrawBondAdorner(CurrentEditor, BondThickness)
                    {
                        Stereo = EditViewModel.CurrentStereo,
                        BondOrder = bondOrder,
                        ExistingBond = existingBond
                    };

                    _adorner.StartPoint = _currentAtomVisual.Position;
                    _adorner.EndPoint = lastPos;
                }
            }
            else
            {
                if (targetedVisual is AtomVisual av)
                {
                    if (EditViewModel.SelectedElement != av.ParentAtom.Element)
                    {
                        CurrentStatus = "Click to set element.";
                    }
                    else
                    {
                        CurrentStatus = "Click to sprout chain";
                    }
                }
                else if (targetedVisual is BondVisual bv)
                {
                    CurrentStatus = "Click to modify bond";
                }
            }
        }

        /// <summary>
        /// What happens when we release the mouse button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentEditor_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = "";
            //first get the current active visuals
            var landedAtomVisual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor)) as AtomVisual;

            var landedBondVisual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor)) as BondVisual;
            //check to see whether or not we've clicked and released on the same atom
            bool sameAtom = landedAtomVisual == _currentAtomVisual;
            //check to see whether the target is in the same molecule
            bool sameMolecule = landedAtomVisual?.ParentAtom.Parent == _currentAtomVisual?.ParentAtom.Parent;
            //check bonds first - we can't connect to a bond so we need to simply do some stuff with it
            if (landedBondVisual != null)
            {
                //clicking on a stereo bond should just invert it
                if (landedBondVisual.ParentBond.Stereo == BondStereo.Hatch &
                    EditViewModel.CurrentStereo == BondStereo.Hatch |
                    landedBondVisual.ParentBond.Stereo == BondStereo.Wedge &
                    EditViewModel.CurrentStereo == BondStereo.Wedge)
                {
                    EditViewModel.SwapBondDirection(landedBondVisual.ParentBond);
                }
                else
                {
                    //modify the bond attribute (order, stereo, whatever's selected really)
                    EditViewModel.SetBondAttributes(landedBondVisual.ParentBond);
                }
            }
            else //we clicked on empty space or an atom
            {
                Atom parentAtom = _currentAtomVisual?.ParentAtom;
                if (landedAtomVisual == null) //no atom hit
                {
                    if (parentAtom != null)
                    {
                        if (!parentAtom.CanAddAtoms)
                        {
                            Core.UserInteractions.AlertUser("Unable to add an atom chain:  atom is saturated.");
                        }
                        //but we went mouse-down on an atom
                        else if (_currentAtomVisual != null)
                        {
                            //so just sprout a chain off it at two-o-clock
                            EditViewModel.AddAtomChain(parentAtom, _angleSnapper.SnapBond(e.GetPosition(CurrentEditor), e),
                                                   ClockDirections.II);
                        }
                        else
                        {
                            //otherwise create a singleton
                            EditViewModel.AddAtomChain(null, e.GetPosition(CurrentEditor), ClockDirections.II);
                        }
                    }
                    else
                    {
                        //create a singleton
                        //otherwise create a singleton
                        EditViewModel.AddAtomChain(null, e.GetPosition(CurrentEditor), ClockDirections.II);
                    }
                }
                else //we went mouse-up on an atom
                {
                    Atom lastAtom = landedAtomVisual.ParentAtom;
                    if (sameAtom) //both are the same atom
                    {
                        if (lastAtom.Element.Symbol != EditViewModel.SelectedElement.Symbol)
                        {
                            EditViewModel.SetElement(EditViewModel.SelectedElement, new List<Atom>() { lastAtom });
                        }
                        else
                        {
                            if (!lastAtom.CanAddAtoms)
                            {
                                Core.UserInteractions.AlertUser("Unable to add an atom chain:  atom is saturated.");
                            }
                            else
                            {
                                var atomMetrics = GetNewChainEndPos(landedAtomVisual);
                                EditViewModel.AddAtomChain(lastAtom, atomMetrics.NewPos, atomMetrics.sproutDir);
                            }
                        }
                    }
                    else //we must have hit a different atom altogether
                    {
                        //already has a bond to the target atom
                        var existingBond = parentAtom.BondBetween(lastAtom);
                        if (!parentAtom.CanAddAtoms | !lastAtom.CanAddAtoms)
                        {
                            Core.UserInteractions.AlertUser(
                                "Unable to increase bond order:  either atom is saturated.");
                        }
                        else if (existingBond != null) //it must be in the same molecule
                        {
                            EditViewModel.IncreaseBondOrder(existingBond);
                        }
                        else //doesn't have a bond to the target atom
                        {
                            if (!parentAtom.CanAddAtoms | !lastAtom.CanAddAtoms)
                            {
                                Core.UserInteractions.AlertUser("Unable to add bond:  either atom is saturated.");
                            }
                            else if (sameMolecule)
                            {
                                EditViewModel.AddNewBond(parentAtom, lastAtom,
                                                     parentAtom.Parent);
                            }
                            else
                            {
                                EditViewModel.JoinMolecules(parentAtom, lastAtom, EditViewModel.CurrentBondOrder,
                                                        EditViewModel.CurrentStereo);
                            }
                        }
                    }
                }
            }

            if (_adorner != null)
            {
                RemoveAdorner(ref _adorner);
            }

            _currentAtomVisual = null;
            _flag = false;
            //clear this to prevent a weird bug in drawing
            CurrentEditor.ActiveChemistry = null;
        }

        private bool CrowdingOut(Point p)
        {
            return CurrentEditor.GetTargetedVisual(p) is AtomVisual;
        }

        private void RemoveAdorner(ref DrawBondAdorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(adorner);
            adorner = null;
        }

        private static ClockDirections GetGeneralDir(Vector bondVector)
        {
            double bondAngle = Vector.AngleBetween(BasicGeometry.ScreenNorth, bondVector);

            ClockDirections hour = (ClockDirections)BasicGeometry.SnapToClock(bondAngle);
            return hour;
        }

        /// <summary>
        /// tells you where to put a new atom
        /// </summary>
        /// <param name="lastAtomVisual"></param>
        /// <param name="congestedPositions">Places to avoid dumping the new atom</param>
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
                newDirection = ClockDirections.II.ToVector() * EditViewModel.Model.XamlBondLength;
                newTag = ClockDirections.II;
            }
            else if (lastAtom.Degree == 1)
            {
                Vector bondVector = lastAtom.Position - lastAtom.Neighbours.First().Position;

                var hour = GetGeneralDir(bondVector);

                if (VirginAtom(lastAtom)) //it hasn't yet sprouted
                {
                    //Tag is used to store the direction the atom sprouted from its previous atom
                    newTag = GetNewSproutDirection(hour);
                    newDirection = newTag.ToVector() * EditViewModel.Model.XamlBondLength;
                }
                else //it has sprouted, so where to put the new branch?
                {
                    var vecA = ((ClockDirections)lastAtom.Tag).ToVector();
                    vecA.Normalize();
                    var vecB = -bondVector;
                    vecB.Normalize();

                    var balancingVector = -(vecA + vecB);
                    balancingVector.Normalize();
                    newTag = GetGeneralDir(balancingVector);
                    newDirection = balancingVector * EditViewModel.Model.XamlBondLength;
                }
            }
            else if (lastAtom.Degree == 2)
            {
                var balancingVector = lastAtom.BalancingVector();
                balancingVector.Normalize();
                newDirection = balancingVector * EditViewModel.Model.XamlBondLength;
                newTag = GetGeneralDir(balancingVector);
            }
            else //lastAtom.Degree >= 2:  could get congested
            {
                FindOpenSpace(lastAtom, EditViewModel.Model.XamlBondLength, out newDirection);
                newTag = GetGeneralDir(newDirection);
            }

            return (newDirection + lastAtom.Position, newTag);
        }

        private class CandidatePlacement
        {
            public int Separation { get; set; }
            public Vector Orientation { get; set; }
            public int NeighbourWeights { get; set; }
            public ClockDirections Direction => GetGeneralDir(Orientation);
            public Point PossiblePlacement { get; set; }
            public bool Crowding { get; set; }
        }

        /// <summary>
        /// Tries to find the best pace to put a bond
        /// by placing it in uncongested space
        /// </summary>
        /// <param name="rootAtom"></param>
        /// <param name="modelXamlBondLength"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        private void FindOpenSpace(Atom rootAtom, double modelXamlBondLength, out Vector vector)
        {
            //we need to work out which adjacent pairs of bonds around the atom have most space
            //first, sort each bond around the atom by its position from twelve-o-clock
            List<CandidatePlacement> possiblePlacements = new List<CandidatePlacement>();
            var atomBonds = (from b in rootAtom.Bonds
                             orderby b.AngleStartingAt(rootAtom)
                             select b).ToList();
            //add the first item in at the end so this makes comparison of pairs easier
            atomBonds.Add(atomBonds[0]);
            //now compare each bond with the previous bond and sort them by angle descending

            for (int i = 1; i < atomBonds.Count; i++)
            {
                var otherAtom = atomBonds[i - 1].OtherAtom(rootAtom);
                Vector vec0 = otherAtom.Position - rootAtom.Position;
                var atom = atomBonds[i].OtherAtom(rootAtom);
                Vector vec1 = atom.Position - rootAtom.Position;

                vec0.Normalize();
                vec1.Normalize();

                var splitDirection = vec0 + vec1;
                splitDirection.Normalize();

                var angleBetween = Vector.AngleBetween(vec0, vec1);
                if (angleBetween < 180d)
                {
                    var combinedWeights = atom.Degree + otherAtom.Degree;
                    var possiblePlacement = rootAtom.Position + (splitDirection * modelXamlBondLength);
                    CandidatePlacement cp = new CandidatePlacement
                    {
                        NeighbourWeights = combinedWeights,
                        Orientation = splitDirection,
                        Separation = (int)angleBetween,
                        PossiblePlacement = possiblePlacement,
                        Crowding = CrowdingOut(possiblePlacement)
                    };
                    possiblePlacements.Add(cp);
                }
            }

            var sortedPlacements = (from p in possiblePlacements
                                    orderby p.Crowding ascending, p.NeighbourWeights, p.Separation descending

                                    select p);

            Vector newPlacement = sortedPlacements.First().Orientation;

            newPlacement.Normalize();
            newPlacement *= modelXamlBondLength;
            vector = newPlacement;
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
                case ClockDirections.I:
                    newTag = ClockDirections.III;
                    break;

                case ClockDirections.II:
                    newTag = ClockDirections.IV;
                    break;

                case ClockDirections.III:
                    newTag = ClockDirections.II;
                    break;

                case ClockDirections.IV:
                    newTag = ClockDirections.II;
                    break;

                case ClockDirections.V:
                    newTag = ClockDirections.III;
                    break;

                case ClockDirections.VI:
                    newTag = ClockDirections.VIII;
                    break;

                case ClockDirections.VII:
                    newTag = ClockDirections.IX;
                    break;

                case ClockDirections.VIII:
                    newTag = ClockDirections.X;
                    break;

                case ClockDirections.IX:
                    newTag = ClockDirections.XI;
                    break;

                case ClockDirections.X:
                    newTag = ClockDirections.VIII;
                    break;

                case ClockDirections.XII:
                    newTag = ClockDirections.I;
                    break;

                default:
                    newTag = ClockDirections.II;
                    break;
            }
            return newTag;
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(CurrentEditor);
            _currentAtomVisual = CurrentEditor.GetTargetedVisual(position) as AtomVisual;
            if (_currentAtomVisual == null)
            {
                _angleSnapper = new Snapper(position, EditViewModel);
            }
            else
            {
                Mouse.Capture(CurrentEditor);
                _angleSnapper = new Snapper(_currentAtomVisual.ParentAtom.Position, EditViewModel);
                _lastAtomVisual = _currentAtomVisual;
            }
            _flag = true;
        }

        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & _flag;
        }

        //private AtomVisual GetAtomUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        //{
        //    var result = GetTarget(mouseButtonEventArgs.GetPosition(AssociatedObject));
        //    return (result?.VisualHit as AtomVisual);
        //}

        //private BondVisual GetBondUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        //{
        //    var result = GetTarget(mouseButtonEventArgs.GetPosition(AssociatedObject));
        //    return (result?.VisualHit as BondVisual);
        //}

        private HitTestResult GetTarget(Point p)
        {
            return VisualTreeHelper.HitTest(CurrentEditor, p);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonUp -= CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove -= CurrentEditor_PreviewMouseMove;
            CurrentEditor.PreviewMouseRightButtonUp -= CurrentEditor_PreviewMouseRightButtonUp;
            CurrentStatus = "";
        }
    }
}