// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Enums;
using Chem4Word.Model.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Chem4Word.ViewModel
{
    /// <summary>
    /// Static class to define bond geometries
    /// now uses StreamGeometry in preference to PathGeometry
    /// Old code is commented out
    /// </summary>
    public static class BondGeometry
    {

        /// <summary>
        /// Returns the geometry of a wedge bond.  Hatch bonds use the same geometry
        /// but a different brush.
        /// </summary>
        /// <param name="startPoint">Position of starting atom</param>
        /// <param name="endPoint">Position of ending atom </param>
        /// <param name="bondLength"></param>
        /// <param name="startAtomGeometry"></param>
        /// <param name="endAtomGeometry"></param>
        /// <returns></returns>
        public static Geometry WedgeBondGeometry(Point startPoint, Point endPoint, double bondLength,
            Geometry startAtomGeometry=null, Geometry endAtomGeometry=null)
        {
            void ComputeWedge(StreamGeometry streamGeometry, Point start, Point end, Vector vector)
            {
                Point pointA;
                Point pointB;
                using (StreamGeometryContext sgc = streamGeometry.Open())
                {
                    sgc.BeginFigure(start, true, true);
                    pointA = end + vector;
                    pointB = end - vector;
                    sgc.LineTo(pointA, true, true);
                    sgc.LineTo(pointB, true, true);
                    sgc.Close();
                }
            }



            Vector bondVector = endPoint - startPoint;
            Vector perpVector = bondVector.Perpendicular();
            perpVector.Normalize();
            perpVector *= bondLength * Globals.BondOffsetPecentage;
           

            StreamGeometry sg = new StreamGeometry();

            ComputeWedge(sg,startPoint,endPoint, perpVector);

            sg.Freeze();


            if (startAtomGeometry == null & endAtomGeometry == null)
            {
                return sg;
            }
            else //adjust the start and end points of the bond
            {
                var start = startPoint;
                var end = endPoint;
                Vector offset = bondVector * (1d / 100d);

                if (startAtomGeometry != null)
                {
                    var overlap = startAtomGeometry.FillContainsWithDetail(sg);
                    
                    while (overlap == IntersectionDetail.Intersects)
                    {
                        start += offset;
                        sg = new StreamGeometry();

                        ComputeWedge(sg, start, end, perpVector);

                        overlap = startAtomGeometry.FillContainsWithDetail(sg);
                    }
                }

                if (endAtomGeometry != null)
                {
                    var overlap = endAtomGeometry.FillContainsWithDetail(sg);
                    while (overlap == IntersectionDetail.Intersects)
                    {
                        end -= offset;
                        sg = new StreamGeometry();

                        ComputeWedge(sg, start, end, perpVector);

                        overlap = endAtomGeometry.FillContainsWithDetail(sg);
                    }
                }

               

                sg.Freeze();

                return sg;
            }
        }

        

        /// <summary>
        /// Defines the three parallel lines of a Triple bond.
        /// </summary>
        /// <param name="startPoint">Where the bond starts</param>
        /// <param name="endPoint">Where it ends</param>
        /// <param name="bondLength"></param>
        /// <param name="enclosingPoly"></param>
        /// <param name="startAtomGeometry"></param>
        /// <param name="endAtomGeometry"></param>
        /// <returns></returns>
        public static Geometry TripleBondGeometry(Point startPoint, Point endPoint,
            double bondLength, ref List<Point> enclosingPoly, 
            Geometry startAtomGeometry=null, Geometry endAtomGeometry=null)
        {
            Vector v = endPoint - startPoint;
            Vector normal = v.Perpendicular();
            normal.Normalize();

            double distance = bondLength * Globals.BondOffsetPecentage;
            Point point1 = startPoint + normal * distance;
            Point point2 = point1 + v;

            Point point3 = startPoint - normal * distance;
            Point point4 = point3 + v;

            

            if (startAtomGeometry != null)
            {
                AdjustTerminus(ref startPoint, endPoint, startAtomGeometry);
                AdjustTerminus(ref point1, point2, startAtomGeometry);
                AdjustTerminus(ref point3, point4, startAtomGeometry);
                enclosingPoly = new List<Point> { point1, point2, point4, point3 };
            }

            if (endAtomGeometry != null)
            {
                AdjustTerminus(ref endPoint, startPoint, endAtomGeometry);
                AdjustTerminus(ref point2, point1, endAtomGeometry);
                AdjustTerminus(ref point4, point3, endAtomGeometry);
                enclosingPoly = new List<Point>() { point1, point2, point4, point3 };
            }
            StreamGeometry sg = new StreamGeometry();
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(startPoint, false, false);
                sgc.LineTo(endPoint, true, false);
                sgc.BeginFigure(point1, false, false);
                sgc.LineTo(point2, true, false);
                sgc.BeginFigure(point3, false, false);
                sgc.LineTo(point4, true, false);
                sgc.Close();
            }
            sg.Freeze();

            return sg;
        }

        /// <summary>
        /// draws the two parallel lines of a double bond
        /// These bonds can either straddle the atom-atom line or fall to one or other side of it
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="bondLength"></param>
        /// <param name="doubleBondPlacement"></param>
        /// <param name="ringCentroid"></param>
        /// <param name="enclosingPoly"></param>
        /// <returns></returns>
        public static System.Windows.Media.Geometry DoubleBondGeometry(Point startPoint, Point endPoint, double bondLength,
            BondDirection doubleBondPlacement, ref List<Point> enclosingPoly, Point? ringCentroid = null, 
            Geometry startAtomGeometry=null, Geometry endAtomGeometry=null)

        {
            Point point1;
            Point point2;
            Point point3;
            Point point4;
            enclosingPoly = GetDoubleBondPoints(startPoint, endPoint, bondLength, doubleBondPlacement, ringCentroid, out point1, out point2, out point3, out point4);
            if (startAtomGeometry != null)
            {
                AdjustTerminus(ref point1, point2, startAtomGeometry);
                AdjustTerminus(ref point3, point4, startAtomGeometry);
                enclosingPoly = new List<Point> {point1, point2, point4, point3};
            }

            if (endAtomGeometry != null)
            {
                AdjustTerminus(ref point4, point3, endAtomGeometry);
                AdjustTerminus(ref point2, point1, endAtomGeometry);
                enclosingPoly = new List<Point> { point1, point2, point4, point3};
            }
            StreamGeometry sg = new StreamGeometry();
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(point1, false, false);
                sgc.LineTo(point2, true, false);
                sgc.BeginFigure(point3, false, false);
                sgc.LineTo(point4, true, false);
                sgc.Close();
            }
            sg.Freeze();
            return sg;
        }

        /// <summary>
        /// Defines the 4 points that characterise a double bond and returns a list of them in polygon order
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="bondLength"></param>
        /// <param name="doubleBondPlacement"></param>
        /// <param name="ringCentroid"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <param name="point4"></param>
        /// <returns></returns>
        public static List<Point> GetDoubleBondPoints(Point startPoint, Point endPoint, double bondLength, BondDirection doubleBondPlacement,
            Point? ringCentroid, out Point point1, out Point point2, out Point point3, out Point point4)
        {
            List<Point> enclosingPoly;
            Vector v = endPoint - startPoint;
            Vector normal = v.Perpendicular();
            normal.Normalize();

            Point? point3a, point4a;

            double distance = bondLength * Globals.BondOffsetPecentage;

            if (ringCentroid == null)
            {
                switch (doubleBondPlacement)
                {
                    case BondDirection.None:

                        point1 = startPoint + normal * distance;
                        point2 = point1 + v;

                        point3 = startPoint - normal * distance;
                        point4 = point3 + v;

                        break;

                    case BondDirection.Clockwise:
                        {
                            point1 = startPoint;

                            point2 = endPoint;
                            point3 = startPoint - normal * 2 * distance;
                            point4 = point3 + v;

                            break;
                        }

                    case BondDirection.Anticlockwise:
                        point1 = startPoint;
                        point2 = endPoint;
                        point3 = startPoint + normal * 2 * distance;
                        point4 = point3 + v;
                        break;

                    default:

                        point1 = startPoint + normal * distance;
                        point2 = point1 + v;

                        point3 = startPoint - normal * distance;
                        point4 = point3 + v;
                        break;
                }
            }
            else
            {
                point1 = startPoint;
                point2 = endPoint;

                var bondvector = endPoint - startPoint;
                var centreVector = ringCentroid - startPoint;
                var bondPlacement = (BondDirection)Math.Sign(Vector.CrossProduct(centreVector.Value, bondvector));
                if (bondPlacement == BondDirection.Clockwise)
                {
                    point3 = startPoint - normal * 2 * distance;
                    point4 = point3 + v;
                }
                else
                {
                    point3 = startPoint + normal * 2 * distance;
                    point4 = point3 + v;
                }

                point3a = BasicGeometry.LineSegmentsIntersect(startPoint, ringCentroid.Value, point3, point4);

                var tempPoint3 = point3a ?? point3;

                point4a = BasicGeometry.LineSegmentsIntersect(endPoint, ringCentroid.Value, point3, point4);

                var tempPoint4 = point4 = point4a ?? point4;

                point3 = tempPoint3;
                point4 = tempPoint4;
            }
            //capture  the enclosing polygon for hit testing later

            enclosingPoly = new List<Point>() { point1, point2, point4, point3 };

            //shorten the supporting bond if it's a ring bond
            if (ringCentroid != null)
            {
            }
            return enclosingPoly;
        }

        /// <summary>
        /// Draws the crossed double bond to indicate indeterminate geometry
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="enclosingPoly"></param>
        /// <returns></returns>
        public static Geometry CrossedDoubleGeometry(Point startPoint, Point endPoint, double bondLength, 
            ref List<Point> enclosingPoly, Geometry startAtomGeometry = null, Geometry endAtomGeometry = null)
        {
            Vector v = endPoint - startPoint;
            Vector normal = v.Perpendicular();
            normal.Normalize();

            Point point1, point2, point3, point4;

            double distance = bondLength * Globals.BondOffsetPecentage;

            point1 = startPoint + normal * distance;
            point2 = point1 + v;

            point3 = startPoint - normal * distance;
            point4 = point3 + v;

           
            enclosingPoly = new List<Point> { point1, point2, point4, point3 };

            if (startAtomGeometry != null)
            {
                
                AdjustTerminus(ref point1, point2, startAtomGeometry);
                AdjustTerminus(ref point3, point4, startAtomGeometry);
                enclosingPoly = new List<Point> { point1, point2, point4, point3 };
            }

            if (endAtomGeometry != null)
            {
                AdjustTerminus(ref point2, point1, endAtomGeometry);
                AdjustTerminus(ref point4, point3, endAtomGeometry);
                enclosingPoly = new List<Point> { point1, point2, point4, point3 };
            }
            StreamGeometry sg = new StreamGeometry();
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(point1, false, false);
                sgc.LineTo(point4, true, false);
                sgc.BeginFigure(point2, false, false);
                sgc.LineTo(point3, true, false);
                sgc.Close();
            }
            sg.Freeze();
            return sg;
        }

        public static Geometry SingleBondGeometry(Point startPoint, Point endPoint, Geometry startAtomGeometry=null, Geometry endAtomGeometry=null)
        {
            var start = startPoint;
            var end = endPoint;

            StreamGeometry sg = new StreamGeometry();
            if (startAtomGeometry != null)
            {
                AdjustTerminus(ref start, end, startAtomGeometry);
            }

            if (endAtomGeometry != null)
            {
                AdjustTerminus(ref end, start, endAtomGeometry);
            }
            using (StreamGeometryContext sgc = sg.Open())
            {
                sgc.BeginFigure(start, false, false);
                sgc.LineTo(end, true, false);
                sgc.Close();
            }
            sg.Freeze();
            return sg;
        }

        public static void AdjustTerminus(ref Point startPoint, Point endPoint, Geometry startAtomGeometry)
        {
            Vector bondVector = endPoint - startPoint;

            Point tempStartPoint = startPoint;
            Vector offset = bondVector * (1d / 100d);

            while (startAtomGeometry.FillContains(tempStartPoint))
            {
                tempStartPoint += offset;
            }

            startPoint = tempStartPoint;

        }

        private static List<PathFigure> GetSingleBondSegment(Point startPoint, Point endPoint)
        {
            List<PathSegment> segments = new List<PathSegment> { new LineSegment(endPoint, false) };

            List<PathFigure> figures = new List<PathFigure>();
            PathFigure pf = new PathFigure(startPoint, segments, true);
            figures.Add(pf);
            return figures;
        }

        public static Geometry WavyBondGeometry(Point startPoint, Point endPoint, double standardBondLength,
            Geometry startAtomGeometry=null, Geometry endAtomGeometry=null)
        {
            Point newStart = startPoint;
            Point newEnd = endPoint;
            StreamGeometry sg = new StreamGeometry();

            if (startAtomGeometry != null)
            {
                AdjustTerminus(ref newStart, newEnd, startAtomGeometry);
            }

            if (endAtomGeometry != null)
            {
                AdjustTerminus(ref newEnd, newStart, endAtomGeometry);
            }
            using (StreamGeometryContext sgc = sg.Open())
            {
                Vector bondVector = newEnd - newStart;
                int noOfWiggles = (int)Math.Ceiling(bondVector.Length / standardBondLength * Globals.BondOffsetPecentage);
                if (noOfWiggles < 3)
                {
                    noOfWiggles = 3;
                }

                double wiggleLength = bondVector.Length / noOfWiggles;
                Debug.WriteLine($"standardBondLength: {standardBondLength} noOfWiggles: {noOfWiggles}");

                Vector halfAWiggle = bondVector;
                halfAWiggle.Normalize();
                halfAWiggle *= wiggleLength / 2;

                Matrix toLeft = new Matrix();
                toLeft.Rotate(-60);
                Matrix toRight = new Matrix();
                toRight.Rotate(60);
                Vector leftVector = halfAWiggle * toLeft;
                Vector rightVector = halfAWiggle * toRight;

                List<Point> allpoints = new List<Point>();



               
                allpoints.Add(newStart);

                Point lastPoint = newStart;
                for (int i = 0; i < noOfWiggles; i++)
                {
                    Point leftPoint = lastPoint + leftVector;
                    allpoints.Add(leftPoint);
                    allpoints.Add(lastPoint + halfAWiggle);
                    Point rightPoint = lastPoint + halfAWiggle + rightVector;
                    allpoints.Add(rightPoint);
                    lastPoint += halfAWiggle * 2;
                    allpoints.Add(lastPoint);
                }
                allpoints.Add(newEnd);
                MakePathFromPoints(sgc, allpoints);
                sgc.Close();
            }
            sg.Freeze();
            return sg;
        }

        private static void MakePathFromPoints(StreamGeometryContext sgc, List<Point> allpoints)
        {
            sgc.BeginFigure(allpoints[0], false, false);
            sgc.PolyQuadraticBezierTo(allpoints.Skip(1).ToArray(), true, true);
        }
    }
}