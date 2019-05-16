// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners
{
    /// <summary>
    /// Decorates an atom to show it is selected
    /// </summary>
    public class AtomSelectionAdorner : Adorner
    {
        private Atom _adornedAtom;
        public EditorCanvas CurrentEditor { get; }

        public Atom AdornedAtom => _adornedAtom;

        public AtomSelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            CurrentEditor = (EditorCanvas)AdornedElement;
            MouseLeftButtonDown += AtomSelectionAdorner_MouseLeftButtonDown;
            PreviewMouseLeftButtonDown += AtomSelectionAdorner_PreviewMouseLeftButtonDown;
            MouseMove += AtomSelectionAdorner_MouseMove;
            PreviewMouseMove += AtomSelectionAdorner_PreviewMouseMove;
            PreviewMouseLeftButtonUp += AtomSelectionAdorner_PreviewMouseLeftButtonUp;
            PreviewMouseRightButtonUp += AtomSelectionAdorner_PreviewMouseRightButtonUp;
            MouseLeftButtonUp += AtomSelectionAdorner_MouseLeftButtonUp;
            IsHitTestVisible = true;
        }

        private void AtomSelectionAdorner_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
            ;
        }

        private void AtomSelectionAdorner_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void AtomSelectionAdorner_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        private void AtomSelectionAdorner_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
        }

        private void AtomSelectionAdorner_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
        }

        private void AtomSelectionAdorner_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
        }

        private void AtomSelectionAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //transmit the event to the current editor
            CurrentEditor.RaiseEvent(e);
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

            if (_adornedAtom.SymbolText == "")
            {
                drawingContext.DrawEllipse(renderBrush, null, _adornedAtom.Position, renderRadius, renderRadius);
            }
            else
            {
                drawingContext.DrawGeometry(renderBrush, null, CurrentEditor.GetAtomVisual(AdornedAtom).HullGeometry);
            }
        }

        ~AtomSelectionAdorner()
        {
            MouseLeftButtonDown -= AtomSelectionAdorner_MouseLeftButtonDown;
            MouseMove += AtomSelectionAdorner_MouseMove;
            MouseLeftButtonUp += AtomSelectionAdorner_MouseLeftButtonUp;
            PreviewMouseRightButtonUp -= AtomSelectionAdorner_PreviewMouseRightButtonUp;
        }
    }
}