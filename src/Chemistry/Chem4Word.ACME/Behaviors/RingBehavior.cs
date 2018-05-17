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
            AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;


            AssociatedObject.IsHitTestVisible = true;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            }

        }

        private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
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
                if (overlap.GetArea(0.01, ToleranceType.Relative) < altOverlap.GetArea(0.01, ToleranceType.Relative))
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

        private List<Point> PaceOut(Atom hitAtom, Vector direction, double xamlBondSize, int ringSize)
        {
            throw new NotImplementedException();
        }

        private void _parent_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
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
