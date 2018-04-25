// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using static Chem4Word.View.GlyphUtils;

namespace Chem4Word.View
{
    /// <summary>
    /// Replcacement for existing AtomShape.  uses glyph based rendering
    /// </summary>
    public class AtomShape : Shape
    {
        private static double MaskOffsetWidth = 0;
        public static double SymbolSize = 0;
        public static double ScriptSize = 0;
        public static double IsotopeSize = 0;

        //list of points that make up the hull of the shape

        #region Members

        private List<Point> _shapeHull;

        #endregion Members

        #region constructors

        //needs a default constructor to be used in XAML

        #endregion constructors

        /// <summary>
        /// Defines a subscripted group of atoms eg H_3
        /// </summary>
        ///
        private class SubscriptedGroup
        {
            //how many atoms in the group
            public int Count { get; }

            //the group text
            public string Text { get; }

            //holds the text of the atoms
            private GlyphText _mainText;

            //holds the text of the subscript
            private SubLabelText _subText;

            private static double FontSize;

            public SubscriptedGroup(int count, string text, double fontSize)
            {
                Count = count;
                Text = text;
                FontSize = fontSize;
            }

            /// <summary>
            /// Measures the dimensions of the atom prior to rendering
            /// </summary>
            /// <param name="parentMetrics">Metrics of the parent atom</param>
            /// <param name="direction">Orientation of the group relative to the parent atom, i.e. NESW</param>
            /// <returns>AtomTextMetrics object describing placement</returns>
            public AtomTextMetrics Measure(AtomTextMetrics parentMetrics, CompassPoints direction, float pixelsPerDip)
            {
                _subText = null;

                List<Point> mainOutline;
                //first, get some initial size measurements
                _mainText = new GlyphText(Text, SymbolTypeface, FontSize, pixelsPerDip);
                _mainText.Premeasure();

                //measure up the subscript (if we have one)
                string subscriptText = AtomHelpers.GetSubText(Count);
                if (subscriptText != "")
                {
                    _subText = new SubLabelText(subscriptText, pixelsPerDip);
                    _subText.Premeasure();
                }

                //calculate the center of the H Atom depending on the direction
                var groupCenter = GetAdjunctCenter(parentMetrics, direction, _mainText.GlyphInfo, _subText?.GlyphInfo);
                //remeasure the main text
                _mainText.MeasureAtCenter(groupCenter);

                mainOutline = _mainText.FlattenedPath;

                if (_subText != null)
                //get the offset for the subscript

                {
                    Vector subscriptOffset = new Vector(_mainText.TextMetrics.TotalBoundingBox.Width + _mainText.TrailingBearing + _subText.LeadingBearing,
                        _subText.TextMetrics.BoundingBox.Height / 2);
                    Point subBottomLeft = _mainText.TextMetrics.TotalBoundingBox.BottomLeft + subscriptOffset;
                    _subText.MeasureAtBottomLeft(subBottomLeft, pixelsPerDip);
                    //merge the total bounding boxes
                    _mainText.Union(_subText);
                    mainOutline.AddRange(_subText.FlattenedPath);
                }
                //return the placement metrics for the subscripted atom.
                AtomTextMetrics result = new AtomTextMetrics
                {
                    Geocenter = groupCenter,
                    BoundingBox = _mainText.TextMetrics.BoundingBox,
                    TotalBoundingBox = _mainText.TextMetrics.TotalBoundingBox,
                    FlattenedPath = mainOutline
                };

                return result;
            }

            /// <summary>
            /// Draws the subscripted group text
            /// </summary>
            /// <param name="drawingContext">DC supplied by OnRender</param>
            /// <param name="measure">Provided by calling the Measure method previously</param>
            /// <param name="pixelsPerDip"></param>
            /// <param name="fill"></param>
            public void DrawSelf(DrawingContext drawingContext, AtomTextMetrics measure, float pixelsPerDip, Brush fill)
            {
                _mainText.Fill = fill;
                _mainText.DrawAtBottomLeft(measure.BoundingBox.BottomLeft, drawingContext);
                if (_subText != null)
                {
                    _subText.Fill = fill;
                    _subText.DrawAtBottomLeft(_subText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
                }
            }

            /// <summary>
            /// Gets the centerpoint of an atom adjunct (like an implicit hydrogen plus subscripts)
            /// The Adjunct in NH2 is H2
            /// </summary>
            /// <param name="parentMetrics">Metrics of the parent atom</param>
            /// <param name="direction">NESW direction of the adjunct respective to the atom</param>
            /// <param name="adjunctGlyphInfo">Intial measurements of the adjunct</param>
            /// <param name="subscriptInfo">Initial measurements of the subscript (can be null for no subscripts)</param>
            /// <returns></returns>
            private static Point GetAdjunctCenter(AtomTextMetrics parentMetrics, CompassPoints direction,
                GlyphInfo adjunctGlyphInfo, GlyphInfo? subscriptInfo = null)
            {
                Point adjunctCenter;
                double charHeight = (GlyphUtils.GlyphTypeface.Baseline * FontSize);
                double adjunctWidth = (parentMetrics.BoundingBox.Width + adjunctGlyphInfo.Width) / 2;
                switch (direction)
                {
                    //all addition in this routine is *vector* addition.
                    //We are not adding absolute X and Y values
                    case CompassPoints.East:
                    default:
                        adjunctCenter = parentMetrics.Geocenter + BasicGeometry.ScreenEast * adjunctWidth;
                        break;

                    case CompassPoints.North:
                        adjunctCenter = parentMetrics.Geocenter +
                                        BasicGeometry.ScreenNorth * charHeight;
                        break;

                    case CompassPoints.West:

                        if (subscriptInfo != null)
                        {
                            adjunctCenter = parentMetrics.Geocenter + (BasicGeometry.ScreenWest *
                                                                       (adjunctWidth + subscriptInfo.Value.Width));
                        }
                        else
                        {
                            adjunctCenter = parentMetrics.Geocenter + (BasicGeometry.ScreenWest * (adjunctWidth));
                        }
                        break;

                    case CompassPoints.South:
                        adjunctCenter = parentMetrics.Geocenter +
                                        BasicGeometry.ScreenSouth * charHeight;
                        break;
                }
                return adjunctCenter;
            }
        }

        #region Overrides

        protected override void OnRender(DrawingContext drawingContext)
        {
            double average = Model.Globals.EstimatedAverageBondSize;
            if (ParentAtom.Parent.Bonds.Any())
            {
                average = ParentAtom.Parent.MeanBondLength;
            }
            else
            {
                average = ParentAtom.Parent.SingleAtomAssumedBondLength;
            }

            SymbolSize = average / 2.0d;

            ScriptSize = SymbolSize * 0.6;
            IsotopeSize = SymbolSize * 0.8;
            MaskOffsetWidth = SymbolSize * 0.1;

            RenderAtom(drawingContext);
        }

        #endregion Overrides

        #region Methods

        /// <summary>
        /// used for device independent rendering
        /// </summary>
        /// <returns></returns>
        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }

        /// <summary>
        /// Rebders an atom plus charges and labels to the drawing context
        /// </summary>
        /// <param name="drawingContext"></param>
        private void RenderAtom(DrawingContext drawingContext)
        {
            // ToDo: Fix This
            //renders the atom complete with charges, hydrogens and labels.
            //this code is *complex*

            //private variables used to keep track of onscreen visuals
            AtomTextMetrics hydrogenMetrics = null;
            LabelMetrics isoMetrics = null;
            SubscriptedGroup subscriptedGroup = null;
            _shapeHull = new List<Point>();

            //stage 1:  measure up the main atom symbol in position
            //we need the metrics first
            if (AtomSymbol != "")
            {
                var symbolText = new GlyphText(AtomSymbol,
                    SymbolTypeface, SymbolSize, PixelsPerDip());
                symbolText.MeasureAtCenter(Position);
                //grab the hull for later
                if (symbolText.FlattenedPath != null)
                {
                    _shapeHull.AddRange(symbolText.FlattenedPath);
                }
            }

            //stage 2.  grab the main atom metrics br drawing it
            //need to draw the atom twice as it will be obscured by the mask
            var mainAtomMetrics = DrawSelf(drawingContext, true);

            //stage 3:  measure up the hydrogens
            //if we have implicit hydrogens and we have an explicit label, draw them
            if (ImplicitHydrogenCount > 0 && AtomSymbol != "")
            {
                var defaultHOrientation = ParentAtom.GetDefaultHOrientation();

                subscriptedGroup = new SubscriptedGroup(ImplicitHydrogenCount, "H", SymbolSize);
                hydrogenMetrics = subscriptedGroup.Measure(mainAtomMetrics, defaultHOrientation, PixelsPerDip());

                //subscriptedGroup.DrawSelf(drawingContext,hydrogenMetrics , PixelsPerDip(), Fill);
                _shapeHull.AddRange(hydrogenMetrics.FlattenedPath);
            }

            //stage 4: draw the background mask
            //recalculate the hull, which is a hotchpotch of points by now
            if (_shapeHull.Any())
            {
                //sort the points properly before doing a hull calculation
                var sortedHull = (from Point p in _shapeHull
                                  orderby p.X, p.Y descending
                                  select p).ToList();

                _shapeHull = Geometry<Point>.GetHull(sortedHull, p => p);

                DrawMask(_shapeHull, drawingContext);
            }

            //then do the drawing of the main symbol (again)
            mainAtomMetrics = DrawSelf(drawingContext);
            //stage 5:  draw the hydrogens
            if (ImplicitHydrogenCount > 0 && AtomSymbol != "")
            {
                subscriptedGroup.DrawSelf(drawingContext, hydrogenMetrics, PixelsPerDip(), Fill);
            }
            //stage 6:  draw an isotope label if needed
            if (Isotope != null)
            {
                isoMetrics = DrawIsotopeLabel(drawingContext, mainAtomMetrics, hydrogenMetrics);
            }
            //stage7:  draw any charges
            if ((Charge ?? 0) != 0)
            {
                LabelMetrics cMetrics = DrawCharges(drawingContext, mainAtomMetrics, hydrogenMetrics, isoMetrics, ParentAtom.GetDefaultHOrientation());
            }
        }

        /// <summary>
        /// Draws a background mask for the atom symbol
        /// </summary>
        /// <param name="shapeHull"></param>
        /// <param name="drawingContext"></param>
        private void DrawMask(List<Point> shapeHull, DrawingContext drawingContext)
        {
            if (BackgroundColor == null)
            {
                BackgroundColor = SystemColors.WindowBrush;
            }

            drawingContext.DrawGeometry(BackgroundColor,
                new Pen(BackgroundColor, MaskOffsetWidth),
                BasicGeometry.BuildPath(shapeHull).Data);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="mainAtomMetrics"></param>
        /// <param name="hMetrics"></param>
        /// <param name="isoMetrics"></param>
        /// <param name="defaultHOrientation"></param>
        /// <returns></returns>
        private LabelMetrics DrawCharges(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics, CompassPoints defaultHOrientation)
        {
            Debug.Assert((Charge ?? 0) != 0);
            var chargeString = AtomHelpers.GetChargeString(Charge);
            var chargeText = DrawChargeOrRadical(drawingContext, mainAtomMetrics, hMetrics, isoMetrics, chargeString, Fill, defaultHOrientation);
            return chargeText.TextMetrics;
        }

        /// <param name="drawingContext"></param>
        /// <param name="mainAtomMetrics">
        /// </param>
        /// <param name="hMetrics">
        /// </param>
        /// <param name="isoMetrics">
        /// </param>
        /// <param name="chargeString"></param>
        /// <param name="fill"></param>
        /// <param name="defaultHOrientation"></param>
        /// <returns></returns>
        /// <summary>
        /// Draws a charge or radical label at the given point
        /// </summary>
        /// <returns></returns>
        private ChargeLabelText DrawChargeOrRadical(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics, string chargeString, Brush fill, CompassPoints defaultHOrientation)
        {
            // ToDo: Fix This
            ChargeLabelText chargeText = new ChargeLabelText(chargeString, PixelsPerDip());

            //try to place the charge at 2 o clock to the atom
            Vector chargeOffset = BasicGeometry.ScreenNorth * SymbolSize * 0.9;
            RotateUntilClear(mainAtomMetrics, hMetrics, isoMetrics, chargeOffset, chargeText, out var chargeCenter, defaultHOrientation);
            chargeText.MeasureAtCenter(chargeCenter);
            chargeText.Fill = fill;
            chargeText.DrawAtBottomLeft(chargeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return chargeText;
        }

        private static void RotateUntilClear(AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics,
            Vector labelOffset, GlyphText labelText, out Point labelCenter, CompassPoints defHOrientation)
        {
            Matrix rotator = new Matrix();
            double angle = ClockDirections.Two.ToDegrees();
            rotator.Rotate(angle);

            labelOffset = labelOffset * rotator;
            Rect bb = new Rect();
            Rect bb2 = new Rect();
            if (hMetrics != null)
            {
                bb = hMetrics.TotalBoundingBox;
            }
            if (isoMetrics != null)
            {
                bb2 = isoMetrics.BoundingBox;
            }
            labelCenter = mainAtomMetrics.Geocenter + labelOffset;
            labelText.MeasureAtCenter(labelCenter);

            double increment;
            if (defHOrientation == CompassPoints.East)
            {
                increment = -10;
            }
            else
            {
                increment = 10;
            }
            while (labelText.CollidesWith(mainAtomMetrics.TotalBoundingBox, bb,
                bb2) & Math.Abs(angle - 30) > 0.001)
            {
                rotator = new Matrix();

                angle += increment;
                rotator.Rotate(increment);
                labelOffset = labelOffset * rotator;
                labelCenter = mainAtomMetrics.Geocenter + labelOffset;
                labelText.MeasureAtCenter(labelCenter);
            }
        }

        //draws the isotope label at ten-o-clock
        private LabelMetrics DrawIsotopeLabel(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics)
        {
            // ToDo: Fix This
            Debug.Assert(Isotope != null);

            string isoLabel = Isotope.ToString();
            var isotopeText = new IsotopeLabelText(isoLabel, PixelsPerDip());

            Vector isotopeOffsetVector = BasicGeometry.ScreenNorth * SymbolSize;
            Matrix rotator = new Matrix();
            rotator.Rotate(ClockDirections.Ten.ToDegrees());
            isotopeOffsetVector = isotopeOffsetVector * rotator;
            Point isoCenter = mainAtomMetrics.Geocenter + isotopeOffsetVector;
            isotopeText.MeasureAtCenter(isoCenter);
            isotopeText.DrawAtBottomLeft(isotopeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return isotopeText.TextMetrics;
        }

        //draws the main atom symbol, or an ellipse if necessary
        private AtomTextMetrics DrawSelf(DrawingContext drawingContext, bool measureOnly = false)
        {
            // ToDo: Fix This
            if (AtomSymbol == "") //implicit carbon
            {
                //so draw a circle
                double radiusX = Globals.AtomWidth / 2;
                if (!measureOnly)
                {
                    drawingContext.DrawEllipse(Fill, null, Position, radiusX, radiusX);
                }
                Rect boundingBox = new Rect(new Point(Position.X - radiusX, Position.Y - radiusX),
                    new Point(Position.X + radiusX, Position.Y + radiusX));
                return new AtomTextMetrics
                {
                    BoundingBox = boundingBox,
                    Geocenter = Position,
                    TotalBoundingBox = boundingBox,
                    FlattenedPath = new List<Point> { boundingBox.BottomLeft, boundingBox.TopLeft, boundingBox.TopRight, boundingBox.BottomRight }
                };
            }
            else
            {
                var symbolText = new GlyphText(AtomSymbol, SymbolTypeface, SymbolSize, PixelsPerDip());
                symbolText.Fill = Fill;
                symbolText.MeasureAtCenter(Position);
                if (!measureOnly)
                {
                    symbolText.DrawAtBottomLeft(symbolText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
                }
                return symbolText.TextMetrics;
            }
        }

        #endregion Methods

        #region Dependency Properties

        #region Positioning DPs

        public Point Position
        {
            get { return (Point)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(Point), typeof(AtomShape),
                new FrameworkPropertyMetadata(new Point(0, 0),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion Positioning DPs

        #region layout DPs

        // ToDo: Fix This
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        // ToDo: Fix This
        // Using a DependencyProperty as the backing store for FontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(double), typeof(AtomShape),
                new FrameworkPropertyMetadata(200d, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion layout DPs

        #region Atom DPs

        public string AtomSymbol
        {
            get { return (string)GetValue(AtomSymbolProperty); }
            set { SetValue(AtomSymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AtomSymbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AtomSymbolProperty =
            DependencyProperty.Register("AtomSymbol", typeof(string), typeof(AtomShape),
                new FrameworkPropertyMetadata("",
                    FrameworkPropertyMetadataOptions.AffectsArrange
                    | FrameworkPropertyMetadataOptions.AffectsMeasure
                    | FrameworkPropertyMetadataOptions.AffectsRender,
                    AtomSymbolChangedCallback));

        private static void AtomSymbolChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
        }

        public Atom ParentAtom
        {
            get { return (Atom)GetValue(ParentAtomProperty); }
            set { SetValue(ParentAtomProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParentAtom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentAtomProperty =
            DependencyProperty.Register("ParentAtom", typeof(Atom), typeof(AtomShape), new PropertyMetadata(null));

        #endregion Atom DPs

        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor",
                typeof(Brush),
                typeof(AtomShape),
                new FrameworkPropertyMetadata(SystemColors.WindowBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        #region Charge DP

        public int? Charge
        {
            get { return (int?)GetValue(ChargeProperty); }
            set { SetValue(ChargeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Charge.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChargeProperty =
            DependencyProperty.Register("Charge",
                typeof(int?),
                typeof(AtomShape),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion Charge DP

        protected override Geometry DefiningGeometry
        {
            get
            {
                //so draw a circle
                double radiusX = Globals.AtomWidth / 2;

                return new EllipseGeometry(Position, radiusX, radiusX);
            }
        }

        public int? Isotope
        {
            get { return (int?)GetValue(IsotopeProperty); }
            set { SetValue(IsotopeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Isotope.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsotopeProperty =
            DependencyProperty.Register("Isotope", typeof(int?), typeof(AtomShape),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, IsotopeChangedCallback));

        private static void IsotopeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var changedAtomShape = (AtomShape)d;
            var newval = (int?)args.NewValue;
        }

        public int ImplicitHydrogenCount
        {
            get { return (int)GetValue(ImplicitHydrogenCountProperty); }
            set { SetValue(ImplicitHydrogenCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImplicitHydrogenCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImplicitHydrogenCountProperty =
            DependencyProperty.Register("ImplicitHydrogenCount", typeof(int), typeof(AtomShape),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion Dependency Properties
    }
}