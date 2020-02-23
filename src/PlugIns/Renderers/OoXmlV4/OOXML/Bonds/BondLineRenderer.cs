// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Enums;
using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using Point = System.Windows.Point;
using Wpg = DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using Wps = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Chem4Word.Renderer.OoXmlV4.OOXML.Bonds
{
    public class BondLineRenderer
    {
        private Rect _canvasExtents;
        private long _ooxmlId;
        private double _medianBondLength;
        private OoXmlV4Options _options;

        public BondLineRenderer(Rect canvasExtents, OoXmlV4Options options, ref long ooxmlId, double medianBondLength)
        {
            _canvasExtents = canvasExtents;
            _ooxmlId = ooxmlId;
            _medianBondLength = medianBondLength;
            _options = options;
        }

        public void DrawWedgeBond(Wpg.WordprocessingGroup wordprocessingGroup, BondLine bl)
        {
            BondLine leftBondLine = bl.GetParallel(BondOffset() / 2);
            BondLine rightBondLine = bl.GetParallel(-BondOffset() / 2);

            List<Point> points = new List<Point>();
            points.Add(new Point(bl.Start.X, bl.Start.Y));
            points.Add(new Point(leftBondLine.End.X, leftBondLine.End.Y));
            points.Add(new Point(bl.End.X, bl.End.Y));
            points.Add(new Point(rightBondLine.End.X, rightBondLine.End.Y));

            Point wedgeStart = new Point(bl.Start.X, bl.Start.Y);
            Point wedgeEndLeft = new Point(leftBondLine.End.X, leftBondLine.End.Y);
            Point wedgeEndRight = new Point(rightBondLine.End.X, rightBondLine.End.Y);

            Bond thisBond = bl.Bond;
            Atom endAtom = thisBond.EndAtom;

            // EndAtom == C and Label is ""
            if (endAtom.Element as Element == Globals.PeriodicTable.C
                && thisBond.Rings.Count == 0
                && string.IsNullOrEmpty(endAtom.SymbolText))
            {
                // Has at least one other bond
                if (endAtom.Bonds.Count() > 1)
                {
                    var otherBonds = endAtom.Bonds.Except(new[] { thisBond }).ToList();
                    bool allSingle = true;
                    List<Bond> nonHydrogenBonds = new List<Bond>();
                    foreach (var otherBond in otherBonds)
                    {
                        if (!otherBond.Order.Equals(Globals.OrderSingle))
                        {
                            allSingle = false;
                            //break;
                        }

                        var otherAtom = otherBond.OtherAtom(endAtom);
                        if (otherAtom.Element as Element != Globals.PeriodicTable.H)
                        {
                            nonHydrogenBonds.Add(otherBond);
                        }
                    }

                    // All other bonds are single
                    if (allSingle)
                    {
                        // Determine chamfer shape
                        Vector left = (wedgeEndLeft - wedgeStart) * 2;
                        Point leftEnd = wedgeStart + left;

                        Vector right = (wedgeEndRight - wedgeStart) * 2;
                        Point rightEnd = wedgeStart + right;

                        bool intersect;
                        Point intersection;

                        Vector shortestLeft = left;
                        Vector shortestRight = right;

                        if (otherBonds.Count - nonHydrogenBonds.Count == 1)
                        {
                            otherBonds = nonHydrogenBonds;
                        }

                        if (otherBonds.Count == 1)
                        {
                            Bond bond = otherBonds[0];
                            Atom atom = bond.OtherAtom(endAtom);
                            Vector vv = (endAtom.Position - atom.Position) * 2;
                            Point otherEnd = atom.Position + vv;

                            CoordinateTool.FindIntersection(wedgeStart, leftEnd,
                                                            atom.Position, otherEnd,
                                                            out _, out intersect, out intersection);
                            if (intersect)
                            {
                                Vector v = intersection - wedgeStart;
                                if (v.Length < shortestLeft.Length)
                                {
                                    shortestLeft = v;
                                }
                            }

                            CoordinateTool.FindIntersection(wedgeStart, rightEnd,
                                                            atom.Position, otherEnd,
                                                            out _, out intersect, out intersection);
                            if (intersect)
                            {
                                Vector v = intersection - wedgeStart;
                                if (v.Length < shortestRight.Length)
                                {
                                    shortestRight = v;
                                }
                            }

                            // Re-write list of points
                            points = new List<Point>();
                            points.Add(wedgeStart);
                            points.Add(wedgeStart + shortestLeft);
                            points.Add(endAtom.Position);
                            points.Add(wedgeStart + shortestRight);
                        }
                        else
                        {
                            foreach (var bond in otherBonds)
                            {
                                CoordinateTool.FindIntersection(wedgeStart, leftEnd,
                                                                bond.StartAtom.Position, bond.EndAtom.Position,
                                                                out _, out intersect, out intersection);
                                if (intersect)
                                {
                                    Vector v = intersection - wedgeStart;
                                    if (v.Length < shortestLeft.Length)
                                    {
                                        shortestLeft = v;
                                    }
                                }

                                CoordinateTool.FindIntersection(wedgeStart, rightEnd,
                                                                bond.StartAtom.Position, bond.EndAtom.Position,
                                                                out _, out intersect, out intersection);
                                if (intersect)
                                {
                                    Vector v = intersection - wedgeStart;
                                    if (v.Length < shortestRight.Length)
                                    {
                                        shortestRight = v;
                                    }
                                }
                            }

                            // Re-write list of points
                            points = new List<Point>();
                            points.Add(wedgeStart);
                            points.Add(wedgeStart + shortestLeft);
                            points.Add(endAtom.Position);
                            points.Add(wedgeStart + shortestRight);
                        }
                    }
                }
            }

            switch (bl.Style)
            {
                case BondLineStyle.Wedge:
                    DrawWedgeBond(wordprocessingGroup, points);
                    break;

                case BondLineStyle.Hatch:
                    DrawHatchBond(wordprocessingGroup, points);
                    break;

                default:
                    DrawBondLine(wordprocessingGroup, bl);
                    break;
            }
        }

        public void DrawBondLine(Wpg.WordprocessingGroup wordprocessingGroup, BondLine bl, string colour = "000000")
        {
            Point startPoint = new Point(bl.Start.X, bl.Start.Y);
            Point endPoint = new Point(bl.End.X, bl.End.Y);
            Rect cmlExtents = bl.BoundingBox;

            // Move Bond Line Extents and Points to have 0,0 Top Left Reference
            startPoint.Offset(-_canvasExtents.Left, -_canvasExtents.Top);
            endPoint.Offset(-_canvasExtents.Left, -_canvasExtents.Top);
            cmlExtents.Offset(-_canvasExtents.Left, -_canvasExtents.Top);

            // Move points into New Bond Line Extents
            startPoint.Offset(-cmlExtents.Left, -cmlExtents.Top);
            endPoint.Offset(-cmlExtents.Left, -cmlExtents.Top);

            switch (bl.Style)
            {
                case BondLineStyle.Solid:
                    DrawSolidLine(wordprocessingGroup, cmlExtents, startPoint, endPoint);
                    break;

                case BondLineStyle.Dotted:
                    DrawDottedLine(wordprocessingGroup, cmlExtents, startPoint, endPoint, colour);
                    break;

                case BondLineStyle.Dashed:
                    DrawDashedLine(wordprocessingGroup, cmlExtents, startPoint, endPoint, colour);
                    break;

                case BondLineStyle.Wavy:
                    DrawWavyLine(wordprocessingGroup, cmlExtents, startPoint, endPoint);
                    break;

                default:
                    DrawSolidLine(wordprocessingGroup, cmlExtents, startPoint, endPoint);
                    break;
            }
        }

        private void DrawWavyLine(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, Point bondStart, Point bondEnd)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "WavyLine" + id;

            Vector bondVector = bondEnd - bondStart;
            int noOfWiggles = (int)Math.Ceiling(bondVector.Length / BondOffset());
            if (noOfWiggles < 1)
            {
                noOfWiggles = 1;
            }

            double wiggleLength = bondVector.Length / noOfWiggles;

            Vector originalWigglePortion = bondVector;
            originalWigglePortion.Normalize();
            originalWigglePortion *= wiggleLength / 2;

            Matrix toLeft = new Matrix();
            toLeft.Rotate(-60);
            Matrix toRight = new Matrix();
            toRight.Rotate(60);
            Vector leftVector = originalWigglePortion * toLeft;
            Vector rightVector = originalWigglePortion * toRight;

            List<Point> allpoints = new List<Point>();
            List<List<Point>> allTriangles = new List<List<Point>>();
            List<Point> triangle = new List<Point>();

            Point lastPoint = bondStart;
            allpoints.Add(lastPoint);
            triangle.Add(lastPoint);
            for (int i = 0; i < noOfWiggles; i++)
            {
                Point leftPoint = lastPoint + leftVector;
                allpoints.Add(leftPoint);
                triangle.Add(leftPoint);

                Point midPoint = lastPoint + originalWigglePortion;
                allpoints.Add(midPoint);
                triangle.Add(midPoint);
                allTriangles.Add(triangle);
                triangle = new List<Point>();
                triangle.Add(midPoint);

                Point rightPoint = lastPoint + originalWigglePortion + rightVector;
                allpoints.Add(rightPoint);
                triangle.Add(rightPoint);

                lastPoint += originalWigglePortion * 2;
                allpoints.Add(lastPoint);
                triangle.Add(lastPoint);
                allTriangles.Add(triangle);
                triangle = new List<Point>();
                triangle.Add(lastPoint);
            }

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (Point p in allpoints)
            {
                maxX = Math.Max(p.X + cmlExtents.Left, maxX);
                minX = Math.Min(p.X + cmlExtents.Left, minX);
                maxY = Math.Max(p.Y + cmlExtents.Top, maxY);
                minY = Math.Min(p.Y + cmlExtents.Top, minY);
            }

            Rect newExtents = new Rect(minX, minY, maxX - minX, maxY - minY);
            double xOffset = cmlExtents.Left - newExtents.Left;
            double yOffset = cmlExtents.Top - newExtents.Top;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(newExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(newExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(newExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(newExtents.Left);

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id, Name = bondLineName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(bondStart.X + xOffset).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(bondStart.Y + yOffset).ToString() };
            moveTo.Append(point1);
            path.Append(moveTo);

            // Render as Curves
            foreach (var tri in allTriangles)
            {
                A.CubicBezierCurveTo cubicBezierCurveTo = new A.CubicBezierCurveTo();
                foreach (var p in tri)
                {
                    A.Point point = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(p.X + xOffset).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(p.Y + yOffset).ToString() };
                    cubicBezierCurveTo.Append(point);
                }
                path.Append(cubicBezierCurveTo);
            }

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int) OoXmlHelper.ACS_LINE_WIDTH_EMUS) , CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = "000000" };
            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);
        }

        private void DrawWedgeBond(Wpg.WordprocessingGroup wordprocessingGroup, List<Point> points)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string atomLabelName = "WedgeBond" + id;

            Rect cmlExtents = new Rect(points[0], points[points.Count - 1]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(points[i], points[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_canvasExtents.Left, -_canvasExtents.Top);

            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Y);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.X);
            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id, Name = atomLabelName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(points[0]));
            path.Append(moveTo);

            for (int i = 1; i < points.Count; i++)
            {
                A.LineTo lineTo = new A.LineTo();
                lineTo.Append(MakePoint(points[i]));
                path.Append(lineTo);
            }

            A.CloseShapePath closeShapePath = new A.CloseShapePath();
            path.Append(closeShapePath);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            // Set shape fill colour
            A.SolidFill solidFill1 = new A.SolidFill();
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = "000000" };
            solidFill1.Append(rgbColorModelHex);

            // Set shape outline colour
            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.ACS_LINE_WIDTH_EMUS), CapType = A.LineCapValues.Round };
            A.RgbColorModelHex rgbColorModelHex2 = new A.RgbColorModelHex { Val = "000000" };
            A.SolidFill solidFill2 = new A.SolidFill();
            solidFill2.Append(rgbColorModelHex2);
            outline.Append(solidFill2);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(solidFill1);
            shapeProperties.Append(outline);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);

            // Local Function
            A.Point MakePoint(Point pp)
            {
                pp.Offset(-_canvasExtents.Left, -_canvasExtents.Top);
                pp.Offset(-cmlExtents.Left, -cmlExtents.Top);
                return new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCmlToEmu(pp.X)}",
                    Y = $"{OoXmlHelper.ScaleCmlToEmu(pp.Y)}"
                };
            }
        }

        private void DrawHatchBond(Wpg.WordprocessingGroup wordprocessingGroup, List<Point> points, string colour = "000000")
        {
            Point wedgeStart = points[0];
            Point wedgeEndMiddle = points[2];

            // Draw a small circle for the starting point
            var xx = 0.5;
            Rect bb = new Rect(new Point(points[0].X - xx, points[0].Y - xx), new Point(points[0].X + xx, points[0].Y + xx));
            DrawShape(wordprocessingGroup, bb, A.ShapeTypeValues.Ellipse, colour);

            // Vector pointing from wedgeStart to wedgeEndMiddle
            Vector direction = wedgeEndMiddle - wedgeStart;
            Matrix rightAngles = new Matrix();
            rightAngles.Rotate(90);
            Vector perpendicular = direction * rightAngles;

            Vector step = direction;
            step.Normalize();
            step *= 15 * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE;

            int steps = (int)Math.Ceiling(direction.Length / step.Length);
            double stepLength = direction.Length / steps;

            step.Normalize();
            step *= stepLength;

            Point p0 = wedgeStart + step;
            Point p1 = p0 + perpendicular;
            Point p2 = p0 - perpendicular;

            var r = GeometryTool.ClipLineWithPolygon(p1, p2, points, out _);
            while (r.Length > 2)
            {
                if (r.Length == 4)
                {
                    DrawBondLine(wordprocessingGroup, new BondLine(BondLineStyle.Solid, r[1], r[2]));
                }

                if (r.Length == 6)
                {
                    DrawBondLine(wordprocessingGroup, new BondLine(BondLineStyle.Solid, r[1], r[2]));
                    DrawBondLine(wordprocessingGroup, new BondLine(BondLineStyle.Solid, r[3], r[4]));
                }

                p0 = p0 + step;
                p1 = p0 + perpendicular;
                p2 = p0 - perpendicular;

                r = GeometryTool.ClipLineWithPolygon(p1, p2, points, out _);
            }

            // Draw Tail Lines
            DrawBondLine(wordprocessingGroup, new BondLine(BondLineStyle.Solid, wedgeEndMiddle, points[1]));
            DrawBondLine(wordprocessingGroup, new BondLine(BondLineStyle.Solid, wedgeEndMiddle, points[3]));
        }

        private void DrawSolidLine(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, Point bondStart, Point bondEnd, string colour = "000000")
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "BondLine" + id;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id, Name = bondLineName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(bondStart.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(bondStart.Y).ToString() };

            moveTo.Append(point1);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(bondEnd.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(bondEnd.Y).ToString() };

            lineTo.Append(point2);

            path.Append(moveTo);
            path.Append(lineTo);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.ACS_LINE_WIDTH_EMUS), CapType = A.LineCapValues.Round };
            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);
            outline.Append(solidFill);

            if (_options.ShowBondDirection)
            {
                A.TailEnd tailEnd = new A.TailEnd { Type = A.LineEndValues.Stealth };
                outline.Append(tailEnd);
            }

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);
        }

        private void DrawDottedLine(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, Point bondStart, Point bondEnd, string colour = "000000")
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "DottedBondLine" + id;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id, Name = bondLineName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(bondStart.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(bondStart.Y).ToString() };

            moveTo.Append(point1);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(bondEnd.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(bondEnd.Y).ToString() };

            lineTo.Append(point2);

            path.Append(moveTo);
            path.Append(lineTo);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.ACS_LINE_WIDTH_EMUS), CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.PresetDash presetDash = new A.PresetDash { Val = A.PresetLineDashValues.SystemDot };

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);
            outline.Append(presetDash);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);
        }

        private void DrawDashedLine(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, Point bondStart, Point bondEnd, string colour = "000000")
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "DashedBondLine" + id;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties { Id = id, Name = bondLineName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(bondStart.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(bondStart.Y).ToString() };

            moveTo.Append(point1);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(bondEnd.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(bondEnd.Y).ToString() };

            lineTo.Append(point2);

            path.Append(moveTo);
            path.Append(lineTo);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            A.Outline outline = new A.Outline { Width = Int32Value.FromInt32((int)OoXmlHelper.ACS_LINE_WIDTH_EMUS), CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();
            A.PresetDash presetDash = new A.PresetDash { Val = A.PresetLineDashValues.SystemDash };

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);
            outline.Append(presetDash);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);
        }

        private void DrawShape(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, A.ShapeTypeValues shape, string colour)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "shape" + id;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(left, top);
            Size size = new Size(width, height);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_canvasExtents.Left), OoXmlHelper.ScaleCmlToEmu(-_canvasExtents.Top));
            Rect boundingBox = new Rect(location, size);

            width = (Int64Value)boundingBox.Width;
            height = (Int64Value)boundingBox.Height;
            top = (Int64Value)boundingBox.Top;
            left = (Int64Value)boundingBox.Left;

            A.PresetGeometry presetGeometry = null;
            A.Extents extents = new A.Extents { Cx = width, Cy = height };
            presetGeometry = new A.PresetGeometry { Preset = shape };

            Wps.WordprocessingShape wordprocessingShape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties
            {
                Id = id,
                Name = bondLineName
            };

            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.AdjustValueList adjustValueList = new A.AdjustValueList();

            presetGeometry.Append(adjustValueList);
            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(presetGeometry);
            shapeProperties.Append(solidFill);

            OoXmlHelper.AppendShapeStyle(wordprocessingShape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(wordprocessingShape);
        }

        private List<Point> MovePointsBy(List<Point> points, double dx, double dy)
        {
            List<Point> result = new List<Point>();

            foreach (var p in points)
            {
                var pp = new Point(p.X, p.Y);
                pp.Offset(dx, dy);
                result.Add(pp);
            }

            return result;
        }

        private double BondOffset()
        {
            return (_medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE);
        }
    }
}