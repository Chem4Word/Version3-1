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
                            result.GetWidenedPathGeometry(new Pen(Brushes.Black, BondThickness)));
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

        public static bool GetBondGeometry(Point startPoint, Point endPoint, Geometry startAtomGeometry, Geometry endAtomGeometry,
            double modelXamlBondLength, out Geometry singleBondGeometry, Bond parentBond, out List<Point> enclosingPoly, bool ignoreCentroid = false)
        {
            enclosingPoly = null;
            //check to see if it's a wedge or a hatch yet
            if (parentBond.Stereo == Globals.BondStereo.Wedge | parentBond.Stereo == Globals.BondStereo.Hatch)
            {
                {
                    singleBondGeometry = BondGeometry.WedgeBondGeometry(startPoint, endPoint, modelXamlBondLength,
                        startAtomGeometry, endAtomGeometry);
                    return true;
                }
            }

            if (parentBond.Stereo == Globals.BondStereo.Indeterminate && parentBond.OrderValue == 1.0)
            {
                {
                    singleBondGeometry = BondGeometry.WavyBondGeometry(startPoint, endPoint, modelXamlBondLength,
                        startAtomGeometry, endAtomGeometry);
                    return true;
                }
            }

            //single or dotted bond
            if (parentBond.OrderValue <= 1)
            {
                {
                    singleBondGeometry =
                        BondGeometry.SingleBondGeometry(startPoint, endPoint, startAtomGeometry, endAtomGeometry);
                    return true;
                }
            }

            if (parentBond.OrderValue == 1.5)
            {
                //it's a resonance bond, so we deal with this in Render
                //as we can't return a single geometry that can be
                //stroked with two different brushes
                //return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                {
                    singleBondGeometry = new StreamGeometry();
                    return true;
                }
            }

            //double bond
            if (parentBond.OrderValue == 2)
            {
                if (parentBond.Stereo == Globals.BondStereo.Indeterminate)
                {
                    {
                        singleBondGeometry = BondGeometry.CrossedDoubleGeometry(startPoint, endPoint, modelXamlBondLength,
                            ref enclosingPoly, startAtomGeometry, endAtomGeometry);
                        return true;
                    }
                }

                Point? centroid = null;
                if (parentBond.IsCyclic())
                {
                    centroid = parentBond.PrimaryRing?.Centroid;
                }

                {
                    if (!ignoreCentroid)
                    {
                        singleBondGeometry = BondGeometry.DoubleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                          parentBond.Placement,
                          ref enclosingPoly, centroid,  parentBond.SubsidiaryRing?.Centroid, startAtomGeometry, endAtomGeometry);
                        return true;
                    }
                    else
                    {
                        singleBondGeometry = BondGeometry.DoubleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                                                                             parentBond.Placement,
                                                                             ref enclosingPoly, null, null,  startAtomGeometry, endAtomGeometry);
                        return true;
                    }
                }
            }

            //tripe bond
            if (parentBond.OrderValue == 3)
            {
                {
                    singleBondGeometry = BondGeometry.TripleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                        ref enclosingPoly, startAtomGeometry, endAtomGeometry);
                    return true;
                }
            }

            singleBondGeometry = null;
            return false;
        }

        private Brush GetHatchBrush()
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
            var bondLength = ParentBond.Model.XamlBondLength;
            var cv1 = ChemicalVisuals.ContainsKey(ParentBond.StartAtom);
            var cv2 = ChemicalVisuals.ContainsKey(ParentBond.EndAtom);
            if (!cv1 || !cv2)
            {
                // Hack: Abort if either ChemicalVisual is missing !
                return;
            }
            Geometry startAtomGeometry;
            Geometry endAtomGeometry;

            startAtomGeometry = ((AtomVisual)ChemicalVisuals[ParentBond.StartAtom]).WidenedHullGeometry;

            endAtomGeometry = ((AtomVisual)ChemicalVisuals[ParentBond.EndAtom]).WidenedHullGeometry;

            _bondGeometry = bondGeometry = GetBondGeometry(startPoint, endPoint, startAtomGeometry, endAtomGeometry);

            _mainBondPen = new Pen(Brushes.Black, BondThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            _subsidiaryBondPen = _mainBondPen.Clone();

            if (ParentBond.OrderValue < 1.0d)
            {
                _mainBondPen.DashStyle = DashStyles.Dash;
                Point? centroid = null;
                //grab the enclosing polygon as for a double bond - this overcomes a hit testing bug
                _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                                                                  ParentBond.Placement, centroid, out _,
                                                                  out _, out _, out _);
            }
            else if (ParentBond.OrderValue < 2.0)
            {
                Point? centroid = ParentBond.PrimaryRing?.Centroid;
                //grab the enclosing polygon as for a double bond - this overcomes a hit testing bug
                _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                                                                  ParentBond.Placement, centroid, out _,
                                                                  out _, out _, out _);
                _subsidiaryBondPen.DashStyle = DashStyles.Dash;
            }

            if (ParentBond.Stereo == Globals.BondStereo.Indeterminate && ParentBond.OrderValue == 1.0)
            {
                //it's a wavy bond
            }

            if (ParentBond.OrderValue != 1.5)
            {
                using (DrawingContext dc = RenderOpen())
                {
                    Brush bondBrush = Brushes.Black;
                    if (ParentBond.Stereo == Globals.BondStereo.Hatch || ParentBond.Stereo == Globals.BondStereo.Wedge)
                    {
                        _mainBondPen.Thickness = 0; //don't draw around the bonds
                        if (ParentBond.Stereo == Globals.BondStereo.Hatch)
                        {
                            bondBrush = GetHatchBrush();
                        }
                    }
                    else
                    {
                        bondBrush = new SolidColorBrush(Colors.Black);
                    }
                    dc.DrawGeometry(bondBrush, _mainBondPen, bondGeometry);
                    if (ParentBond.OrderValue <= 1.0)
                    {
                        //we need to draw another transparent thicker line on top of the existing one
                        dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, BondThickness * 4), _bondGeometry);
                    }

                    dc.Close();
                }
            }
            else
            {
                Point point1, point2, point3, point4;

                Point? centroid = ParentBond.PrimaryRing?.Centroid;

                _enclosingPoly = BondGeometry.GetDoubleBondPoints(startPoint, endPoint, bondLength,
                    ParentBond.Placement, centroid, out point1,
                    out point2, out point3, out point4);
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