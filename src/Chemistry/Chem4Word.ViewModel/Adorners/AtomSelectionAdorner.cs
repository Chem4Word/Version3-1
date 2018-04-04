// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ViewModel.Adorners
{
    public class AtomSelectionAdorner : Adorner
    {
        private Atom _adornedAtom;

        public AtomSelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }

        public AtomSelectionAdorner(UIElement adornedElement, Atom adornedAtom) : this(adornedElement)
        {
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            _adornedAtom = adornedAtom;
            myAdornerLayer.Add(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            SolidColorBrush renderBrush = new SolidColorBrush(Colors.Green);
            renderBrush.Opacity = 0.2;
            Pen renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);
            double renderRadius = 8.0;
            if (_adornedAtom.SymbolText == "")
            {
                drawingContext.DrawEllipse(renderBrush, renderPen, _adornedAtom.Position, renderRadius, renderRadius);
            }
            else
            {
                drawingContext.DrawRectangle(renderBrush, renderPen, _adornedAtom.BoundingBox());
            }
        }
    }
}