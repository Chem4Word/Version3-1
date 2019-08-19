// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;

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
        public BondDescriptor BondDescriptor { get; private set; }

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

        /// <summary>
        /// Returns a BondDescriptor object describing the visual layout of the visual
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="startAtomVisual"></param>
        /// <param name="endAtomVisual"></param>
        /// <param name="modelXamlBondLength"></param>
        /// <param name="ignoreCentroid"></param>
        /// <returns></returns>
        public static BondDescriptor GetBondDescriptor(Bond parent, AtomVisual startAtomVisual, AtomVisual endAtomVisual, double modelXamlBondLength, bool ignoreCentroid = false)
        {
            //check to see if it's a wedge or a hatch yet
            var startAtomPosition = parent.StartAtom.Position;
            var endAtomPosition = parent.EndAtom.Position;

            Point? centroid = null;
            Point? secondaryCentroid = null;
            if (!ignoreCentroid)
            {
                if (parent.IsCyclic())
                {
                    centroid = parent.PrimaryRing?.Centroid;
                    secondaryCentroid = parent.SubsidiaryRing?.Centroid;
                }
                else
                {
                    centroid = parent.Centroid;
                    secondaryCentroid = null;
                }
            }

            //do the straightforward cases first -discriminate by stereo
            var parentStereo = parent.Stereo;
            var parentOrderValue = parent.OrderValue;
            var parentPlacement = parent.Placement;

            return GetBondDescriptor(startAtomVisual, endAtomVisual, modelXamlBondLength, parentStereo, startAtomPosition, endAtomPosition, parentOrderValue, parentPlacement, centroid, secondaryCentroid);
        }

        public static BondDescriptor GetBondDescriptor(AtomVisual startAtomVisual, AtomVisual endAtomVisual,
                                                        double modelXamlBondLength, Globals.BondStereo parentStereo,
                                                        Point startAtomPosition, Point endAtomPosition,
                                                        double? parentOrderValue, Globals.BondDirection parentPlacement,
                                                        Point? centroid, Point? secondaryCentroid)
        {
            if (parentStereo == Globals.BondStereo.Wedge | parentStereo == Globals.BondStereo.Hatch)
            {
                WedgeBondDescriptor wbd = new WedgeBondDescriptor()
                {
                    Start = startAtomPosition,
                    End = endAtomPosition,
                    StartAtomVisual = startAtomVisual,
                    EndAtomVisual = endAtomVisual
                };

                var endAtom = endAtomVisual.ParentAtom;
                var otherBonds = endAtom.Bonds.Except(new[] { startAtomVisual.ParentAtom.BondBetween(endAtom) });
                Bond bond = null;
                if (otherBonds.Any())
                {
                    bond = otherBonds.ToArray()[0];
                }

                bool chamferBond = (otherBonds.Any() &&
                                    (endAtom.Element as Element) == Globals.PeriodicTable.C
                                    && endAtom.SymbolText == ""
                                    && bond.Order == Globals.OrderSingle);
                if (!chamferBond)
                {
                    wbd.CappedOff = false;
                    BondGeometry.GetWedgeBondGeometry(wbd, modelXamlBondLength);
                }
                else
                {
                    var nonHPs = from b in otherBonds
                                 where (b.OtherAtom(endAtom)).Element as Element != Globals.PeriodicTable.H
                                 select b.OtherAtom(endAtom).Position;

                    wbd.CappedOff = true;
                    BondGeometry.GetChamferedWedgeGeometry(wbd, modelXamlBondLength, nonHPs.ToList());
                }

                return wbd;
            }

            //wavy bond
            if (parentStereo == Globals.BondStereo.Indeterminate && parentOrderValue == 1.0)
            {
                BondDescriptor sbd = new BondDescriptor
                {
                    Start = startAtomPosition,
                    End = endAtomPosition,
                    StartAtomVisual = startAtomVisual,
                    EndAtomVisual = endAtomVisual
                };
                BondGeometry.GetWavyBondGeometry(sbd, modelXamlBondLength);
                return sbd;
            }

            switch (parentOrderValue)
            {
                //indeterminate double
                case 2 when parentStereo == Globals.BondStereo.Indeterminate:
                    DoubleBondDescriptor dbd = new DoubleBondDescriptor()
                    {
                        StartAtomVisual = startAtomVisual,
                        EndAtomVisual = endAtomVisual,
                        Start = startAtomPosition,
                        End = endAtomPosition
                    };
                    BondGeometry.GetCrossedDoubleGeometry(dbd, modelXamlBondLength);
                    return dbd;

                //partial or undefined bonds
                case 0:
                case 0.5:
                case 1.0:
                    BondDescriptor sbd = new BondDescriptor
                    {
                        Start = startAtomPosition,
                        End = endAtomPosition,
                        StartAtomVisual = startAtomVisual,
                        EndAtomVisual = endAtomVisual
                    };

                    BondGeometry.GetSingleBondGeometry(sbd);
                    return sbd;

                //double bond & 1.5 bond
                case 1.5:
                case 2:
                    DoubleBondDescriptor dbd2 = new DoubleBondDescriptor()
                    {
                        StartAtomVisual = startAtomVisual,
                        EndAtomVisual = endAtomVisual,
                        Start = startAtomPosition,
                        End = endAtomPosition,
                        Placement = parentPlacement,
                        PrimaryCentroid = centroid,
                        SecondaryCentroid = secondaryCentroid
                    };

                    BondGeometry.GetDoubleBondGeometry(dbd2, modelXamlBondLength);
                    return dbd2;

                //triple and 2.5 bond
                case 2.5:
                case 3:
                    TripleBondDescriptor tbd = new TripleBondDescriptor()
                    {
                        StartAtomVisual = startAtomVisual,
                        EndAtomVisual = endAtomVisual,
                        Start = startAtomPosition,
                        End = endAtomPosition,
                        Placement = parentPlacement,
                        PrimaryCentroid = centroid,
                        SecondaryCentroid = secondaryCentroid
                    };
                    BondGeometry.GetTripleBondGeometry(tbd, modelXamlBondLength);
                    return tbd;

                default:
                    return null;
            }
        }

        public static Brush GetHatchBrush(double angle)
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
                    Angle = angle
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

            startPoint = ParentBond.StartAtom.Position;
            endPoint = ParentBond.EndAtom.Position;

            var bondLength = ParentBond.Model.XamlBondLength;
            var cv1 = ChemicalVisuals.ContainsKey(ParentBond.StartAtom);
            var cv2 = ChemicalVisuals.ContainsKey(ParentBond.EndAtom);

            //bale out in case we have a null start or end
            if (!cv1 || !cv2)
            {
                // Abort if either ChemicalVisual is missing !
                return;
            }

            //now get the geometry of start and end atoms
            var startVisual = (AtomVisual)ChemicalVisuals[ParentBond.StartAtom];

            var endVisual = (AtomVisual)ChemicalVisuals[ParentBond.EndAtom];

            //first grab the main descriptor
            BondDescriptor = GetBondDescriptor(ParentBond, startVisual, endVisual, bondLength);

            _enclosingPoly = BondDescriptor.Boundary;
            //set up the default pens for rendering
            _mainBondPen = new Pen(Brushes.Black, BondThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Miter
            };

            _subsidiaryBondPen = _mainBondPen.Clone();

            switch (ParentBond.Order)
            {
                case Globals.OrderZero:
                case Globals.OrderOther:
                case "unknown":
                    // Handle Zero Bond
                    _mainBondPen.DashStyle = DashStyles.Dot;

                    using (DrawingContext dc = RenderOpen())
                    {
                        dc.DrawGeometry(Brushes.Black, _mainBondPen, BondDescriptor.DefiningGeometry);
                        //we need to draw another transparent thicker line on top of the existing one
                        dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, BondThickness * 4),
                                        BondDescriptor.DefiningGeometry);
                        dc.Close();
                    }
                    DoubleBondDescriptor dbd = new DoubleBondDescriptor
                    {
                        Start = startPoint,
                        End = endPoint,
                        Placement = ParentBond.Placement
                    };

                    BondGeometry.GetDoubleBondPoints(dbd, bondLength);
                    _enclosingPoly = dbd.Boundary;
                    break;

                case Globals.OrderPartial01:
                    _mainBondPen.DashStyle = DashStyles.Dash;

                    using (DrawingContext dc = RenderOpen())
                    {
                        dc.DrawGeometry(Brushes.Black, _mainBondPen, BondDescriptor.DefiningGeometry);
                        //we need to draw another transparent thicker line on top of the existing one
                        dc.DrawGeometry(Brushes.Transparent, new Pen(Brushes.Transparent, BondThickness * 4),
                                        BondDescriptor.DefiningGeometry);
                        dc.Close();
                    }

                    //grab the enclosing polygon as for a double ParentBond - this overcomes a hit testing bug
                    DoubleBondDescriptor dbd2 = new DoubleBondDescriptor
                    {
                        Start = startPoint,
                        End = endPoint,
                        Placement = ParentBond.Placement
                    };

                    BondGeometry.GetDoubleBondPoints(dbd2, bondLength);
                    _enclosingPoly = dbd2.Boundary;

                    break;

                case "1":
                case Globals.OrderSingle:
                    // Handle Single bond
                    switch (ParentBond.Stereo)
                    {
                        case Globals.BondStereo.Indeterminate:
                        case Globals.BondStereo.None:
                        case Globals.BondStereo.Wedge:
                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawGeometry(Brushes.Black, _mainBondPen, BondDescriptor.DefiningGeometry);

                                dc.Close();
                            }
                            break;

                        case Globals.BondStereo.Hatch:
                            using (DrawingContext dc = RenderOpen())
                            {
                                dc.DrawGeometry(GetHatchBrush(ParentBond.Angle), _mainBondPen, BondDescriptor.DefiningGeometry);

                                dc.Close();
                            }
                            break;
                    }
                    break;

                case Globals.OrderPartial12:
                case Globals.OrderAromatic:
                case "2":
                case Globals.OrderDouble:
                    // Handle 1.5 bond
                    DoubleBondDescriptor dbd3 = (DoubleBondDescriptor)BondDescriptor;
                    Point? centroid = ParentBond.Centroid;
                    dbd3.PrimaryCentroid = centroid;

                    if (ParentBond.Order == Globals.OrderPartial12 | ParentBond.Order == Globals.OrderAromatic)
                    {
                        _subsidiaryBondPen.DashStyle = DashStyles.Dash;
                    }

                    _enclosingPoly = dbd3.Boundary;

                    if (ParentBond.Stereo != Globals.BondStereo.Indeterminate)
                    {
                        using (DrawingContext dc = RenderOpen())
                        {
                            dc.DrawLine(_mainBondPen, BondDescriptor.Start, BondDescriptor.End);
                            dc.DrawLine(_subsidiaryBondPen,
                                        dbd3.SecondaryStart,
                                        dbd3.SecondaryEnd);
                            dc.Close();
                        }
                    }
                    else
                    {
                        using (DrawingContext dc = RenderOpen())
                        {
                            dc.DrawGeometry(_mainBondPen.Brush, _mainBondPen, BondDescriptor.DefiningGeometry);

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

                    var tbd = (BondDescriptor as TripleBondDescriptor);
                    using (DrawingContext dc = RenderOpen())
                    {
                        if (ParentBond.Placement == Globals.BondDirection.Clockwise)
                        {
                            dc.DrawLine(_mainBondPen, tbd.SecondaryStart, tbd.SecondaryEnd);
                            dc.DrawLine(_mainBondPen, tbd.Start, tbd.End);
                            dc.DrawLine(_subsidiaryBondPen, tbd.TertiaryStart, tbd.TertiaryEnd);
                        }
                        else
                        {
                            dc.DrawLine(_subsidiaryBondPen, tbd.SecondaryStart, tbd.SecondaryEnd);
                            dc.DrawLine(_mainBondPen, tbd.Start, tbd.End);
                            dc.DrawLine(_mainBondPen, tbd.TertiaryStart, tbd.TertiaryEnd);
                        }

                        dc.Close();
                    }
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
                if (BondDescriptor.DefiningGeometry.StrokeContains(widepen, hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }
    }
}