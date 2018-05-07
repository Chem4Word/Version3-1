using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.Model.Annotations;
using Chem4Word.Model.Enums;

namespace Chem4Word.ViewModel.Adorners
{
    public class DrawBondAdorner : Adorner
    {
        private StreamGeometry _outline;
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
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(DrawBondAdorner), new FrameworkPropertyMetadata(new Point(0d,0d), FrameworkPropertyMetadataOptions.AffectsRender));



        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EndPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(DrawBondAdorner), new PropertyMetadata(new Point(0d, 0d)));



        public DrawBondAdorner([NotNull] UIElement adornedElement) : base(adornedElement)
        {
            _solidColorBrush = new SolidColorBrush(SystemColors.HighlightColor);
            _solidColorBrush.Opacity = 0.5;


            _dashPen = new Pen(SystemColors.HighlightBrush, 1);

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        public DrawBondAdorner([NotNull] UIElement adornedElement, StreamGeometry outline) : this(adornedElement)
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

        public Geometry GetBondGeometry(Point? startPoint, Point? endPoint, BondStereo stereo, string order)
        {
            //Vector startOffset = new Vector();
            //Vector endOffset = new Vector();
            if (startPoint != null & endPoint != null)
            {
                //check to see if it's a wedge or a hatch yet
                if (ParentBond.Stereo == BondStereo.Wedge | ParentBond.Stereo == BondStereo.Hatch)
                {
                    return BondGeometry.WedgeBondGeometry(startPoint.Value, endPoint.Value);
                }

                if (ParentBond.Stereo == BondStereo.Indeterminate && ParentBond.OrderValue == 1.0)
                {
                    return BondGeometry.WavyBondGeometry(startPoint.Value, endPoint.Value);
                }

                //single or dotted bond
                if (ParentBond.OrderValue <= 1)
                {
                    return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                }
                if (ParentBond.OrderValue == 1.5)
                {
                    //it's a resonance bond, so we deal with this in OnRender
                    //return BondGeometry.SingleBondGeometry(startPoint.Value, endPoint.Value);
                    return new StreamGeometry();
                }

                //double bond
                if (ParentBond.OrderValue == 2)
                {
                    if (ParentBond.Stereo == BondStereo.Indeterminate)
                    {
                        return BondGeometry.CrossedDoubleGeometry(startPoint.Value, endPoint.Value, ref _enclosingPoly);
                    }
                    Point? centroid = null;
                    if (ParentBond.IsCyclic())
                    {
                        centroid = ParentBond.PrimaryRing?.Centroid;
                    }
                    return BondGeometry.DoubleBondGeometry(startPoint.Value, endPoint.Value, Placement,
                        ref _enclosingPoly, centroid);
                }
                //tripe bond
                if (ParentBond.OrderValue == 3)
                {
                    return BondGeometry.TripleBondGeometry(startPoint.Value, endPoint.Value, ref _enclosingPoly);
                }

                return null;
            }

            return null;
        }
    }
   
}
