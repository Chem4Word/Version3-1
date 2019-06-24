// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.ACME.Controls;

namespace Chem4Word.ACME.Adorners
{
    public class DrawBondAdorner : Adorner
    {
        //private StreamGeometry _outline;
        private SolidColorBrush _solidColorBrush;

        private Pen _dashPen;

        private EditorCanvas CurrentEditor { get; }
        public Globals.BondStereo Stereo { get; set; }

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

        public Bond ExistingBond { get; set; }

        // Using a DependencyProperty as the backing store for EndPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(DrawBondAdorner), new FrameworkPropertyMetadata(new Point(0d, 0d), FrameworkPropertyMetadataOptions.AffectsRender));

        public DrawBondAdorner([NotNull] UIElement adornedElement, double bondThickness) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(SystemColors.HighlightColor);
            _solidColorBrush.Opacity = 0.5;
            _dashPen = new Pen(SystemColors.HighlightBrush, bondThickness);

            CurrentEditor = (EditorCanvas) adornedElement;
        
            PreviewKeyDown += DrawBondAdorner_PreviewKeyDown;
    
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);

            Focusable = true;
            Focus();
        }

        private void DrawBondAdorner_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Geometry outline;
            // ToDo: This may not be accurate
            var length = (StartPoint - EndPoint).Length;
            if (ExistingBond == null || !ExistingBond.IsCyclic())
            {
                outline = GetBondGeometry(StartPoint, EndPoint, length, Stereo, BondOrder);
            }
            else
            {
                outline = GetBondGeometry(StartPoint, EndPoint, length, Stereo, BondOrder, ExistingBond.PrimaryRing, ExistingBond.SubsidiaryRing);
            }
            drawingContext.DrawGeometry(_solidColorBrush, _dashPen, outline);
        }

        public static Geometry GetBondGeometry(Point startPoint, Point endPoint, double bondLength, Globals.BondStereo stereo, string order, Ring existingRing = null, Ring subsidiaryRing=null)
        {
            //Vector startOffset = new Vector();
            //Vector endOffset = new Vector();

            BondDescriptor sbd=null;
            //check to see if it's a wedge or a hatch yet
            if (stereo == Globals.BondStereo.Wedge | stereo == Globals.BondStereo.Hatch)
            {
                var wbd = new WedgeBondDescriptor {Start = startPoint, End = endPoint};
                BondGeometry.GetWedgeBondGeometry(wbd, bondLength);
                sbd = wbd;
            }

            if (stereo == Globals.BondStereo.Indeterminate && (order == Globals.OrderSingle))
            {
                sbd = new BondDescriptor {Start = startPoint, End = endPoint};
                BondGeometry.GetWavyBondGeometry(sbd, bondLength);
            }

            var ordervalue = Bond.OrderToOrderValue(order);
            //single or dotted bond
            if (ordervalue <= 1)
            {
                sbd = new BondDescriptor { Start = startPoint, End = endPoint };
                BondGeometry.GetSingleBondGeometry(sbd);
            }
            if (ordervalue == 1.5)
            {
                //it's a resonance bond, so we deal with this in OnRender
                //return BondGeometry.GetSingleBondGeometry(startPoint.Value, endPoint.Value);
                return new StreamGeometry();
            }
            List<Point> dummy = new List<Point>();
            //double bond
            if (ordervalue == 2)
            {
                DoubleBondDescriptor dbd = new DoubleBondDescriptor() {Start = startPoint, End = endPoint};
                if (stereo == Globals.BondStereo.Indeterminate)
                {
                    
                    BondGeometry.GetCrossedDoubleGeometry(dbd, bondLength);
                }
                else
                {
                    dbd.PrimaryCentroid = existingRing?.Centroid;
                    dbd.SecondaryCentroid = subsidiaryRing?.Centroid;
                    BondGeometry.GetDoubleBondGeometry(dbd, bondLength);
                }

                sbd = dbd;
            }
            //tripe bond
            if (ordervalue == 3)
            {
                var tbd= new TripleBondDescriptor() {Start = startPoint, End=endPoint};
                BondGeometry.GetTripleBondGeometry( tbd, bondLength);
            }

            return sbd?.DefiningGeometry;
        }
    }
}