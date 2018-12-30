using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model;
using Chem4Word.Model.Enums;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Drawing
{
    public class BondVisual: ChemicalVisual
    {
        #region Properties

            public Bond ParentBond { get; }
            public double BondThickness { get; set; }


        #endregion

        #region Fields
            private Pen _bondPen;
            private List<Point> _enclosingPoly = new List<Point>();
        #endregion
      

        public BondVisual(Bond bond)
        {
            
            ParentBond = bond;
        }

        public Geometry GetBondGeometry(Point? startPoint, Point? endPoint)
        {
            //Vector startOffset = new Vector();
            //Vector endOffset = new Vector();
            var modelXamlBondLength = this.ParentBond.Model.XamlBondLength;

            if (startPoint != null & endPoint != null)
            {
                //check to see if it's a wedge or a hatch yet
                if (ParentBond.Stereo == BondStereo.Wedge | ParentBond.Stereo == BondStereo.Hatch)
                {
                    return BondGeometry.WedgeBondGeometry(startPoint.Value, endPoint.Value, modelXamlBondLength);
                }

                if (ParentBond.Stereo == BondStereo.Indeterminate && ParentBond.OrderValue == 1.0)
                {
                    return BondGeometry.WavyBondGeometry(startPoint.Value, endPoint.Value, modelXamlBondLength);
                }

                //single or dotted bond
                if (ParentBond.OrderValue <= 1)
                {
                    return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                }

                if (ParentBond.OrderValue == 1.5)
                {
                    //it's a resonance bond, so we deal with this in OnRender
                    //return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                    return new StreamGeometry();
                }

                //double bond
                if (ParentBond.OrderValue == 2)
                {
                    if (ParentBond.Stereo == BondStereo.Indeterminate)
                    {
                        return BondGeometry.CrossedDoubleGeometry(startPoint.Value, endPoint.Value, modelXamlBondLength,
                            ref _enclosingPoly);
                    }

                    Point? centroid = null;
                    if (ParentBond.IsCyclic())
                    {
                        centroid = ParentBond.PrimaryRing?.Centroid;
                    }

                    return BondGeometry.DoubleBondGeometry(startPoint.Value, endPoint.Value, modelXamlBondLength,
                        ParentBond.Placement,
                        ref _enclosingPoly, centroid);
                }

                //tripe bond
                if (ParentBond.OrderValue == 3)
                {
                    return BondGeometry.TripleBondGeometry(startPoint.Value, endPoint.Value, modelXamlBondLength,
                        ref _enclosingPoly);
                }

                return null;
            }

            return null;
        }

        public override void Render()
        {
            Point startPoint, endPoint;

            startPoint = ParentBond.StartAtom.Position;
            endPoint = ParentBond.EndAtom.Position;

            Geometry bondGeometry = GetBondGeometry(startPoint, endPoint);
            using (DrawingContext dc = RenderOpen())
            {
                _bondPen = new Pen(Brushes.Black, BondThickness);
                _bondPen.Thickness = BondThickness;

                _bondPen.StartLineCap = PenLineCap.Round;
                _bondPen.EndLineCap = PenLineCap.Round;

                dc.DrawGeometry(Brushes.Black, _bondPen, bondGeometry);
                
                dc.Close();
            }
        }
    }
}
