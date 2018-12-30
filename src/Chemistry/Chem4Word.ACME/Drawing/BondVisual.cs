using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Automation.Peers;
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

        public Geometry GetBondGeometry(Point startPoint, Point endPoint)
        {
            //Vector startOffset = new Vector();
            //Vector endOffset = new Vector();
            var modelXamlBondLength = this.ParentBond.Model.XamlBondLength;

       
            //check to see if it's a wedge or a hatch yet
            if (ParentBond.Stereo == BondStereo.Wedge | ParentBond.Stereo == BondStereo.Hatch)
            {
               
                return BondGeometry.WedgeBondGeometry(startPoint, endPoint, modelXamlBondLength);
            }

            if (ParentBond.Stereo == BondStereo.Indeterminate && ParentBond.OrderValue == 1.0)
            {
                return BondGeometry.WavyBondGeometry(startPoint, endPoint, modelXamlBondLength);
            }

            //single or dotted bond
            if (ParentBond.OrderValue <= 1)
            {
                return BondGeometry.SingleBondGeometry(startPoint, endPoint);
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
                    return BondGeometry.CrossedDoubleGeometry(startPoint, endPoint, modelXamlBondLength,
                        ref _enclosingPoly);
                }

                Point? centroid = null;
                if (ParentBond.IsCyclic())
                {
                    centroid = ParentBond.PrimaryRing?.Centroid;
                }

                return BondGeometry.DoubleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                    ParentBond.Placement,
                    ref _enclosingPoly, centroid);
            }

            //tripe bond
            if (ParentBond.OrderValue == 3)
            {
                return BondGeometry.TripleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                    ref _enclosingPoly);
            }

            return null;
        }
        Brush GetHatchBrush()
            {
                Brush bondBrush;
                bondBrush = new LinearGradientBrush
                {
                    MappingMode = BrushMappingMode.Absolute,
                    SpreadMethod = GradientSpreadMethod.Repeat,
                    StartPoint = new Point(50, 0),
                    EndPoint = new Point(50, 5),
                    GradientStops = new GradientStopCollection()
                    {
                        new GradientStop {Offset = 0d, Color = Colors.Black},
                        new GradientStop {Offset = 0.25d, Color = Colors.Black},
                        new GradientStop {Offset = 0.25d, Color = Colors.Transparent},
                        new GradientStop {Offset = 0.30, Color = Colors.Transparent}
                    },
                    RelativeTransform = new ScaleTransform
                    {
                        ScaleX = ParentBond.HatchScaling,
                        ScaleY = ParentBond.HatchScaling
                    },
                    Transform = new RotateTransform
                    {
                        Angle = ParentBond.Angle
                    }
                };
                return bondBrush;
            }
        public override void Render()
        {
            Point startPoint, endPoint;
            //Point? idealStartPoint = null, idealEndPoint=null;
            startPoint = ParentBond.StartAtom.Position;
            endPoint = ParentBond.EndAtom.Position;
            Geometry bondGeometry = null;
            Vector bondVector = endPoint - startPoint;
            
            if(ParentBond.StartAtom.SymbolText !="")
            {
                var startAtomGeometry = ((AtomVisual) ChemicalVisuals[ParentBond.StartAtom]).WidenedGeometry;
                for (double d = 1d; d<100d; d++)
                {
                    Vector offset = bondVector * (d / 100);
                    startPoint = ParentBond.StartAtom.Position + offset;
                    
                    bondGeometry = GetBondGeometry(startPoint, endPoint);
                    var overlap = new CombinedGeometry(GeometryCombineMode.Intersect, bondGeometry,startAtomGeometry);
                    if (overlap.IsEmpty())
                    {
                      
                        break;
                    }
                }

            }

            if (ParentBond.EndAtom.SymbolText != "")
            {
                var endAtomGeometry = ((AtomVisual)ChemicalVisuals[ParentBond.EndAtom]).WidenedGeometry;
                for (double d = 1d; d < 100d; d++)
                {
                    Vector offset = bondVector * (d / 100);
                    endPoint = ParentBond.EndAtom.Position - offset;

                    bondGeometry = GetBondGeometry(startPoint, endPoint);
                    var overlap = new CombinedGeometry(GeometryCombineMode.Intersect, bondGeometry, endAtomGeometry);
                    if (overlap.IsEmpty())
                    {
                        break;
                    }
                }

            }




            using (DrawingContext dc = RenderOpen())
            {
                _bondPen = new Pen(Brushes.Black, BondThickness);
                _bondPen.Thickness = BondThickness;

                _bondPen.StartLineCap = PenLineCap.Round;
                _bondPen.EndLineCap = PenLineCap.Round;

                Brush bondBrush = Brushes.Black;
                if (ParentBond.Stereo == BondStereo.Hatch || ParentBond.Stereo==BondStereo.Wedge)
                {
                    _bondPen.Thickness = 0; //don't draw around the bonds
                    if (ParentBond.Stereo == BondStereo.Wedge)
                    {
                      bondBrush=  GetHatchBrush();
                    }
                }
                else
                {
                    bondBrush = new SolidColorBrush(Colors.Black);
                }


                dc.DrawGeometry(bondBrush, _bondPen, bondGeometry);
                
                dc.Close();
            }
        }
    }
}
