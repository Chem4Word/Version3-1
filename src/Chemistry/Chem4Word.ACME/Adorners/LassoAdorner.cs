// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2.Annotations;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ACME.Adorners
{
    public class LassoAdorner : Adorner
    {
        private StreamGeometry _outline;
        private SolidColorBrush _solidColorBrush;
        private Pen _dashPen;
        private EditorCanvas CurrentEditor { get; }

        public LassoAdorner([NotNull] UIElement adornedElement) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(SystemColors.HighlightColor);
            _solidColorBrush.Opacity = 0.25;

            _dashPen = new Pen(SystemColors.HighlightBrush, 1);
            _dashPen.DashStyle = DashStyles.Dash;
            CurrentEditor = (EditorCanvas)adornedElement;
            PreviewMouseLeftButtonUp += LassoAdorner_PreviewMouseLeftButtonUp;
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        private void LassoAdorner_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

       

        public LassoAdorner([NotNull] UIElement adornedElement, StreamGeometry outline) : this(adornedElement)
        {
            _outline = outline;
        }

        public StreamGeometry Outline
        {
            get { return _outline; }
            set
            {
                _outline = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawGeometry(_solidColorBrush, _dashPen, _outline);
        }
    }
}