// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public class BondVisual : ChemicalVisual
    {
        #region Properties

        public Bond ParentBond { get; }
        public double BondThickness { get; set; }

        #endregion Properties

        #region Fields

        private Pen _mainBondPen;
        private Pen _subsidiaryBondPen;
        private List<Point> _enclosingPoly = new List<Point>();

        #endregion Fields

        private Geometry _hullGeometry;
        private Geometry _bondGeometry;

        public Geometry HullGeometry
        {
            get

            {
                if (_hullGeometry == null)
                {
                    if (_enclosingPoly != null && _enclosingPoly.Count > 0) //it's not a single-line bond
                    {
                        var result = BasicGeometry.BuildPolyPath(_enclosingPoly);

                        _hullGeometry = new CombinedGeometry(result,
                                                             result.GetWidenedPathGeometry(
                                                                 new Pen(Brushes.Black, BondThickness)));
                    }
                }

                return _hullGeometry;
            }
        }

        public BondVisual(Bond bond)
        {
            ParentBond = bond;
        }

        public Geometry GetBondGeometry(Point startPoint, Point endPoint,
                                        Geometry startAtomGeometry = null, Geometry endAtomGeometry = null)
        {
            //Vector startOffset = new Vector();
            //Vector endOffset = new Vector();
            var modelXamlBondLength = this.ParentBond.Model.XamlBondLength;

            if (GetBondGeometry(startPoint, endPoint, startAtomGeometry, endAtomGeometry, modelXamlBondLength,
                                out var singleBondGeometry, ParentBond, out _enclosingPoly))
            {
                return singleBondGeometry;
            }

            return null;
        }

        public static bool GetBondGeometry(Point startPoint, Point endPoint, Geometry startAtomGeometry,
                                           Geometry endAtomGeometry,
                                           double modelXamlBondLength, out Geometry bondGeometry, Bond parentBond,
                                           out List<Point> enclosingPoly, bool ignoreCentroid = false)
        {
            enclosingPoly = null;
            //check to see if it's a wedge or a hatch yet
            if (parentBond.Stereo == Globals.BondStereo.Wedge | parentBond.Stereo == Globals.BondStereo.Hatch)
            {
                {
                    bondGeometry = BondGeometry.WedgeBondGeometry(startPoint, endPoint, modelXamlBondLength,
                                                                  startAtomGeometry, endAtomGeometry);
                    return true;
                }
            }

            if (parentBond.Stereo == Globals.BondStereo.Indeterminate && parentBond.OrderValue == 1.0)
            {
                {
                    bondGeometry = BondGeometry.WavyBondGeometry(startPoint, endPoint, modelXamlBondLength,
                                                                 startAtomGeometry, endAtomGeometry);
                    return true;
                }
            }

            //single or dotted bond
            if (parentBond.OrderValue <= 1)
            {
                {
                    bondGeometry =
                        BondGeometry.SingleBondGeometry(startPoint, endPoint, startAtomGeometry, endAtomGeometry);
                    return true;
                }
            }

            switch (parentBond.OrderValue)
            {
                //double bond
                case 1.5:
                case 2.5:
                    //it's a resonance bond, so we deal with this in Render
                    //as we can't return a single geometry that can be
                    //stroked with two different brushes
                    //return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                    bondGeometry = new StreamGeometry();
                    return true;
                case 2 when parentBond.Stereo == Globals.BondStereo.Indeterminate:
                    bondGeometry = BondGeometry.CrossedDoubleGeometry(startPoint, endPoint, modelXamlBondLength,
                                                                      ref enclosingPoly, startAtomGeometry,
                                                                      endAtomGeometry);
                    return true;
                //tripe bond
                case 2:
                    Point? centroid = null;
                    if (parentBond.IsCyclic())
                    {
                        centroid = parentBond.PrimaryRing?.Centroid;
                    }

                {
                    if (!ignoreCentroid)
                    {
                        bondGeometry = BondGeometry.DoubleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                          parentBond.Placement,
                          ref enclosingPoly, centroid,  parentBond.SubsidiaryRing?.Centroid, startAtomGeometry, endAtomGeometry);
                        return true;
                    }
                    else
                    {
                        bondGeometry = BondGeometry.DoubleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                                                                             parentBond.Placement,
                                                                             ref enclosingPoly, null, null,  startAtomGeometry, endAtomGeometry);
                        return true;
                    }
                }
                case 3:
                    bondGeometry = BondGeometry.TripleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                                                                   ref enclosingPoly, startAtomGeometry,
                                                                   endAtomGeometry);
                    return true;
                default:
                    bondGeometry = null;
                    return false;
            }
        }

        private Brush GetHatchBrush()
        {
            Brush bondBrush;
            bondBrush = new LinearGradientBrush
                        {
                            MappingMode = BrushMappingMode.Absolute,
                            SpreadMethod = GradientSpreadMethod.Repeat,
                            StartPoint = new Point(50, 0),
                            EndPoint = new Point(50, 3),
                            GradientStops = new GradientStopCollection()
                                            {
                                                new GradientStop {Offset = 0d, Color = Colors.Black},
                                                new GradientStop {Offset = 0.25d, Color = Colors.Black},
                                                new GradientStop {Offset = 0.25d, Color = Colors.Transparent},
                                                new GradientStop {Offset = 0.30, Color = Colors.Transparent}
                                            },
                        
                            Transform = new RotateTransform
                                        {
                                            Angle = ParentBond.Angle
                                        }
                        };
            return bondBrush;
        }

        /// <summary>
        /// Renders a bond to the display
        /// </summary>
       public override void Render()
        {
            //set up the shared variables first
            Point startPoint, endPoint;
            //Point? idealStartPoint = null, idealEndPoint=null;
            startPoint = ParentBond.StartAtom.Position;
            endPoint = ParentBond.EndAtom.Position;
            Geometry bondGeometry = null;
            Vector bondVector = endPoint - startPoint;
            var bondLength = ParentBond.Model.XamlBondLength;
            var cv1 = ChemicalVisuals.ContainsKey(ParentBond.StartAtom);
            var cv2 = ChemicalVisuals.ContainsKey(ParentBond.EndAtom);

            Geometry startAtomGeometry;
            Geometry endAtomGeometry;
            Point point1 = new Point(0, 0),
                  point2 = new Point(0, 0),
                  point3 = new Point(0, 0),
                  point4 = new Point(0, 0);

            //bale out in case we have a null start or end
            if (!cv1 || !cv2)
            {
                // Hack: Abort if either ChemicalVisual is missing !
                return;
            }

            //now get the geometry of start and end atoms
            startAtomGeometry = ((AtomVisual) ChemicalVisuals[ParentBond.StartAtom]).WidenedHullGeometry;
            endAtomGeometry = ((AtomVisual) ChemicalVisuals[ParentBond.EndAtom]).WidenedHullGeometry;
            _bondGeometry = bondGeometry = GetBondGeometry(startPoint, endPoint, startAtomGeometry, endAtomGeometry);

            //set up the default pens for rendering
            _mainBondPen = new Pen(Brushes.Black, BondThickness)
                           {
                               StartLineCap = PenLineCap.Round,
                               EndLineCap = PenLineCap.Round
                           };

            _subsidiaryBondPen = _mainBondPen.Clone();


            switch (ParentBond.Order)
            {
                case Globals.OrderZero:
                case "unknown":
                    // Handle Zero Bond 
                    _mainBondPen.DashStyle = DashStyles.Dot;
                    //grab the enclosing polygon as for a double ParentBond - this overcomes a hit testing bug


                    if (startAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref startPoint, endPoint, startAtomGeometry);
                    }

                    if (endAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref endPoint, startPoint, endAtomGeometry);
                    }

                    using (DrawingContext dc = RenderOpen())
                    {
                        dc.DrawLine(_mainBondPen, startPoint, endPoint);
                        //we need to draw another transparent thicker line on top of the existing one
                        dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, BondThickness * 4),
                                        _bondGeometry);
                        dc.Close();
                    }

                    _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                                                                      ParentBond.Placement, null, out _,
                                                                      out _, out _, out _);
                    break;

                case Globals.OrderPartial01:
                    _mainBondPen.DashStyle = DashStyles.Dash;
                    
                    if (startAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref startPoint, endPoint, startAtomGeometry);
                    }
                    if (endAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref endPoint, startPoint, endAtomGeometry);
                    }

                    using (DrawingContext dc = RenderOpen())
                    {
                        dc.DrawLine(_mainBondPen, startPoint, endPoint);
                        //we need to draw another transparent thicker line on top of the existing one
                        dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, BondThickness * 4),
                                        _bondGeometry);
                        dc.Close();
                    }

                    //grab the enclosing polygon as for a double ParentBond - this overcomes a hit testing bug
                    _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                                                                      ParentBond.Placement, null, out _,
                                                                      out _, out _, out _);

                    break;

                case "1":
                case Globals.OrderSingle:
                    // Handle Single bond 
                    switch (ParentBond.Stereo)
                    {
                        case Globals.BondStereo.Indeterminate:
                            //draw a wavy bond

                            bondGeometry =
                                BondGeometry.WavyBondGeometry(startPoint, endPoint, bondLength, startAtomGeometry,
                                                              endAtomGeometry);
                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawGeometry(null, _mainBondPen, bondGeometry);

                                dc.Close();
                            }

                            break;
                        case Globals.BondStereo.Hatch:
                            bondGeometry =
                                BondGeometry.WedgeBondGeometry(startPoint, endPoint, bondLength, startAtomGeometry,
                                                               endAtomGeometry);
                            _mainBondPen.Thickness = 0d;
                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawGeometry(GetHatchBrush(), _mainBondPen, bondGeometry);

                                dc.Close();
                            }

                            break;
                        case Globals.BondStereo.Wedge:
                            bondGeometry =
                                bondGeometry =
                                    BondGeometry.WedgeBondGeometry(startPoint, endPoint, bondLength, startAtomGeometry,
                                                                   endAtomGeometry);
                            _mainBondPen.Thickness = 0d;
                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawGeometry(Brushes.Black, _mainBondPen, bondGeometry);

                                dc.Close();
                            }

                            break;
                        default:
                            if (startAtomGeometry != null)
                            {
                                BondGeometry.AdjustTerminus(ref startPoint, endPoint, startAtomGeometry);
                            }
                            if (endAtomGeometry != null)
                            {
                                BondGeometry.AdjustTerminus(ref endPoint, startPoint, endAtomGeometry);
                            }

                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawLine(_mainBondPen, startPoint, endPoint);
                                //we need to draw another transparent thicker line on top of the existing one
                                dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, BondThickness * 4),
                                                _bondGeometry);
                                dc.Close();
                            }

                            //grab the enclosing polygon as for a double ParentBond - this overcomes a hit testing bug
                            _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                                                                              ParentBond.Placement, null, out _,
                                                                              out _, out _, out _);
                         break;
                    }

                    break;

                case Globals.OrderPartial12:
                case Globals.OrderAromatic:
                    // Handle 1.5 bond 
                    Point? centroid = ParentBond.PrimaryRing?.Centroid;
                    _subsidiaryBondPen.DashStyle = DashStyles.Dash;
                    _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,ParentBond.Placement, centroid,
                                                                      out point1, out point2, out point3, out point4);
                    _subsidiaryBondPen.DashStyle = DashStyles.Dash;
                    if (startAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref point1, point2, startAtomGeometry);
                        BondGeometry.AdjustTerminus(ref point3, point4, startAtomGeometry);
                        _enclosingPoly = new List<Point> { point1, point2, point4, point3 };
                    }

                    if (endAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref point4, point3, endAtomGeometry);
                        BondGeometry.AdjustTerminus(ref point2, point1, endAtomGeometry);
                        _enclosingPoly = new List<Point> { point1, point2, point4, point3 };
                    }
                    using (DrawingContext dc = RenderOpen())
                    {
                        dc.DrawLine(_mainBondPen, point1, point2);
                        dc.DrawLine(_subsidiaryBondPen, point3, point4);
                        dc.Close();
                    }
                    break;

                case "2":
                case Globals.OrderDouble:
                    // Handle Double bond 
                    if (ParentBond.Stereo == Globals.BondStereo.Indeterminate)
                    {
                        bondGeometry = BondGeometry.CrossedDoubleGeometry(
                            startPoint, endPoint, bondLength, ref _enclosingPoly, startAtomGeometry, endAtomGeometry);
                        using (DrawingContext dc = RenderOpen())
                        {
                            dc.DrawGeometry(null, _mainBondPen, bondGeometry);
                            dc.Close();
                        }
                    }
                    else
                    {
                        // Handle Half bond 
                        centroid = ParentBond.PrimaryRing?.Centroid;
                        //grab the enclosing polygon as for a double ParentBond - this overcomes a hit testing bug
                        _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                                                                          ParentBond.Placement, centroid, out point1,
                                                                          out point2, out point3, out point4);

                        if (startAtomGeometry != null)
                        {
                            BondGeometry.AdjustTerminus(ref point1, point2, startAtomGeometry);
                            BondGeometry.AdjustTerminus(ref point3, point4, startAtomGeometry);
                            _enclosingPoly = new List<Point> {point1, point2, point4, point3};
                        }

                        if (endAtomGeometry != null)
                        {
                            BondGeometry.AdjustTerminus(ref point4, point3, endAtomGeometry);
                            BondGeometry.AdjustTerminus(ref point2, point1, endAtomGeometry);
                            _enclosingPoly = new List<Point> {point1, point2, point4, point3};
                        }

                        using (DrawingContext dc = RenderOpen())
                        {
                            dc.DrawLine(_mainBondPen, point1, point2);
                            dc.DrawLine(_subsidiaryBondPen, point3, point4);

                            dc.Close();
                        }
                    }

                    break;

                case Globals.OrderPartial23:
                   

                case "3":
                case Globals.OrderTriple:
                    // Handle 2.5 bond 
                    if (ParentBond.Order == Globals.OrderPartial23)
                    {
                        _subsidiaryBondPen.DashStyle = DashStyles.Dash;
                    }
                    _enclosingPoly = BondGeometry.GetTripleBondPoints(ref startPoint, ref endPoint, bondLength,
                                                                      startAtomGeometry, endAtomGeometry, out point1,
                                                                      out point2, out point3, out point4);
                
                    using (DrawingContext dc = RenderOpen())
                    {
                        if (ParentBond.Placement == Globals.BondDirection.Clockwise)
                        {
                            dc.DrawLine(_mainBondPen, point1, point2);
                            dc.DrawLine(_mainBondPen, startPoint, endPoint);
                            dc.DrawLine(_subsidiaryBondPen, point3, point4);
                        }
                        else
                        {
                            dc.DrawLine(_subsidiaryBondPen, point1, point2);
                            dc.DrawLine(_mainBondPen, startPoint, endPoint);
                            dc.DrawLine(_mainBondPen, point3, point4);
                        }

                        dc.Close();
                    }

                    break;

                default:
                    if (startAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref startPoint, endPoint, startAtomGeometry);
                    }
                    if (endAtomGeometry != null)
                    {
                        BondGeometry.AdjustTerminus(ref endPoint, startPoint, endAtomGeometry);
                    }

                    using (DrawingContext dc = RenderOpen())
                    {
                        dc.DrawLine(_mainBondPen, startPoint, endPoint);
                        //we need to draw another transparent thicker line on top of the existing one
                        dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, BondThickness * 4),
                                        _bondGeometry);
                        dc.Close();
                    }

                    //grab the enclosing polygon as for a double ParentBond - this overcomes a hit testing bug
                    _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                                                                      ParentBond.Placement, null, out _,
                                                                      out _, out _, out _);
                    break;
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (HullGeometry != null) //not single bond
            {
                if (HullGeometry.FillContains(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }
            else
            {
                var widepen = new Pen(Brushes.Black, BondThickness * 8.0);
                if (_bondGeometry.StrokeContains(widepen, hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }
    }
}