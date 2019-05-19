// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using Chem4Word.Model2.Annotations;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners
{
    public class NRingAdorner : FixedRingAdorner
    {
        public Point EndPoint { get; }
        public Point StartPoint { get; }

        public NRingAdorner([NotNull] UIElement adornedElement, double bondThickness, List<Point> placements, Point startPoint, Point endPoint) : base(adornedElement, bondThickness, placements)
        {
            MouseLeftButtonDown += FixedRingAdorner_MouseLeftButtonDown;
            PreviewMouseUp += NRingAdorner_PreviewMouseUp;
            MouseUp += NRingAdorner_MouseUp;
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        private void NRingAdorner_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void NRingAdorner_PreviewMouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void FixedRingAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //_currentEditor.RaiseEvent(e);
            //e.Handled = true;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            DrawPlacementArrow(drawingContext);
            DrawRingSize(drawingContext);
        }

        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }

        private void DrawRingSize(DrawingContext drawingContext)
        {
            Point pos = (EndPoint - StartPoint) * 0.5 + StartPoint;
            int RingSize = Placements.Count;
            string ringLabel = RingSize.ToString();
            var symbolText = new GlyphText(ringLabel,
                                           GlyphUtils.SymbolTypeface, GlyphText.SymbolSize, PixelsPerDip());
            Brush fillBrush = BondPen.Brush.Clone();

            Pen boundaryPen = new Pen(fillBrush, 2.0);

            symbolText.MeasureAtCenter(pos);
            var textMetricsBoundingBox = symbolText.TextMetrics.BoundingBox;
            double radius =
                (textMetricsBoundingBox.BottomRight - textMetricsBoundingBox.TopLeft).Length /
                2 * 1.1;
            drawingContext.DrawEllipse(new SolidColorBrush(SystemColors.WindowColor), boundaryPen, pos, radius, radius);
            symbolText.Fill = fillBrush;
            symbolText.DrawAtBottomLeft(textMetricsBoundingBox.BottomLeft, drawingContext);
        }

        private void DrawPlacementArrow(DrawingContext dc)
        {
            Brush fillBrush = BondPen.Brush.Clone();

            var fatArrow = FatArrowGeometry.GetArrowGeometry(StartPoint, EndPoint);
            dc.DrawGeometry(fillBrush, null, fatArrow);
        }
    }
}