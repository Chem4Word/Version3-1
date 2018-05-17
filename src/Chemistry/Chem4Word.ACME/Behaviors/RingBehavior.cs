using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.Model;
using Chem4Word.View;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Behaviors
{
    public class RingBehavior :BaseEditBehavior
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

        public RingBehavior(string ringspec):this()
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


            if (hitAtom != null)
            {
                parentMolecule = hitAtom.Parent;
                Vector direction = hitAtom.BalancingVector;

                //try to work out exactly where best to place the ring

                var xamlBondSize = ViewModel.Model.XamlBondLength;

                placements = PaceOut(hitAtom, direction, xamlBondSize, RingSize);
                altPlacements = PaceOut(hitAtom, -direction, xamlBondSize, RingSize);

                var overlap = GetOverlap(parentMolecule, placements);
                var altOverlap = GetOverlap(parentMolecule, altPlacements);

                List<Point> preferredPlacements;
                if (overlap.GetArea(0.0001, ToleranceType.Relative) < altOverlap.GetArea(0.0001, ToleranceType.Relative))
                {
                    preferredPlacements = placements;
                }
                else
                {
                    preferredPlacements = altPlacements;
                }

                List<NewAtomPlacement> newAtomPlacements = new List<NewAtomPlacement>();

                foreach (Point placement in preferredPlacements)
                {
                    var nap = new NewAtomPlacement
                    {
                        ExistingAtom = (GetTarget(placement).VisualHit as AtomShape)?.ParentAtom,
                        Position = placement
                    };
                    newAtomPlacements.Add(nap);
                }

                ViewModel.DrawRing(newAtomPlacements, Unsaturated);

            }
            else if (hitBond!=null)
            {
                parentMolecule = hitBond.Parent;
                Vector bondDirection = hitBond.BondVector;

            }
        }

        private static CombinedGeometry GetOverlap(Molecule parentMolecule, List<Point> placements)
        {
            Polygon molPolygon = new Polygon();
            molPolygon.Points = new PointCollection(parentMolecule.ConvexHull.Select(a => a.Position));

            Polygon firstRing = new Polygon();
            firstRing.Points = new PointCollection(placements);
            var firstRingGeo = firstRing.RenderedGeometry;
            var combinedGeo = new CombinedGeometry(GeometryCombineMode.Intersect, firstRingGeo, molPolygon.RenderedGeometry);
            return combinedGeo;
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
            rotator.Rotate(exteriorAngle/2);

            Vector bonddir = direction;
            bonddir.Normalize();
            bonddir *= bondSize;


            var lastPos = startAtom.Position;
            placements.Add(startAtom.Position);

            for (int i = 1; i < ringSize; i++)
            {
                bonddir = bonddir * rotator;
                lastPos = lastPos + bonddir;
                placements.Add(lastPos);
                rotator = new Matrix();
                rotator.Rotate(exteriorAngle);
            }
            return placements;
        }

        private List<Point> PaceOut(Bond startBond, bool followsBond, double bondSize, int ringSize)
        {
            List<Point> placements = new List<Point>();

            Point lastPos;

            Vector bondVector;
            if (followsBond)
            {
                bondVector = startBond.EndAtom.Position - startBond.StartAtom.Position;
                lastPos = startBond.StartAtom.Position;

            }
            else
            {
                bondVector = startBond.StartAtom.Position - startBond.EndAtom.Position;
                lastPos = startBond.EndAtom.Position;
            }

            double exteriorAngle = 360.0 / ringSize;
            Matrix rotator = new Matrix();

            placements.Add(lastPos);
            for (int i = 1; i < ringSize; i++)
            {
                bondVector = bondVector * rotator;
                lastPos = lastPos + bondVector;
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
            base.OnDetaching();
        }
    }
}
