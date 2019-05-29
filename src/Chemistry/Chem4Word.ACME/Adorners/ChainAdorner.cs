// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners
{
    public class ChainAdorner: Adorner
    {
        private SolidColorBrush _solidColorBrush;

        public Pen BondPen { get; }
        public List<Point> Placements { get; }
        public bool Unsaturated { get; }
        public EditorCanvas CurrentEditor { get; }
        public Point FirstPoint { get; set; }

        public ChainAdorner(Point firstPoint, [NotNull] UIElement adornedElement, double bondThickness,
                            List<Point> placements, Point currentPoint, Atom target) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(SystemColors.HighlightColor);
            _solidColorBrush.Opacity = 0.1;
            
            BondPen = new Pen(SystemColors.HighlightBrush, bondThickness);
            DashedPen= new Pen(SystemColors.HighlightBrush, bondThickness);
            DashedPen.DashStyle = DashStyles.Dash;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
            FirstPoint = firstPoint;
            Placements = placements;
            CurrentPoint = currentPoint;
            CurrentEditor = (EditorCanvas)adornedElement;
            DrawingDetached = (target == null);


        }

        public bool DrawingDetached { get; set; }

        public Pen DashedPen { get;  }

        public Point CurrentPoint { get; }

        public float PixelsPerDip()
        {
            return (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // ToDo: This may not be accurate

            PathSegmentCollection psc = new PathSegmentCollection();
            foreach (Point point in Placements)
            {
                psc.Add(new LineSegment(point, true));
            }

            PathFigure pf = new PathFigure(FirstPoint, psc, false);
            PathGeometry pg = new PathGeometry(new []{pf});
            drawingContext.DrawGeometry(null,BondPen,pg);
            drawingContext.DrawLine(DashedPen, Placements.Last(), CurrentPoint);
            if (DrawingDetached)
            {
                NRingAdorner.DrawRingSize(drawingContext, Placements.Count, Placements.Last(), PixelsPerDip(), BondPen.Brush.Clone());
            }
            else
            { if (Placements.Count > 1)
                {
                    NRingAdorner.DrawRingSize(drawingContext, Placements.Count-1, Placements.Last(), PixelsPerDip(), BondPen.Brush.Clone());
                }

            }
        }
    }

   
}

