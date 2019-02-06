// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Model;
using Chem4Word.Model.Geometry;
using Chem4Word.View;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Behaviors
{
    public class RingBehavior : BaseEditBehavior
    {
        private int _ringSize;

        public int RingSize
        {
            get { return _ringSize; }
            set { _ringSize = value; }
        }

        private bool _unsaturated;
        private Window _parent;

        public RingBehavior()
        {
        }

        public RingBehavior(string ringspec) : this()
        {
            RingSize = int.Parse(ringspec[0].ToString());
            Unsaturated = ringspec[1] == 'U';
        }

        public bool Unsaturated
        {
            get { return _unsaturated; }
            set { _unsaturated = value; }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            ViewModel.SelectedItems?.Clear();

            _parent = Application.Current.MainWindow;

            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;

            AssociatedObject.IsHitTestVisible = true;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            }
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Molecule parentMolecule;

            Atom hitAtom = GetAtomUnderCursor(e)?.ParentAtom;
            Bond hitBond = GetBondUnderCursor(e)?.ParentBond;

            List<Point> placements;
            List<Point> altPlacements;

            List<NewAtomPlacement> newAtomPlacements = new List<NewAtomPlacement>();

            List<Point> preferredPlacements;
            var xamlBondSize = ViewModel.Model.XamlBondLength;

            if (hitAtom != null)
            {
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

                preferredPlacements = PaceOut(hitAtom, direction, xamlBondSize, RingSize);
                if (parentMolecule.Overlaps(preferredPlacements, new List<Atom> { hitAtom }))
                {
                    UserInteractions.AlertUser("No room left to draw any more rings!");
                    return;
                }
                //altPlacements = PaceOut(hitAtom, -direction, xamlBondSize, RingSize);
            }
            else if (hitBond != null)
            {
                parentMolecule = hitBond.Parent;
                Vector bondDirection = hitBond.BondVector;

                placements = PaceOut(hitBond, true, RingSize);
                altPlacements = PaceOut(hitBond, false, RingSize);
                if (parentMolecule != null)
                {
                    if (!parentMolecule.Overlaps(placements))
                    {
                        preferredPlacements = placements;
                    }
                    else if (!parentMolecule.Overlaps(altPlacements))
                    {
                        preferredPlacements = altPlacements;
                    }
                    else
                    {
                        UserInteractions.AlertUser("No room left to draw any more rings!");
                        return;
                    }
                }
                else
                {
                    preferredPlacements = placements;
                }
            }
            else //clicked on empty space
            {
                parentMolecule = null;
                preferredPlacements = PaceOut(e.GetPosition(AssociatedObject), BasicGeometry.ScreenNorth, xamlBondSize, RingSize);
                //altPlacements = PaceOut(e.GetPosition(AssociatedObject), BasicGeometry.ScreenSouth, xamlBondSize, RingSize);
            }

            foreach (Point placement in preferredPlacements)
            {
                var nap = new NewAtomPlacement
                {
                    ExistingAtom = (GetTarget(placement)?.VisualHit as AtomShape)?.ParentAtom,
                    Position = placement
                };
                newAtomPlacements.Add(nap);
            }

            ViewModel.DrawRing(newAtomPlacements, Unsaturated);
        }

        private List<Point> PaceOut(Atom startAtom, Vector direction, double bondSize, int ringSize)
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

            var lastPos = startAtom.Position;
            placements.Add(startAtom.Position);

            for (int i = 1; i < ringSize; i++)
            {
                var newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        private List<Point> PaceOut(Bond startBond, bool followsBond, int ringSize)
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
                var newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        private List<Point> PaceOut(Point start, Vector direction, double bondSize, int ringSize)
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

            var lastPos = start;
            placements.Add(start);

            for (int i = 1; i < ringSize; i++)
            {
                var newBondVector = bondVector * rotator;
                lastPos = lastPos + newBondVector;
                placements.Add(lastPos);
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        private AtomShape GetAtomUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        {
            var result = GetTarget(mouseButtonEventArgs.GetPosition(AssociatedObject));
            return (result?.VisualHit as AtomShape);
        }

        private BondShape GetBondUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        {
            var result = GetTarget(mouseButtonEventArgs.GetPosition(AssociatedObject));
            return (result?.VisualHit as BondShape);
        }

        private HitTestResult GetTarget(Point p)
        {
            return VisualTreeHelper.HitTest(AssociatedObject, p);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            }

            _parent = null;
        }
    }
}