// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using Wps = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public static class OoXmlHelper
    {
        // https://startbigthinksmall.wordpress.com/2010/02/05/unit-converter-and-specification-search-for-ooxmlwordml-development/
        // http://lcorneliussen.de/raw/dashboards/ooxml/

        // Margins are in CML Points
        public const double DRAWING_MARGIN = 5; // 5 is a good value to use (Use 0 to compare with AMC diagrams)

        public const double CHARACTER_CLIPPING_MARGIN = 1.25;   // cml pixels
        public const double CHARACTER_VERTICAL_SPACING = 1.25;  // cml pixels

        // Percentage of average (median) bond length
        public const double MULTIPLE_BOND_OFFSET_PERCENTAGE = 0.18;

        public const double SUBSCRIPT_SCALE_FACTOR = 0.6;
        public const double SUBSCRIPT_DROP_FACTOR = 0.75;
        public const double CS_SUPERSCRIPT_RAISE_FACTOR = 0.3;

        public const int EMUS_PER_WORD_POINT = 12700;
        public const double ACS_LINE_WIDTH = 0.6;
        public const int ACS_LINE_WIDTH_EMUS = 7620;    // This makes bond line width equal to ACS Guide of 0.6pt
        private const int EMUS_PER_CML_POINT = 9144;    // This makes cml bond length of 20 equal ACS guide 0.2" (0.508cm)

        private const double BRACKET_OFFSET_PERCENTAGE = 0.2;

        // These calculations yield a font which has a point size of 8 at a bond length of 20
        private static double EmusPerCsTtfPoint(double bondLength)
        {
            return bondLength / 2.5;
        }

        private static double EmusPerCsTtfPointSubscript(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return EmusPerCsTtfPoint(bondLength) * SUBSCRIPT_SCALE_FACTOR;
            }
            else
            {
                return EmusPerCsTtfPoint(20) * SUBSCRIPT_SCALE_FACTOR;
            }
        }

        private static double CsTtfToCml(double bondLength)
        {
            if (bondLength > 0.1)
            {
                return EMUS_PER_CML_POINT / EmusPerCsTtfPoint(bondLength);
            }
            else
            {
                return EMUS_PER_CML_POINT / EmusPerCsTtfPoint(20);
            }
        }

        /// <summary>
        /// Scales a CML X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <returns></returns>
        public static Int64Value ScaleCmlToEmu(double XorY)
        {
            double scaled = XorY * EMUS_PER_CML_POINT;
            return Int64Value.FromInt64((long)scaled);
        }

        public static void AppendShapeStyle(Wps.WordprocessingShape shape,
                                            Wps.NonVisualDrawingProperties nonVisualDrawingProperties,
                                            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties,
                                            Wps.ShapeProperties shapeProperties)
        {
            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            shape.Append(nonVisualDrawingProperties);
            shape.Append(nonVisualDrawingShapeProperties);
            shape.Append(shapeProperties);
            shape.Append(shapeStyle);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            shape.Append(textBodyProperties);
        }

        #region C# TTF

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        /// <param name="XorY"></param>
        /// <param name="bondLength"></param>
        /// <returns></returns>
        public static Int64Value ScaleCsTtfToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                double scaled = XorY * EmusPerCsTtfPoint(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                double scaled = XorY * EmusPerCsTtfPoint(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        /// <summary>
        /// Scales a CS TTF SubScript X or Y co-ordinate to DrawingML Units (EMU)
        /// </summary>
        public static Int64Value ScaleCsTtfSubScriptToEmu(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                double scaled = XorY * EmusPerCsTtfPointSubscript(bondLength);
                return Int64Value.FromInt64((long)scaled);
            }
            else
            {
                double scaled = XorY * EmusPerCsTtfPointSubscript(20);
                return Int64Value.FromInt64((long)scaled);
            }
        }

        /// <summary>
        /// Scales a C# TTF X or Y co-ordinate to CML Units
        /// </summary>
        /// <param name="XorY"></param>
        /// <returns></returns>
        public static double ScaleCsTtfToCml(double XorY, double bondLength)
        {
            if (bondLength > 0.1)
            {
                return XorY / CsTtfToCml(bondLength);
            }
            else
            {
                return XorY / CsTtfToCml(20);
            }
        }

        public static double BracketOffset(double bondLength)
        {
            return bondLength * BRACKET_OFFSET_PERCENTAGE;
        }

        #endregion C# TTF
    }
}