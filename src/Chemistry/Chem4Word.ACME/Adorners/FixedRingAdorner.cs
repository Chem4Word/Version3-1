// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2.Annotations;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners
{
    public class FixedRingAdorner : Adorner
    {
        private SolidColorBrush _solidColorBrush;
        public Pen BondPen { get; }
        public List<Point> Placements { get; }
        public bool Unsaturated { get; }
        public EditorCanvas CurrentEditor { get; }
        private readonly RingDrawer _ringDrawer;

        public FixedRingAdorner([NotNull] UIElement adornedElement, double bondThickness, List<Point> placements, bool unsaturated = false) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(SystemColors.HighlightColor);
            _solidColorBrush.Opacity = 0.1;

            BondPen = new Pen(SystemColors.HighlightBrush, bondThickness);

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
            Placements = placements;
            Unsaturated = unsaturated;
            CurrentEditor = (EditorCanvas)adornedElement;
            MouseLeftButtonDown += FixedRingAdorner_MouseLeftButtonDown;
            _ringDrawer = new RingDrawer(this);
        }

        private void FixedRingAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //_currentEditor.RaiseEvent(e);
            //e.Handled = true;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // ToDo: This may not be accurate

            List<NewAtomPlacement> newPlacements = new List<NewAtomPlacement>();

            RingBehavior.FillExistingAtoms(Placements, Placements, newPlacements, CurrentEditor);

            _ringDrawer.DrawNRing(drawingContext, newPlacements);
        }
    }
}