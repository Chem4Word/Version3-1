﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.Model.Geometry;

namespace Chem4Word.ACME.Graphics
{
    public class RegularPolygon :Shape
    {




        public int PointCount
        {
            get { return (int)GetValue(PointCountProperty); }
            set { SetValue(PointCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PointCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointCountProperty =
            DependencyProperty.Register("PointCount", typeof(int), typeof(RegularPolygon), new PropertyMetadata(4));



        public double SideLength
        {
            get { return (double)GetValue(SideLengthProperty); }
            set { SetValue(SideLengthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SideLength.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SideLengthProperty =
            DependencyProperty.Register("SideLength", typeof(double), typeof(RegularPolygon), new PropertyMetadata(25.0));


        protected override Geometry DefiningGeometry
        {
            get
            {
                var path = Path;

                path.Data.Freeze();
                return path.Data;

            }
        }

        public Path Path
        {
            get
            {
                double extAngle = 360.00 / (PointCount);
                Point startPoint = new Point(SideLength / 2, 0.0);

                Matrix rotator = new Matrix();
                rotator.Rotate(extAngle / 2.0);
                Vector side = new Vector(SideLength, 0.0);

                List<Point> pathPoints = new List<Point>();

                pathPoints.Add(startPoint);

                for (int i = 1; i < PointCount; i++)
                {
                    side = side * rotator;
                    startPoint = startPoint + side;

                    pathPoints.Add(startPoint);
                    rotator = new Matrix();
                    rotator.Rotate(extAngle);
                }

                var path = BasicGeometry.BuildPath(pathPoints, true);
                return path;
            }
        }
    }
}
