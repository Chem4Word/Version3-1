// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ViewModel.Adorners
{
    public class BondSelectionAdorner : Adorner
    {
        private Bond _adornedBond;

        public BondSelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }

        public BondSelectionAdorner(UIElement adornedElement, Bond adornedBond) : this(adornedElement)
        {
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            _adornedBond = adornedBond;
            myAdornerLayer.Add(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double renderRadius = 8.0;

            SolidColorBrush renderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            renderBrush.Opacity = 0.25;

            Pen renderPen = new Pen(SystemColors.HighlightBrush, 1);
            renderPen.DashStyle = DashStyles.Dash;

            Matrix toLeft = new Matrix();
            toLeft.Rotate(-90);
            Matrix toRight = new Matrix();
            toRight.Rotate(90);

            Vector right = _adornedBond.BondVector * toRight;
            right.Normalize();
            Vector left = _adornedBond.BondVector * toLeft;
            left.Normalize();

            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = _adornedBond.StartAtom.Position + right * renderRadius;
            pathFigure.IsClosed = true;

            LineSegment lineSegment1 = new LineSegment();
            lineSegment1.Point = _adornedBond.StartAtom.Position + left * renderRadius;
            pathFigure.Segments.Add(lineSegment1);

            LineSegment lineSegment2 = new LineSegment();
            lineSegment2.Point = _adornedBond.EndAtom.Position + left * renderRadius;
            pathFigure.Segments.Add(lineSegment2);

            LineSegment lineSegment3 = new LineSegment();
            lineSegment3.Point = _adornedBond.EndAtom.Position + right * renderRadius;
            pathFigure.Segments.Add(lineSegment3);
            List<PathFigure> figures = new List<PathFigure>();
            figures.Add(pathFigure);
            Geometry pathGeometry = new PathGeometry(figures);

            Geometry start = new EllipseGeometry(_adornedBond.StartAtom.Position, renderRadius + 2, renderRadius + 2);
            Geometry end = new EllipseGeometry(_adornedBond.EndAtom.Position, renderRadius + 2, renderRadius + 2);
            Geometry final = Geometry.Combine(pathGeometry, start, GeometryCombineMode.Exclude, null);
            final = Geometry.Combine(final, end, GeometryCombineMode.Exclude, null);

            drawingContext.DrawGeometry(renderBrush, renderPen, final);
        }
    }
}