// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Annotations;
using Chem4Word.Model.Enums;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Chem4Word.ViewModel.Adorners
{
    public class DrawBondAdorner : Adorner
    {
        //private StreamGeometry _outline;
        private SolidColorBrush _solidColorBrush;

        private Pen _dashPen;

        public BondStereo Stereo { get; set; }

        public string BondOrder { get; set; }

        public Point StartPoint
        {
            get { return (Point)GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(DrawBondAdorner), new FrameworkPropertyMetadata(new Point(0d, 0d), FrameworkPropertyMetadataOptions.AffectsRender));

        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EndPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(DrawBondAdorner), new FrameworkPropertyMetadata(new Point(0d, 0d), FrameworkPropertyMetadataOptions.AffectsRender));

        public DrawBondAdorner([NotNull] UIElement adornedElement, double bondThickness) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(SystemColors.HighlightColor);
            _solidColorBrush.Opacity = 0.5;

            _dashPen = new Pen(SystemColors.HighlightBrush, bondThickness);

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // ToDo: This may not be accurate
            var length = (StartPoint - EndPoint).Length;
            var outline = GetBondGeometry(StartPoint, EndPoint, length, Stereo, BondOrder);

            drawingContext.DrawGeometry(_solidColorBrush, _dashPen, outline);
        }

        public Geometry GetBondGeometry(Point startPoint, Point endPoint, double bondLength, BondStereo stereo, string order)
        {
            //Vector startOffset = new Vector();
            //Vector endOffset = new Vector();

            //check to see if it's a wedge or a hatch yet
            if (stereo == BondStereo.Wedge | stereo == BondStereo.Hatch)
            {
                return BondGeometry.WedgeBondGeometry(startPoint, endPoint, bondLength);
            }

            if (stereo == BondStereo.Indeterminate && (order == Bond.OrderSingle))
            {
                return BondGeometry.WavyBondGeometry(startPoint, endPoint, bondLength);
            }

            var ordervalue = Bond.OrderToOrderValue(order);
            //single or dotted bond
            if (ordervalue <= 1)
            {
                return BondGeometry.SingleBondGeometry(startPoint, endPoint);
            }
            if (ordervalue == 1.5)
            {
                //it's a resonance bond, so we deal with this in OnRender
                //return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                return new StreamGeometry();
            }
            List<Point> dummy = new List<Point>();
            //double bond
            if (ordervalue == 2)
            {
                if (stereo == BondStereo.Indeterminate)
                {
                    return BondGeometry.CrossedDoubleGeometry(startPoint, endPoint, bondLength, ref dummy);
                }
                Point? centroid = null;

                return BondGeometry.DoubleBondGeometry(startPoint, endPoint, bondLength, BondDirection.None,
                    ref dummy, centroid);
            }
            //tripe bond
            if (ordervalue == 3)
            {
                return BondGeometry.TripleBondGeometry(startPoint, endPoint, bondLength, ref dummy);
            }

            return null;
        }
    }
}