// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Controls
{
    public class ZoomBox : Control
    {
        private Thumb zoomThumb;
        private Canvas zoomCanvas;
        private Slider zoomSlider;
        private ScaleTransform scaleTransform;

        #region DPs

        #region ScrollViewer

        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }

        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register("ScrollViewer", typeof(ScrollViewer), typeof(ZoomBox));

        #endregion ScrollViewer

        #region ChemistryCanvas

        public static readonly DependencyProperty ChemistryCanvasProperty =
            DependencyProperty.Register("ChemistryCanvas", typeof(ChemistryCanvas), typeof(ZoomBox),
                new FrameworkPropertyMetadata(null,
                    OnChemistryCanvasChanged));

        public ChemistryCanvas ChemistryCanvas
        {
            get { return (ChemistryCanvas)GetValue(ChemistryCanvasProperty); }
            set { SetValue(ChemistryCanvasProperty, value); }
        }

        private static void OnChemistryCanvasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoomBox target = (ZoomBox)d;
            ChemistryCanvas oldDesignerCanvas = (ChemistryCanvas)e.OldValue;
            ChemistryCanvas newDesignerCanvas = target.ChemistryCanvas;
            target.OnChemistryCanvasChanged(oldDesignerCanvas, newDesignerCanvas);
        }

        protected virtual void OnChemistryCanvasChanged(ChemistryCanvas oldDesignerCanvas, ChemistryCanvas newDesignerCanvas)
        {
            if (oldDesignerCanvas != null)
            {
                newDesignerCanvas.LayoutUpdated -= DesignerCanvas_LayoutUpdated;
                newDesignerCanvas.MouseWheel -= DesignerCanvas_MouseWheel;
            }

            if (newDesignerCanvas != null)
            {
                newDesignerCanvas.LayoutUpdated += DesignerCanvas_LayoutUpdated;
                newDesignerCanvas.MouseWheel += DesignerCanvas_MouseWheel;
                newDesignerCanvas.LayoutTransform = scaleTransform;
            }
        }

        #endregion ChemistryCanvas

        #endregion DPs

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (ScrollViewer == null)
            {
                return;
            }

            zoomThumb = Template.FindName("PART_ZoomThumb", this) as Thumb;
            if (zoomThumb == null)
            {
                Debugger.Break();
                throw new Exception("PART_ZoomThumb template is missing!");
            }

            zoomCanvas = Template.FindName("PART_ZoomCanvas", this) as Canvas;
            if (zoomCanvas == null)
            {
                Debugger.Break();
                throw new Exception("PART_ZoomCanvas template is missing!");
            }

            zoomSlider = Template.FindName("PART_ZoomSlider", this) as Slider;
            if (zoomSlider == null)
            {
                Debugger.Break();
                throw new Exception("PART_ZoomSlider template is missing!");
            }

            zoomThumb.DragDelta += Thumb_DragDelta;
            zoomSlider.ValueChanged += ZoomSlider_ValueChanged;
            scaleTransform = new ScaleTransform();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double scale = e.NewValue / e.OldValue;
            double halfViewportHeight = ScrollViewer.ViewportHeight / 2;
            double newVerticalOffset = ((ScrollViewer.VerticalOffset + halfViewportHeight) * scale - halfViewportHeight);
            double halfViewportWidth = ScrollViewer.ViewportWidth / 2;
            double newHorizontalOffset = ((ScrollViewer.HorizontalOffset + halfViewportWidth) * scale - halfViewportWidth);
            scaleTransform.ScaleX *= scale;
            scaleTransform.ScaleY *= scale;
            ScrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
            ScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double scale, xOffset, yOffset;
            InvalidateScale(out scale, out xOffset, out yOffset);
            ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset + e.HorizontalChange / scale);
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + e.VerticalChange / scale);
        }

        private void DesignerCanvas_LayoutUpdated(object sender, EventArgs e)
        {
            double scale, xOffset, yOffset;
            InvalidateScale(out scale, out xOffset, out yOffset);
            zoomThumb.Width = ScrollViewer.ViewportWidth * scale;
            zoomThumb.Height = ScrollViewer.ViewportHeight * scale;
            Canvas.SetLeft(zoomThumb, xOffset + ScrollViewer.HorizontalOffset * scale);
            Canvas.SetTop(zoomThumb, yOffset + ScrollViewer.VerticalOffset * scale);
        }

        private void DesignerCanvas_MouseWheel(object sender, EventArgs e)
        {
            MouseWheelEventArgs wheel = (MouseWheelEventArgs)e;

            //divide the value by 10 so that it is more smooth
            double value = Math.Max(0, wheel.Delta / 10);
            value = Math.Min(wheel.Delta, 10);
            zoomSlider.Value += value;
        }

        private void InvalidateScale(out double scale, out double xOffset, out double yOffset)
        {
            double w = ChemistryCanvas.ActualWidth * scaleTransform.ScaleX;
            double h = ChemistryCanvas.ActualHeight * scaleTransform.ScaleY;

            // zoom canvas size
            double x = zoomCanvas.ActualWidth;
            double y = zoomCanvas.ActualHeight;
            double scaleX = x / w;
            double scaleY = y / h;
            scale = (scaleX < scaleY) ? scaleX : scaleY;
            xOffset = (x - scale * w) / 2;
            yOffset = (y - scale * h) / 2;
        }
    }
}