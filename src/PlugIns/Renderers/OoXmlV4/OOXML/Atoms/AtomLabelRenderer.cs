// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using Chem4Word.Renderer.OoXmlV4.TTF;
using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using Point = System.Windows.Point;
using Wpg = DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using Wps = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Chem4Word.Renderer.OoXmlV4.OOXML.Atoms
{
    public class AtomLabelRenderer
    {
        private Rect _canvasExtents;
        private long _ooxmlId;
        private Options _options;
        private double _meanBondLength;

        public AtomLabelRenderer(Rect canvasExtents, ref long ooxmlId, Options opts, double meanBondLength)
        {
            _canvasExtents = canvasExtents;
            _ooxmlId = ooxmlId;
            _options = opts;
            _meanBondLength = meanBondLength;
        }

        public void DrawCharacter(Wpg.WordprocessingGroup wordprocessingGroup, AtomLabelCharacter alc)
        {
            Point characterPosition = new Point(alc.Position.X, alc.Position.Y);
            characterPosition.Offset(-_canvasExtents.Left, -_canvasExtents.Top);

            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string atomLabelName = "Atom " + alc.ParentAtom;

            Int64Value width = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Width, _meanBondLength);
            Int64Value height = OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height, _meanBondLength);
            if (alc.IsSmaller)
            {
                width = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Width, _meanBondLength);
                height = OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height, _meanBondLength);
            }
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(characterPosition.Y);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(characterPosition.X);

            // Set variable true to show bounding box of (every) character
            if (_options.ShowCharacterBoundingBoxes)
            {
                Rect boundingBox = new Rect(new Point(left, top), new Size(width, height));
                DrawCharacterBox(wordprocessingGroup, boundingBox, "00ff00", 0.25);
            }

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties() { Id = id, Name = atomLabelName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry geometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            foreach (TtfContour contour in alc.Character.Contours)
            {
                int i = 0;

                while (i < contour.Points.Count)
                {
                    TtfPoint thisPoint = contour.Points[i];
                    TtfPoint nextPoint = null;
                    if (i < contour.Points.Count - 1)
                    {
                        nextPoint = contour.Points[i + 1];
                    }

                    switch (thisPoint.Type)
                    {
                        case TtfPoint.PointType.Start:
                            A.MoveTo moveTo = new A.MoveTo();
                            if (alc.IsSmaller)
                            {
                                A.Point point = MakeSubscriptPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            else
                            {
                                A.Point point = MakeNormalPoint(thisPoint);
                                moveTo.Append(point);
                                path.Append(moveTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.Line:
                            A.LineTo lineTo = new A.LineTo();
                            if (alc.IsSmaller)
                            {
                                A.Point point = MakeSubscriptPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            else
                            {
                                A.Point point = MakeNormalPoint(thisPoint);
                                lineTo.Append(point);
                                path.Append(lineTo);
                            }
                            i++;
                            break;

                        case TtfPoint.PointType.CurveOff:
                            A.QuadraticBezierCurveTo quadraticBezierCurveTo = new A.QuadraticBezierCurveTo();
                            if (alc.IsSmaller)
                            {
                                A.Point pointA = MakeSubscriptPoint(thisPoint);
                                A.Point pointB = MakeSubscriptPoint(nextPoint);
                                quadraticBezierCurveTo.Append(pointA);
                                quadraticBezierCurveTo.Append(pointB);
                                path.Append(quadraticBezierCurveTo);
                            }
                            else
                            {
                                A.Point pointA = MakeNormalPoint(thisPoint);
                                A.Point pointB = MakeNormalPoint(nextPoint);
                                quadraticBezierCurveTo.Append(pointA);
                                quadraticBezierCurveTo.Append(pointB);
                                path.Append(quadraticBezierCurveTo);
                            }
                            i++;
                            i++;
                            break;

                        case TtfPoint.PointType.CurveOn:
                            // Should never get here !
                            i++;
                            break;
                    }
                }

                A.CloseShapePath closeShapePath = new A.CloseShapePath();
                path.Append(closeShapePath);
            }

            pathList.Append(path);

            geometry.Append(adjustValueList);
            geometry.Append(rectangle);
            geometry.Append(pathList);

            A.SolidFill solidFill = new A.SolidFill();

            // Set Colour
            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = alc.Colour };
            solidFill.Append(rgbColorModelHex);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(geometry);
            shapeProperties.Append(solidFill);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);

            // Local Functions
            A.Point MakeSubscriptPoint(TtfPoint ttfPoint)
            {
                A.Point pp = new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(ttfPoint.X - alc.Character.OriginX, _meanBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfSubScriptToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _meanBondLength)}"
                };
                return pp;
            }

            A.Point MakeNormalPoint(TtfPoint ttfPoint)
            {
                A.Point pp = new A.Point
                {
                    X = $"{OoXmlHelper.ScaleCsTtfToEmu(ttfPoint.X - alc.Character.OriginX, _meanBondLength)}",
                    Y = $"{OoXmlHelper.ScaleCsTtfToEmu(alc.Character.Height + ttfPoint.Y - (alc.Character.Height + alc.Character.OriginY), _meanBondLength)}"
                };
                return pp;
            }
        }

        private void DrawCharacterBox(Wpg.WordprocessingGroup wordprocessingGroup, Rect boxExtents, string colour, double points)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "char-diag-box-" + id;

            Int64Value width = (Int64Value)boxExtents.Width;
            Int64Value height = (Int64Value)boxExtents.Height;
            Int64Value top = (Int64Value)boxExtents.Top;
            Int64Value left = (Int64Value)boxExtents.Left;

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties()
            {
                Id = id,
                Name = bondLineName
            };
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

            // Starting Point
            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = "0", Y = "0" };
            moveTo.Append(point1);

            // Mid Point
            A.LineTo lineTo1 = new A.LineTo();
            A.Point point2 = new A.Point { X = boxExtents.Width.ToString("0"), Y = "0" };
            lineTo1.Append(point2);

            // Mid Point
            A.LineTo lineTo2 = new A.LineTo();
            A.Point point3 = new A.Point { X = boxExtents.Width.ToString("0"), Y = boxExtents.Height.ToString("0") };
            lineTo2.Append(point3);

            // Last Point
            A.LineTo lineTo3 = new A.LineTo();
            A.Point point4 = new A.Point { X = "0", Y = boxExtents.Height.ToString("0") };
            lineTo3.Append(point4);

            // Back to Start Point
            A.LineTo lineTo4 = new A.LineTo();
            A.Point point5 = new A.Point { X = "0", Y = "0" };
            lineTo4.Append(point5);

            path.Append(moveTo);
            path.Append(lineTo1);
            path.Append(lineTo2);
            path.Append(lineTo3);
            path.Append(lineTo4);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            Int32Value emus = (Int32Value)(points * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emus, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);
        }
    }
}