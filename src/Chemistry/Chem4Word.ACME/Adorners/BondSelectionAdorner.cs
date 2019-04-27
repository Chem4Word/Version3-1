// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners
{
    public class BondSelectionAdorner : Adorner
    {
        private Bond _adornedBond;
        public EditorCanvas CurrentEditor { get; set; }

        public Bond AdornedBond
        {
            get { return _adornedBond; }
        }

        public BondSelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }

        public BondSelectionAdorner(UIElement adornedElement, Bond adornedBond) : this(adornedElement)
        {
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            CurrentEditor = (EditorCanvas)adornedElement;

            _adornedBond = adornedBond;
            myAdornerLayer.Add(this);
            MouseLeftButtonDown += BondSelectionAdorner_MouseLeftButtonDown;
            MouseMove += BondSelectionAdorner_MouseMove;
            PreviewMouseMove += BondSelectionAdorner_PreviewMouseMove;
            MouseLeftButtonUp += BondSelectionAdorner_MouseLeftButtonUp;
        }

        private void BondSelectionAdorner_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
        }

        private void BondSelectionAdorner_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
        }

        private void BondSelectionAdorner_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
        }

        private void BondSelectionAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Model model = _adornedBond.Model;
            double renderRadius = (model.XamlBondLength * Globals.FontSizePercentageBond) / 4;

            SolidColorBrush renderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            renderBrush.Opacity = 0.25;

            //Pen renderPen = new Pen(SystemColors.HighlightBrush, 1);
            //renderPen.DashStyle = DashStyles.Dash;

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

            Geometry start = new EllipseGeometry(_adornedBond.StartAtom.Position, renderRadius * 1.25, renderRadius * 1.25);
            Geometry end = new EllipseGeometry(_adornedBond.EndAtom.Position, renderRadius * 1.25, renderRadius * 1.25);
            Geometry final = Geometry.Combine(pathGeometry, start, GeometryCombineMode.Exclude, null);
            final = Geometry.Combine(final, end, GeometryCombineMode.Exclude, null);

            drawingContext.DrawGeometry(renderBrush, null, final);
        }

        ~BondSelectionAdorner()
        {
            //this.MouseLeftButtonDown -= BondSelectionAdorner_MouseLeftButtonDown;
        }
    }
}