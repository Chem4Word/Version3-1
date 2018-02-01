// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.Model;
using Chem4Word.Model.Geometry;
using Chem4Word.View;
using Globals = Chem4Word.View.Globals;

namespace Chem4Word.ViewModel
{

    public class AtomShape2 : FrameworkElement
    {
        //private static FrameworkPropertyMetadataOptions _everyOption
        //    = FrameworkPropertyMetadataOptions.AffectsRender
        //    | FrameworkPropertyMetadataOptions.AffectsMeasure
        //    | FrameworkPropertyMetadataOptions.AffectsArrange
        //    | FrameworkPropertyMetadataOptions.AffectsParentMeasure
        //    | FrameworkPropertyMetadataOptions.AffectsParentArrange;


        private List<Point> _shapeHull;

        public AtomShape2()
        {

        }
        /// <summary>
        /// used for device indepndent rendering
        /// </summary>
        /// <returns></returns>
        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }

        #region nested classes
        /// <summary>
        /// Defines a subscripted group of atoms eg H_3
        /// </summary>
        /// 
        private class SubscriptedGroup
        {
            //how many atoms in the group
            public int Count { get; }
            //the group text
            public string Text { get;  }

            //holds the text of the atoms
            private GlyphText _mainText;
            //holds the text of the subscript
            private SubLabelText _subText;
            public SubscriptedGroup(int count, string text)
            {
                Count = count;
                Text = text;
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

                //first, get some initial size measurements
                _mainText = new GlyphText(Text, GlyphUtils.SymbolTypeface, GlyphUtils.SymbolSize, pixelsPerDip);
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
                //get the offset for the subscript
                Vector subscriptOffset = new Vector(_mainText.TextMetrics.TotalBoundingBox.Width,
                        GlyphUtils.GlyphTypeface.CapsHeight / 2);

                if (subscriptText != "")
                {
                    Point subBottomLeft = _mainText.TextMetrics.TotalBoundingBox.BottomLeft + subscriptOffset;
                    _subText.MeasureAtBottomLeft(subBottomLeft,pixelsPerDip);
                    //merge the total bounbding boxes
                    _mainText.Union(_subText);
                }
                //return the placement metrics for the subscripted atom.  
                AtomTextMetrics result = new AtomTextMetrics { Geocenter = groupCenter, BoundingBox = _mainText.TextMetrics.BoundingBox, TotalBoundingBox = _mainText.TextMetrics.TotalBoundingBox};

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
                _mainText.DrawAtBottomLeft(measure.TotalBoundingBox.BottomLeft, drawingContext);
                _subText?.DrawAtBottomLeft(_subText.TextMetrics.TotalBoundingBox.BottomLeft, drawingContext);

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
                GlyphUtils.GlyphInfo adjunctGlyphInfo, GlyphUtils.GlyphInfo? subscriptInfo=null)
            {
                Point adjunctCenter;
                double charHeight = (GlyphUtils.GlyphTypeface.Baseline * GlyphUtils.SymbolSize);
                double adjunctWidth = (parentMetrics.BoundingBox.Width + adjunctGlyphInfo.Width);
                switch (direction)
                {
                    case CompassPoints.East:
                    default:
                        adjunctCenter = parentMetrics.Geocenter + BasicGeometry.ScreenEast() *
                                         adjunctWidth / 2;
                        break;
                    case CompassPoints.North:
                        adjunctCenter = parentMetrics.Geocenter +
                                         BasicGeometry.ScreenNorth() * charHeight;
                        break;
                    case CompassPoints.West:

                        adjunctCenter = parentMetrics.Geocenter + BasicGeometry.ScreenWest() *
                                         (adjunctWidth / 2 + (subscriptInfo?.Width)??0);
                        break;
                    case CompassPoints.South:
                        adjunctCenter = parentMetrics.Geocenter +
                                         BasicGeometry.ScreenSouth() * charHeight;
                        break;
                }
                return adjunctCenter;
            }


           
        }

#endregion

        #region Overrides


        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            Pen widepen = new Pen(Brushes.Black, 3.0);   
            //first work out what the atom bounding box is:
            List<Point> hull = Geometry<Point>.GetHull(_shapeHull, p => p);
            Path pg = BasicGeometry.BuildPath(hull);
            if (pg.Data.FillContains(hitTestParameters.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
            else
            {
                return null;
            }
        }

      

       
  #endregion Overrides

        protected override void OnRender(DrawingContext drawingContext)
        {
            RenderAtom(drawingContext);
        }
        /// <summary>
        /// Rebders an atom plus charges and labels to the drawing context
        /// </summary>
        /// <param name="drawingContext"></param>
        private void RenderAtom(DrawingContext drawingContext)
        {
            
            AtomTextMetrics hydrogenMetrics = null;
            LabelMetrics isoMetrics = null;
            SubscriptedGroup subscriptedGroup =null;
            _shapeHull= new List<Point>();
            
            //we need the metrics first

            //first do all the measuring up
            if (AtomSymbol != "")
            {
                var symbolText = new GlyphText(AtomSymbol, GlyphUtils.SymbolTypeface, GlyphUtils.SymbolSize, PixelsPerDip());
                symbolText.MeasureAtCenter(Position);
                _shapeHull.AddRange(symbolText.Hull);
            }
            //need to draw the atom twice as it will be obscured by the mask 
            var mainAtomMetrics = DrawMainSymbol(drawingContext);
            //if we have implcit hydrogens and we have an explicit label, draw them
            if (ParentAtom.ImplicitHydrogenCount > 0 && AtomSymbol!="")
            {

                DefaultHOrientation = AtomHelpers.GetDefaultHOrientation(ParentAtom);

                subscriptedGroup = new SubscriptedGroup(ParentAtom.ImplicitHydrogenCount, "H");
                hydrogenMetrics = subscriptedGroup.Measure(mainAtomMetrics, DefaultHOrientation.Value, PixelsPerDip());

                //subscriptedGroup.DrawSelf(drawingContext,hydrogenMetrics , PixelsPerDip(), Fill);
                _shapeHull.AddRange(hydrogenMetrics.Corners);
            }
            
            //recalculate the hull
            _shapeHull = Geometry<Point>.GetHull(_shapeHull, p => p);
            DrawMask(_shapeHull, drawingContext);
            //then do the drawing

            mainAtomMetrics = DrawMainSymbol(drawingContext);

            if (ParentAtom.ImplicitHydrogenCount > 0 && AtomSymbol != "")
            {

                subscriptedGroup.DrawSelf(drawingContext, hydrogenMetrics, PixelsPerDip(), Fill);
            }

            if (ParentAtom.IsotopeNumber != null)
            {
                isoMetrics = DrawIsotopeLabel(drawingContext, mainAtomMetrics, hydrogenMetrics);
            }

            if ((ParentAtom.FormalCharge ?? 0) != 0)
            {
                LabelMetrics cMetrics = DrawCharges(drawingContext, mainAtomMetrics, hydrogenMetrics, isoMetrics);
            }
        }

        private void DrawMask(List<Point> shapeHull, DrawingContext drawingContext)
        {
            drawingContext.DrawGeometry(Brushes.White, new Pen(Brushes.White, GlyphUtils.SymbolSize / 4), BasicGeometry.BuildPath(_shapeHull).Data);
        }
        /// <summary>
        /// Draws the atomic charge if required
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="mainAtomMetrics"></param>
        /// <param name="hMetrics"></param>
        /// <param name="isoMetrics"></param>
        /// <returns></returns>
        private LabelMetrics DrawCharges(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics)
        {
            Debug.Assert((ParentAtom.FormalCharge??0)!=0);
            var chargeString = AtomHelpers.GetChargeString(ParentAtom.FormalCharge.Value);
            var chargeText = DrawChargeOrRadical(drawingContext, mainAtomMetrics, hMetrics, isoMetrics, chargeString);
            return chargeText.TextMetrics;

        }
        /// <summary>
        /// Draws a charge or radical label at the given point
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="mainAtomMetrics"></param>
        /// <param name="hMetrics"></param>
        /// <param name="isoMetrics"></param>
        /// <param name="chargeString"></param>
        /// <returns></returns>
        private ChargeLabelText DrawChargeOrRadical(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics,
            AtomTextMetrics hMetrics, LabelMetrics isoMetrics, string chargeString)
        {
            ChargeLabelText chargeText = new ChargeLabelText(chargeString, PixelsPerDip());

            //try to place the charge at 2 o clock to the atom
            Vector chargeOffset = BasicGeometry.ScreenNorth() * GlyphUtils.SymbolSize;
            RotateUntilClear(mainAtomMetrics, hMetrics, isoMetrics, chargeOffset, chargeText, out var chargeCenter);
            chargeText.DrawAtBottomLeft(chargeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return chargeText;
        }

        private static void RotateUntilClear(AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, LabelMetrics isoMetrics,
            Vector labelOffset, GlyphText labelText, out Point labelCenter)
        {
            Matrix rotator = new Matrix();
            double angle = 60;
            rotator.Rotate(angle);

            labelOffset = labelOffset * rotator;

            labelCenter = mainAtomMetrics.Geocenter + labelOffset;
            labelText.MeasureAtCenter(labelCenter);
            while (labelText.CollidesWith(mainAtomMetrics.TotalBoundingBox, hMetrics.TotalBoundingBox,
                isoMetrics.BoundingBox) & Math.Abs(angle - 30) > 0.001)
            {
                rotator.Rotate(30);
                angle += 30;
                labelOffset = labelOffset * rotator;
                labelCenter = mainAtomMetrics.Geocenter + labelOffset;
                labelText.MeasureAtCenter(labelCenter);
            }

            
            
        }

        private LabelMetrics DrawIsotopeLabel(DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics)
        {
            Debug.Assert(ParentAtom.IsotopeNumber!=null);

            string isoLabel = ParentAtom.IsotopeNumber.ToString();
            var isotopeText = new IsotopeLabelText(isoLabel, PixelsPerDip());

            Vector isotopeOffsetVector = BasicGeometry.ScreenNorth() * GlyphUtils.SymbolSize;
            Matrix rotator = new Matrix();
            rotator.Rotate(-60);
            isotopeOffsetVector = isotopeOffsetVector * rotator;
            Point isoCenter = mainAtomMetrics.Geocenter + isotopeOffsetVector;
            isotopeText.MeasureAtCenter(isoCenter);
            isotopeText.DrawAtBottomLeft(isotopeText.TextMetrics.BoundingBox.BottomLeft, drawingContext);
            return isotopeText.TextMetrics;
        }

      

        //draws rthe main atom symbol, or an ellipse if necessary
        private AtomTextMetrics DrawMainSymbol(DrawingContext drawingContext)
        {
            if (AtomSymbol == "") //implicit carbon
            {
                //so draw a circle
                double radiusX = Globals.AtomWidth / 2;
                drawingContext.DrawEllipse(Fill, null, Position, radiusX, radiusX);
                Rect boundingBox = new Rect(new Point(Position.X-radiusX, Position.Y-radiusX), new Point(Position.X+radiusX, Position.Y+radiusX));
                return new AtomTextMetrics()
                {
                    BoundingBox = boundingBox,
                    Geocenter = Position,
                    TotalBoundingBox = boundingBox
                };
            }
            else
            {
                var symbolText = new GlyphText(AtomSymbol, GlyphUtils.SymbolTypeface, GlyphUtils.SymbolSize, PixelsPerDip());
                symbolText.MeasureAtCenter(Position);

                symbolText.DrawAtBottomLeft(symbolText.TextMetrics.BoundingBox.BottomLeft, drawingContext);

                return symbolText.TextMetrics;
            }

        }


     
        public AtomTextMetrics DrawSelf(DrawingContext drawingContext, AtomTextMetrics measure)
        {
            GlyphUtils.GlyphInfo symbolInfo = new GlyphUtils.GlyphInfo();
            symbolInfo.Width = 0;





            if (AtomSymbol == "")
            {
                /*return a circle - this is styled in XAML as Transparent
             but highlighted on a  trigger - in the editor anyhow.
             The atomic symbol block will handle the text */

                var basicAtomGeo = new EllipseGeometry(Position, Globals.AtomWidth / 2, Globals.AtomWidth / 2);
               
                return null;
            }
            else
            {
                //measure up first
                   var groupGlyphInfo = GlyphUtils.GetGlyphsAndInfo(AtomSymbol, PixelsPerDip(), 
                       out GlyphRun groupGlyphRun, new Point(0d, 0d), GlyphUtils.GlyphTypeface, GlyphUtils.SymbolSize);
                //get the offset
                   Vector offset = GlyphUtils.GetOffsetVector(groupGlyphRun, GlyphUtils.SymbolSize);
                //establish the bottom left
                Point bottomLeft = Position + offset;
                groupGlyphInfo = GlyphUtils.GetGlyphsAndInfo(AtomSymbol, PixelsPerDip(),
                    out groupGlyphRun, bottomLeft, GlyphUtils.GlyphTypeface, GlyphUtils.SymbolSize);
                drawingContext.DrawGlyphRun(Fill, groupGlyphRun);
                Rect inkBoundingBox = groupGlyphRun.ComputeInkBoundingBox();
                return new AtomTextMetrics
                {
                    BoundingBox = inkBoundingBox,
                    Geocenter = Position,
                    TotalBoundingBox = inkBoundingBox
                };
            }
            
             

        }
      
        #endregion
        #region Dependency Properties

        /// <summary>
        /// Where the H atoms ended up getting drawn by default
        /// </summary>


     
        public Canvas DrawingCanvas
        {
            get { return (Canvas)GetValue(DrawingCanvasProperty); }
            set { SetValue(DrawingCanvasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DrawingCanvas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DrawingCanvasProperty =
            DependencyProperty.Register("DrawingCanvas", typeof(Canvas), typeof(AtomShape),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                    DrawingCanvasChanged));

        private static void DrawingCanvasChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
        }

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
                    FrameworkPropertyMetadataOptions.AffectsRender, PositionChanged));

        private static void PositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
        }

        #endregion Positioning DPs

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
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure,
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
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, ChargeChanged));

        private static void ChargeChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var changedAtomShape = (AtomShape)d;
            var newval = (int?)args.NewValue;
        }

        #endregion Charge DP
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(AtomShape2), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public int? Isotope
        {
            get { return (int?)GetValue(IsotopeProperty); }
            set { SetValue(IsotopeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Isotope.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsotopeProperty =
            DependencyProperty.Register("Isotope", typeof(int?), typeof(AtomShape), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, IsotopeChangedCallback));

        private static void IsotopeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var changedAtomShape = (AtomShape)d;
            var newval = (int?)args.NewValue;
        }
        #endregion
        #region Property wrappers

        public CompassPoints? DefaultHOrientation { get; set; }
        #endregion
    }
}