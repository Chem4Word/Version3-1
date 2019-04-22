// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.Core;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Behaviors
{
    /// <summary>
    /// Puts the editor into fixed ring mode.
    /// </summary>
    public class RingBehavior : BaseEditBehavior
    {
        public int RingSize { get; set; }

        private Window _parent;
        private FixedRingAdorner _currentAdorner;

        public FixedRingAdorner CurrentAdorner
        {
            get { return _currentAdorner; }
            set
            {
                RemoveRingAdorner();
                _currentAdorner = value;
                if (_currentAdorner != null)
                {
                    _currentAdorner.MouseLeftButtonDown += CurrentAdornerOnMouseLeftButtonDown;
                }
                //local function
                void RemoveRingAdorner()
                {
                    if (_currentAdorner != null)
                    {
                        var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                        layer.Remove(_currentAdorner);
                        _currentAdorner.MouseLeftButtonDown -= CurrentAdornerOnMouseLeftButtonDown;
                        _currentAdorner = null;
                    }
                }
            }
        }

        public RingBehavior()
        {
        }

        public RingBehavior(string ringspec) : this()
        {
            RingSize = int.Parse(ringspec[0].ToString());
            Unsaturated = ringspec[1] == 'U';
        }

        public bool Unsaturated { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            EditViewModel.SelectedItems?.Clear();

            CurrentEditor = (EditorCanvas)AssociatedObject;

            _parent = Application.Current.MainWindow;

            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove += CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp += CurrentEditor_MouseLeftButtonUp;
            AssociatedObject.IsHitTestVisible = true;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }

            CurrentStatus = "Draw a ring by clicking on a bond, atom or free space.";
        }

        private void CurrentEditor_MouseMove(object sender, MouseEventArgs e)
        {
            List<Point> altPlacements;

            CurrentAdorner = null;

            List<Point> preferredPlacements;
            double xamlBondSize = EditViewModel.Model.XamlBondLength;

            switch (CurrentEditor.ActiveVisual)
            {
                case AtomVisual av:
                    IdentifyPlacements(av.ParentAtom, xamlBondSize, out preferredPlacements, RingSize);
                    if (preferredPlacements != null)
                    {
                        CurrentAdorner = new FixedRingAdorner(CurrentEditor, EditViewModel.EditBondThickness, preferredPlacements, Unsaturated);
                        if (av.ParentAtom.Degree >= 2)
                        {
                            CurrentStatus="Click to spiro-fuse.";
                        }
                        else
                        {
                            CurrentStatus = "Click to draw a terminating ring.";
                        }
                    }
                    

                    break;

                case BondVisual bv:
                    IdentifyPlacements(bv.ParentBond, out altPlacements, out preferredPlacements, RingSize);
                    if (preferredPlacements != null | altPlacements != null)
                    {
                        CurrentAdorner = new FixedRingAdorner(CurrentEditor, EditViewModel.EditBondThickness, preferredPlacements??altPlacements, Unsaturated);
                        CurrentStatus = "Click to fuse a ring";
                    }
                    break;

                default:
                    preferredPlacements = MarkOutAtoms(e.GetPosition(AssociatedObject), BasicGeometry.ScreenNorth, xamlBondSize, RingSize);
                    CurrentAdorner = new FixedRingAdorner(CurrentEditor, EditViewModel.EditBondThickness, preferredPlacements, Unsaturated);
                    CurrentStatus = "Click to draw a standalone ring";
                    break;
            }
        }

        private void CurrentAdornerOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.CurrentEditor_MouseLeftButtonDown(sender, e);
        }

        private void CurrentEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //throw new System.NotImplementedException();
            CurrentStatus = "";
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Atom hitAtom = CurrentEditor.ActiveAtomVisual?.ParentAtom;
            Bond hitBond = CurrentEditor.ActiveBondVisual?.ParentBond;

            List<Point> altPlacements = null;
            int startAt = 0; //used to change double bond positions in isolated odd numbered rings
            List<NewAtomPlacement> newAtomPlacements = new List<NewAtomPlacement>();

            List<Point> preferredPlacements;
            double xamlBondSize = EditViewModel.Model.XamlBondLength;

            if (hitAtom != null)
            {
                IdentifyPlacements(hitAtom, xamlBondSize, out preferredPlacements, RingSize);
                if (preferredPlacements == null)
                {
                    UserInteractions.AlertUser("No room left to draw any more rings!");
                }
                else if (preferredPlacements.Count % 2 == 1)
                {
                    startAt = 1;
                }
            }
            else if (hitBond != null)
            {
                IdentifyPlacements(hitBond, out altPlacements, out preferredPlacements, RingSize);
                if (altPlacements == null & preferredPlacements == null)
                {
                    UserInteractions.AlertUser("No room left to draw any more rings!");
                }
            }
            else //clicked on empty space
            {
                preferredPlacements = MarkOutAtoms(e.GetPosition(AssociatedObject), BasicGeometry.ScreenNorth, xamlBondSize, RingSize);
                //al4tPlacements = MarkOutAtoms(e.GetPosition(AssociatedObject), BasicGeometry.ScreenSouth, xamlBondSize, RingSize);
                if (preferredPlacements.Count % 2 == 1)
                {
                    startAt = 1;
                }
            }

            FillExistingAtoms(preferredPlacements, altPlacements, newAtomPlacements, CurrentEditor);

            EditViewModel.DrawRing(newAtomPlacements, Unsaturated, startAt);
            CurrentAdorner = null;
        }

        public static void FillExistingAtoms(List<Point> preferredPlacements,
                                             List<Point> altPlacements,
                                             List<NewAtomPlacement> newAtomPlacements,
                                             EditorCanvas currentEditor)
        {
            foreach (Point placement in (preferredPlacements ?? altPlacements))
            {
                NewAtomPlacement nap = new NewAtomPlacement
                {
                    ExistingAtom =
                                               (currentEditor.GetTargetedVisual(placement) as AtomVisual)?.ParentAtom,
                    Position = placement
                };
                newAtomPlacements.Add(nap);
            }
        }

        public static void IdentifyPlacements(Atom hitAtom, double xamlBondSize, out List<Point> preferredPlacements, int ringSize)
        {
            Molecule parentMolecule;
            parentMolecule = hitAtom.Parent;
            Vector direction;
            if (hitAtom.Degree != 0)
            {
                direction = hitAtom.BalancingVector;
            }
            else
            {
                direction = BasicGeometry.ScreenNorth;
            }

            //try to work out exactly where best to place the ring

            preferredPlacements = MarkOutAtoms(hitAtom, direction, xamlBondSize, ringSize);
            if (parentMolecule.Overlaps(preferredPlacements, new List<Atom> { hitAtom }))
            {
                preferredPlacements = null;
            }
        }

        public static void IdentifyPlacements(Bond hitBond, out List<Point> altPlacements, out List<Point> preferredPlacements, int ringSize)
        {
            Molecule parentMolecule;
            List<Point> placements;
            parentMolecule = hitBond.Parent;
            Vector bondDirection = hitBond.BondVector;

            placements = MarkOutAtoms(hitBond, true, ringSize: ringSize);
            PathGeometry firstOverlap = parentMolecule.OverlapArea(placements);
            double firstOverlapArea = firstOverlap.GetArea();
            altPlacements = MarkOutAtoms(hitBond, false, ringSize: ringSize);
            PathGeometry secondOverlap = parentMolecule.OverlapArea(altPlacements);
            double secondOverlapArea = secondOverlap.GetArea();

            //get a point on the less crowded side of the bond
            var perpvector = hitBond.GetUncrowdedSideVector();
            if (perpvector != null)
            {
                var vec = perpvector.Value;
                vec.Normalize();
                vec *= hitBond.BondVector.Length / 2;
                Point placementPoint = hitBond.MidPoint + vec;

                if (!firstOverlap.FillContains(placementPoint))
                {
                    preferredPlacements = placements;
                }
                else if (!secondOverlap.FillContains(placementPoint))
                {
                    preferredPlacements = altPlacements;
                }
                else
                {
                    if (firstOverlapArea <= secondOverlapArea)
                    {
                        preferredPlacements = placements;
                    }
                    else if (!parentMolecule.Overlaps(altPlacements))
                    {
                        preferredPlacements = altPlacements;
                    }
                    else
                    {
                        preferredPlacements = null;
                        altPlacements = null;
                    }
                }
            }
            else
            {
                preferredPlacements = null;
                altPlacements = null;
            }
        }

        /// <summary>
        /// Paces out the proposed placement points for a ring attached to one atom
        /// </summary>
        /// <param name="startAtom"></param>
        /// <param name="direction"></param>
        /// <param name="bondSize"></param>
        /// <param name="ringSize"></param>
        /// <returns></returns>
        public static List<Point> MarkOutAtoms(Atom startAtom, Vector direction, double bondSize, int ringSize)
        {
            List<Point> placements = new List<Point>();

            //the direction vector points towards the centre of the ring from the start atom
            //so, assuming we are going clockwise, we take the perpendicular of the vector
            //rotate through -90 degrees and then clockwise through half the angle.
            //subsequent rotations are through the full exterior angle

            double exteriorAngle = 360.0 / ringSize;
            Matrix rotator = new Matrix();

            //do the initial rotation
            rotator.Rotate(-90);
            rotator.Rotate(exteriorAngle / 2);

            Vector bondVector = direction;
            bondVector.Normalize();
            bondVector *= bondSize;

            Point lastPos = startAtom.Position;
            placements.Add(startAtom.Position);

            for (int i = 1; i < ringSize; i++)
            {
                Vector newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        public static List<Point> MarkOutAtoms(Bond startBond, bool followsBond, int ringSize)
        {
            List<Point> placements = new List<Point>();

            Point lastPos, nextPos;

            Vector bondVector;
            if (followsBond)
            {
                bondVector = startBond.EndAtom.Position - startBond.StartAtom.Position;
                lastPos = startBond.StartAtom.Position;
                nextPos = startBond.EndAtom.Position;
            }
            else
            {
                bondVector = startBond.StartAtom.Position - startBond.EndAtom.Position;
                lastPos = startBond.EndAtom.Position;
                nextPos = startBond.StartAtom.Position;
            }

            double exteriorAngle = 360.0 / ringSize;
            Matrix rotator = new Matrix();

            placements.Add(lastPos);

            for (int i = 1; i < ringSize; i++)
            {
                Vector newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        public static List<Point> MarkOutAtoms(Point start, Vector direction, double bondSize, int ringSize)
        {
            List<Point> placements = new List<Point>();

            //the direction vector points towards the centre of the ring from the start atom
            //so, assuming we are going clockwise, we take the perpendicular of the vector
            //rotate through -90 degrees and then clockwise through half the angle.
            //subsequent rotations are through the full exterior angle

            double exteriorAngle = 360.0 / ringSize;
            Matrix rotator = new Matrix();

            //do the initial rotation
            rotator.Rotate(-90);
            rotator.Rotate(exteriorAngle / 2);

            Vector bondVector = direction;
            bondVector.Normalize();
            bondVector *= bondSize;

            Point lastPos = start;
            placements.Add(start);

            for (int i = 1; i < ringSize; i++)
            {
                Vector newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        protected override void OnDetaching()
        {
            CurrentAdorner = null;
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove -= CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
            CurrentEditor = null;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            }

            _parent = null;
        }
    }
}