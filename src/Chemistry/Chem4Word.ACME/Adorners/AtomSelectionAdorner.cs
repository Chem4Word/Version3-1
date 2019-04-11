// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners
{
    public class AtomSelectionAdorner : Adorner
    {
        private Atom _adornedAtom;
        private EditorCanvas _currentEditor;

        public Atom AdornedAtom => _adornedAtom;

        public AtomSelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            //IsHitTestVisible = false;
            //this.MouseLeftButtonDown += AtomSelectionAdorner_MouseLeftButtonDown;
            _currentEditor = (EditorCanvas) AdornedElement;
        }

        private void AtomSelectionAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.RaiseEvent(e);
            }
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

            Model model = _adornedAtom.Parent.Model;
            double renderRadius = (model.XamlBondLength * Globals.FontSizePercentageBond) / 4;

            SolidColorBrush renderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            renderBrush.Opacity = 0.25;

            //Pen renderPen = new Pen(SystemColors.HighlightBrush, 1);
            //renderPen.DashStyle = DashStyles.Dash;

            if (_adornedAtom.SymbolText == "")
            {
                drawingContext.DrawEllipse(renderBrush, null, _adornedAtom.Position, renderRadius, renderRadius);
            }
            else
            {
                drawingContext.DrawGeometry(renderBrush, null,_currentEditor.GetAtomVisual(AdornedAtom).HullGeometry);
                //drawingContext.DrawRectangle(renderBrush, null, _adornedAtom.BoundingBox(_adornedAtom.Parent.Model.FontSize));
            }
        }

        ~AtomSelectionAdorner()
        {
            this.MouseLeftButtonDown -= AtomSelectionAdorner_MouseLeftButtonDown;
        }
    }
}