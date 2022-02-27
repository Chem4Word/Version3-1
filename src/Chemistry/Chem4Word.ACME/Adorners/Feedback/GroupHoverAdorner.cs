﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Feedback
{
    public class GroupHoverAdorner : Adorner
    {
        private const double Spacing = 2.0;
        private SolidColorBrush _solidColorBrush;
        private Pen _bracketPen;
        private GroupVisual _targetedVisual;

        public GroupHoverAdorner([NotNull] UIElement adornedElement) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(Globals.HoverAdornerColor);

            _bracketPen = new Pen(_solidColorBrush, Globals.HoverAdornerThickness);
            _bracketPen.StartLineCap = PenLineCap.Round;
            _bracketPen.EndLineCap = PenLineCap.Round;

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        public GroupHoverAdorner(UIElement adornedElement, GroupVisual targetedVisual) : this(adornedElement)
        {
            _targetedVisual = targetedVisual;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            StreamGeometry sg = new StreamGeometry();

            Rect atomBounds = _targetedVisual.ContentBounds;
            atomBounds.Inflate(Spacing, Spacing);
            Vector twiddle = new Vector(3, 0.0);
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(atomBounds.TopLeft + twiddle, false, false);
                sgc.LineTo(atomBounds.TopLeft, true, true);
                sgc.LineTo(atomBounds.BottomLeft, true, true);
                sgc.LineTo(atomBounds.BottomLeft + twiddle, true, true);

                sgc.BeginFigure(atomBounds.TopRight - twiddle, false, false);
                sgc.LineTo(atomBounds.TopRight, true, true);
                sgc.LineTo(atomBounds.BottomRight, true, true);
                sgc.LineTo(atomBounds.BottomRight - twiddle, true, true);
                sgc.Close();
            }

            drawingContext.DrawGeometry(_solidColorBrush, _bracketPen, sg);
        }
    }
}