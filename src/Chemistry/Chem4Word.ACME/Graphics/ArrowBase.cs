using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chem4Word.ACME.Graphics
{
    //comment to force a check-in
    public enum ArrowEnds
    {
        None = 0,
        Start = 1,
        End = 2,
        Both = 3
    }

    /// <summary>
    /// Draws an arrow.  Override ArrowLineFigure if you want to draw a curvy arrow
    /// </summary>

    public class ArrowBase : Shape
    {
        #region "Fields"
        protected PathGeometry pathgeo;


        #endregion

        #region "Constructors"

        public ArrowBase()
        {


        }

        #endregion

        #region "Properties"
        #region "Dependency Properties"
        public bool IsArrowClosed
        {
            get { return (bool)GetValue(IsArrowClosedProperty); }
            set { SetValue(IsArrowClosedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsArrowClosed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsArrowClosedProperty =
            DependencyProperty.Register("IsArrowClosed", typeof(bool), typeof(ArrowBase), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));




        public Point StartPoint
        {
            get { return (Point)GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(ArrowBase), new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsMeasure));



        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EndPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(ArrowBase), new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsMeasure));


        //readonly property for calculating arrow length
        public double ArrowLength
        {
            get
            {
                Vector vect = EndPoint - StartPoint;
                return vect.Length;
            }
        }

        /// <summary>
        ///     Identifies the ArrowEnds dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowEndsProperty =
            DependencyProperty.Register("ArrowEnds",
                typeof(ArrowEnds), typeof(ArrowBase),
                new FrameworkPropertyMetadata(ArrowEnds.End,
                        FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Gets or sets the property that determines which ends of the
        ///     line have arrows.
        /// </summary>
        public ArrowEnds ArrowEnds
        {
            set { SetValue(ArrowEndsProperty, value); }
            get { return (ArrowEnds)GetValue(ArrowEndsProperty); }
        }

        public double ArrowHeadLength
        {
            get { return (double)GetValue(ArrowHeadLengthProperty); }
            set { SetValue(ArrowHeadLengthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ArrowHeadLength.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArrowHeadLengthProperty =
            DependencyProperty.Register("ArrowHeadLength", typeof(double), typeof(ArrowBase), new FrameworkPropertyMetadata(12.0,
                        FrameworkPropertyMetadataOptions.AffectsMeasure));



        public double HeadAngle
        {
            get { return (double)GetValue(HeadAngleProperty); }
            set { SetValue(HeadAngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeadAngle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeadAngleProperty =
            DependencyProperty.Register("HeadAngle", typeof(double), typeof(ArrowBase), new FrameworkPropertyMetadata(45.0,
                        FrameworkPropertyMetadataOptions.AffectsMeasure));

        #endregion

        protected override System.Windows.Media.Geometry DefiningGeometry
        {
            get
            {
                //draw the main line for the arrow

                pathgeo = new PathGeometry();
                pathgeo.Clear();
                var mainline = ArrowLineFigure();
                //mainline.IsClosed = true;
                pathgeo.Figures.Add(mainline);


                // Draw the arrow at the start of the line.
                if ((ArrowEnds & ArrowEnds.Start) == ArrowEnds.Start)
                {
                    pathgeo.Figures.Add(ArrowHeadGeometry(mainline, true));
                }

                // Draw the arrow at the end of the line.
                if ((ArrowEnds & ArrowEnds.End) == ArrowEnds.End)
                {
                    pathgeo.Figures.Add(ArrowHeadGeometry(mainline));
                }
                return pathgeo;
            }


        }
        /// <summary>
        /// Returns the geometry of the main line. 
        /// Overide this if you want to draw a curvy arrow say
        /// </summary>
        /// <returns>PathFigure describing the line of the arrow</returns>
        protected virtual PathFigure ArrowLineFigure()
        {
            PathFigure mainline = new PathFigure();
            mainline.StartPoint = StartPoint;
            LineSegment ls = new LineSegment(EndPoint, true);
            mainline.Segments.Add(ls);
            return mainline;
        }

        #endregion
        /// <summary>
        /// Returns the head of an arrow
        /// </summary>
        /// <param name="line">PathFigure describing the body of the arrow</param>
        /// <param name="reverse">Draw arrow head at start instead of end of arrow</param>
        /// <returns>Simple path figure of arrow head, oriented appropriately </returns>
        public PathFigure ArrowHeadGeometry(PathFigure line, bool reverse = false)
        {
            Matrix matx = new Matrix();
            double progress = reverse ? 0.0 : 1.0;  //if we're going for the start or end of line
            //Vector headVector = pt1 - pt2;

            //create a simple geometry so we can use a wpf trick to determine the length

            PathGeometry tempPG = new PathGeometry();
            tempPG.Figures.Add(line);


            Point tempPoint, tangent;

            //this is a really cool method to get the angle at the end of a line of any shape. 
            tempPG.GetPointAtFractionLength(progress, out tempPoint, out tangent);
            //chuck away the pathgeometry
            tempPG = null;
            //the tangent is an X & Y coordinate that can be converted into a vector
            Vector headVector = new Vector(tangent.X, tangent.Y);
            //normalize the vector
            headVector.Normalize();
            //and invert it
            headVector *= -ArrowHeadLength;

            LineSegment lineseg = line.Segments[0] as LineSegment;

            PolyLineSegment polyseg = new PolyLineSegment();
            if (!IsArrowClosed)
            {
                polyseg.Points.Clear();
                matx.Rotate(HeadAngle / 2);
                var pointa = tempPoint + headVector * matx;
                polyseg.Points.Add(pointa);

                polyseg.Points.Add(tempPoint);

                matx.Rotate(-HeadAngle);
                var pointb = tempPoint + headVector * matx;
                polyseg.Points.Add(pointb);

                PathSegmentCollection psc = new PathSegmentCollection();
                psc.Add(polyseg);

                PathFigure pathfig = new PathFigure(tempPoint, psc, true);
                return pathfig;
            }
            else
            {
                polyseg.Points.Clear();

                polyseg.Points.Add(tempPoint);

                matx.Rotate(HeadAngle / 2);
                var pointa = tempPoint + headVector * matx;
                polyseg.Points.Add(pointa);

                matx.Rotate(-HeadAngle);
                var pointb = tempPoint + headVector * matx;
                polyseg.Points.Add(pointb);

                PathSegmentCollection psc = new PathSegmentCollection();
                psc.Add(polyseg);

                PathFigure pathfig = new PathFigure(tempPoint, psc, true);
                return pathfig;
            }
        }
    }
}
