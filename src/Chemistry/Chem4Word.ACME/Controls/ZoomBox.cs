// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;

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
        #endregion

        #region ChemistryCanvas


        public static readonly DependencyProperty ChemistryCanvasProperty =
            DependencyProperty.Register("ChemistryCanvas", typeof(ChemistryCanvas), typeof(ZoomBox),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnDesignerCanvasChanged)));


        public ChemistryCanvas ChemistryCanvas
        {
            get { return (ChemistryCanvas)GetValue(ChemistryCanvasProperty); }
            set { SetValue(ChemistryCanvasProperty, value); }
        }


        private static void OnDesignerCanvasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoomBox target = (ZoomBox)d;
            ChemistryCanvas oldDesignerCanvas = (ChemistryCanvas)e.OldValue;
            ChemistryCanvas newDesignerCanvas = target.ChemistryCanvas;
            target.OnDesignerCanvasChanged(oldDesignerCanvas, newDesignerCanvas);
        }


        protected virtual void OnDesignerCanvasChanged(ChemistryCanvas oldDesignerCanvas, ChemistryCanvas newDesignerCanvas)
        {
            if (oldDesignerCanvas != null)
            {
                newDesignerCanvas.LayoutUpdated -= new EventHandler(this.DesignerCanvas_LayoutUpdated);
                newDesignerCanvas.MouseWheel -= new MouseWheelEventHandler(this.DesignerCanvas_MouseWheel);
            }

            if (newDesignerCanvas != null)
            {
                newDesignerCanvas.LayoutUpdated += new EventHandler(this.DesignerCanvas_LayoutUpdated);
                newDesignerCanvas.MouseWheel += new MouseWheelEventHandler(this.DesignerCanvas_MouseWheel);
                newDesignerCanvas.LayoutTransform = this.scaleTransform;
            }
        }

        #endregion

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.ScrollViewer == null)
            {
                return;
            }

            this.zoomThumb = Template.FindName("PART_ZoomThumb", this) as Thumb;
            if (this.zoomThumb == null)
            {
                throw new Exception("PART_ZoomThumb template is missing!");
            }

            this.zoomCanvas = Template.FindName("PART_ZoomCanvas", this) as Canvas;
            if (this.zoomCanvas == null)
            {
                throw new Exception("PART_ZoomCanvas template is missing!");
            }

            this.zoomSlider = Template.FindName("PART_ZoomSlider", this) as Slider;
            if (this.zoomSlider == null)
            {
                throw new Exception("PART_ZoomSlider template is missing!");
            }

            this.zoomThumb.DragDelta += new DragDeltaEventHandler(this.Thumb_DragDelta);
            this.zoomSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(this.ZoomSlider_ValueChanged);
            this.scaleTransform = new ScaleTransform();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double scale = e.NewValue / e.OldValue;
            double halfViewportHeight = this.ScrollViewer.ViewportHeight / 2;
            double newVerticalOffset = ((this.ScrollViewer.VerticalOffset + halfViewportHeight) * scale - halfViewportHeight);
            double halfViewportWidth = this.ScrollViewer.ViewportWidth / 2;
            double newHorizontalOffset = ((this.ScrollViewer.HorizontalOffset + halfViewportWidth) * scale - halfViewportWidth);
            this.scaleTransform.ScaleX *= scale;
            this.scaleTransform.ScaleY *= scale;
            this.ScrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
            this.ScrollViewer.ScrollToVerticalOffset(newVerticalOffset);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double scale, xOffset, yOffset;
            this.InvalidateScale(out scale, out xOffset, out yOffset);
            this.ScrollViewer.ScrollToHorizontalOffset(this.ScrollViewer.HorizontalOffset + e.HorizontalChange / scale);
            this.ScrollViewer.ScrollToVerticalOffset(this.ScrollViewer.VerticalOffset + e.VerticalChange / scale);
        }

        private void DesignerCanvas_LayoutUpdated(object sender, EventArgs e)
        {
            double scale, xOffset, yOffset;
            this.InvalidateScale(out scale, out xOffset, out yOffset);
            this.zoomThumb.Width = this.ScrollViewer.ViewportWidth * scale;
            this.zoomThumb.Height = this.ScrollViewer.ViewportHeight * scale;
            Canvas.SetLeft(this.zoomThumb, xOffset + this.ScrollViewer.HorizontalOffset * scale);
            Canvas.SetTop(this.zoomThumb, yOffset + this.ScrollViewer.VerticalOffset * scale);
        }

        private void DesignerCanvas_MouseWheel(object sender, EventArgs e)
        {
            MouseWheelEventArgs wheel = (MouseWheelEventArgs)e;

            //divide the value by 10 so that it is more smooth
            double value = Math.Max(0, wheel.Delta / 10);
            value = Math.Min(wheel.Delta, 10);
            this.zoomSlider.Value += value;
        }

        private void InvalidateScale(out double scale, out double xOffset, out double yOffset)
        {
            double w = ChemistryCanvas.ActualWidth * this.scaleTransform.ScaleX;
            double h = ChemistryCanvas.ActualHeight * this.scaleTransform.ScaleY;

            // zoom canvas size
            double x = this.zoomCanvas.ActualWidth;
            double y = this.zoomCanvas.ActualHeight;
            double scaleX = x / w;
            double scaleY = y / h;
            scale = (scaleX < scaleY) ? scaleX : scaleY;
            xOffset = (x - scale * w) / 2;
            yOffset = (y - scale * h) / 2;
        }
    }
}
