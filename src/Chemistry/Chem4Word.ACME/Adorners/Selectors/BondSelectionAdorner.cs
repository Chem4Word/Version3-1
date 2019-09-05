// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class BondSelectionAdorner : SingleChemistryAdorner
    {
        #region Properties

        public Bond AdornedBond => (Bond)AdornedChemistry;

        #endregion Properties

        #region Constructors

        public BondSelectionAdorner(EditorCanvas currentEditor, Bond adornedBond) : base(currentEditor, adornedBond)
        {
            IsHitTestVisible = true;
        }

        #endregion Constructors

        #region Methods

        #region Overrides

        protected override void OnRender(DrawingContext drawingContext)
        {
            

            Model model = AdornedBond.Model;
            double renderRadius = (model.XamlBondLength * Globals.FontSizePercentageBond) / 4;

            SolidColorBrush renderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            renderBrush.Opacity = 0.25;

            Matrix toLeft = new Matrix();
            toLeft.Rotate(-90);
            Matrix toRight = new Matrix();
            toRight.Rotate(90);

            Vector right = AdornedBond.BondVector * toRight;
            right.Normalize();
            Vector left = AdornedBond.BondVector * toLeft;
            left.Normalize();

            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = AdornedBond.StartAtom.Position + right * renderRadius;
            pathFigure.IsClosed = true;

            LineSegment lineSegment1 = new LineSegment();
            lineSegment1.Point = AdornedBond.StartAtom.Position + left * renderRadius;
            pathFigure.Segments.Add(lineSegment1);

            LineSegment lineSegment2 = new LineSegment();
            lineSegment2.Point = AdornedBond.EndAtom.Position + left * renderRadius;
            pathFigure.Segments.Add(lineSegment2);

            LineSegment lineSegment3 = new LineSegment();
            lineSegment3.Point = AdornedBond.EndAtom.Position + right * renderRadius;
            pathFigure.Segments.Add(lineSegment3);
            List<PathFigure> figures = new List<PathFigure>();
            figures.Add(pathFigure);
            Geometry pathGeometry = new PathGeometry(figures);

            Geometry start =
                new EllipseGeometry(AdornedBond.StartAtom.Position, renderRadius * 1.25, renderRadius * 1.25);
            Geometry end = new EllipseGeometry(AdornedBond.EndAtom.Position, renderRadius * 1.25, renderRadius * 1.25);
            Geometry final = Geometry.Combine(pathGeometry, start, GeometryCombineMode.Exclude, null);
            final = Geometry.Combine(final, end, GeometryCombineMode.Exclude, null);

            drawingContext.DrawGeometry(renderBrush, null, final);
        }

        #endregion Overrides

        #endregion Methods
    }
}