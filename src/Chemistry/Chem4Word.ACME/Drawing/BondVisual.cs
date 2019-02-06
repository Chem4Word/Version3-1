// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------


using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model.Geometry;
//using Chem4Word.ViewModel;


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

            //check to see if it's a wedge or a hatch yet
            if (ParentBond.Stereo == Globals.BondStereo.Wedge | ParentBond.Stereo == Globals.BondStereo.Hatch)
            {
                return BondGeometry.WedgeBondGeometry(startPoint, endPoint, modelXamlBondLength, startAtomGeometry, endAtomGeometry);
            }

            if (ParentBond.Stereo == Globals.BondStereo.Indeterminate && ParentBond.OrderValue == 1.0)
            {
                return BondGeometry.WavyBondGeometry(startPoint, endPoint, modelXamlBondLength, startAtomGeometry, endAtomGeometry);
            }

            //single or dotted bond
            if (ParentBond.OrderValue <= 1)
            {
                return BondGeometry.SingleBondGeometry(startPoint, endPoint, startAtomGeometry, endAtomGeometry);
            }

            if (ParentBond.OrderValue == 1.5)
            {
                //it's a resonance bond, so we deal with this in Render
                //as we can't return a single geometry that can be
                //stroked with two different brushes
                //return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                return new StreamGeometry();
            }

            //double bond
            if (ParentBond.OrderValue == 2)
            {
                if (ParentBond.Stereo == Globals.BondStereo.Indeterminate)
                {
                    return BondGeometry.CrossedDoubleGeometry(startPoint, endPoint, modelXamlBondLength,
                        ref _enclosingPoly, startAtomGeometry, endAtomGeometry);
                }

                Point? centroid = null;
                if (ParentBond.IsCyclic())
                {
                    centroid = ParentBond.PrimaryRing?.Centroid;
                }

                return BondGeometry.DoubleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                    ParentBond.Placement,
                    ref _enclosingPoly, centroid, startAtomGeometry, endAtomGeometry);
            }

            //tripe bond
            if (ParentBond.OrderValue == 3)
            {
                return BondGeometry.TripleBondGeometry(startPoint, endPoint, modelXamlBondLength,
                    ref _enclosingPoly, startAtomGeometry, endAtomGeometry);
            }

            return null;
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
            var startAtomGeometry = ((AtomVisual)ChemicalVisuals[ParentBond.StartAtom]).WidenedHullGeometry;
            var endAtomGeometry = ((AtomVisual)ChemicalVisuals[ParentBond.EndAtom]).WidenedHullGeometry;

            bondGeometry = GetBondGeometry(startPoint, endPoint, startAtomGeometry, endAtomGeometry);

            _mainBondPen = new Pen(Brushes.Black, BondThickness);
            _mainBondPen.Thickness = BondThickness;

            _mainBondPen.StartLineCap = PenLineCap.Round;
            _mainBondPen.EndLineCap = PenLineCap.Round;
            _subsidiaryBondPen = _mainBondPen.Clone();

            if (ParentBond.OrderValue < 1.0d)
            {
                _mainBondPen.DashStyle = DashStyles.Dash;
            }
            else if (ParentBond.OrderValue < 2.0)
            {
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
                    dc.Close();
                }
            }
            else
            {
                Point point1, point2, point3, point4;

                Point? centroid = null;
                if (ParentBond.IsCyclic())
                {
                    centroid = ParentBond.PrimaryRing?.Centroid;
                }

                var bondLength = ParentBond.Model.XamlBondLength;
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
    }
}