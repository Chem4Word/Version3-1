// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using Chem4Word.Model;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ViewModel.Adorners
{
    public class BondSelectionAdorner  : Adorner
    {
        private Bond _adornedBond;

        // ToDo: Task 65
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

            SolidColorBrush renderBrush = new SolidColorBrush(Colors.Green);
            renderBrush.Opacity = 0.2;
            double renderRadius = 8.0;
            Pen renderPen = new Pen(new SolidColorBrush(Colors.BlueViolet), 1.5);
            // Ugly plus does not show for vertical or horizontal bonds
            //drawingContext.DrawRectangle(renderBrush, renderPen, _adornedBond.BoundingBox());

            // Better, but needs to be joined by lines at tangents
            drawingContext.DrawEllipse(renderBrush, renderPen, _adornedBond.StartAtom.Position, renderRadius, renderRadius);
            drawingContext.DrawEllipse(renderBrush, renderPen, _adornedBond.EndAtom.Position, renderRadius, renderRadius);

            //Geometry c1 = new EllipseGeometry(_adornedBond.StartAtom.Position, renderRadius, renderRadius);
            //Geometry c2 = new EllipseGeometry(_adornedBond.StartAtom.Position, renderRadius, renderRadius);
            //drawingContext.DrawGeometry(renderBrush, renderPen, Geometry.Combine(c1, c2, GeometryCombineMode.Intersect, LayoutTransform));
        }
    }
}