// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Enums;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class BondLine
    {
        public Bond Bond { get; }

        public string ParentMolecule => Bond != null ? Bond.Parent.Path : string.Empty;

        public string ParentBond => Bond != null ? Bond.Path : string.Empty;

        public string StartAtomPath => Bond != null ? Bond.StartAtom.Path : string.Empty;

        public string EndAtomPath => Bond != null ? Bond.EndAtom.Path : string.Empty;

        public BondLineStyle Style { get; private set; }

        public string Colour { get; set; } = "000000";

        public Point Start { get; set; }

        public Point End { get; set; }

        private Rect _boundingBox = Rect.Empty;

        public Rect BoundingBox
        {
            get
            {
                if (_boundingBox.IsEmpty)
                {
                    _boundingBox = new Rect(Start, End);
                }

                return _boundingBox;
            }
        }

        public BondLine(BondLineStyle style, Bond bond)
        {
            Style = style;
            Bond = bond;

            if (bond != null)
            {
                Start = bond.StartAtom.Position;
                End = bond.EndAtom.Position;
            }
        }

        public BondLine(BondLineStyle style, Point startPoint, Point endPoint)
        {
            Style = style;
            Start = startPoint;
            End = endPoint;
        }

        public BondLine(BondLineStyle style, Point startPoint, Point endPoint, Bond bond)
        {
            Style = style;
            Start = startPoint;
            End = endPoint;
            Bond = bond;
        }

        public BondLine GetParallel(double offset)
        {
            double xDifference = Start.X - End.X;
            double yDifference = Start.Y - End.Y;
            double length = Math.Sqrt(Math.Pow(xDifference, 2) + Math.Pow(yDifference, 2));

            Point newStartPoint = new Point((float)(Start.X - offset * yDifference / length),
                                            (float)(Start.Y + offset * xDifference / length));
            Point newEndPoint = new Point((float)(End.X - offset * yDifference / length),
                                          (float)(End.Y + offset * xDifference / length));

            return new BondLine(Style, newStartPoint, newEndPoint, Bond)
                   {
                       Colour = Colour
                   };
        }

        public void SetLineStyle(BondLineStyle style)
        {
            Style = style;
        }

        private string PointAsString(Point p)
        {
            return $"{p.X.ToString("#,##0.0000")},{p.Y.ToString("#,##0.0000")}";
        }

        public override string ToString()
        {
            string result = $"{Style} from {PointAsString(Start)} to {PointAsString(End)}";
            if (Bond != null)
            {
                result += $" [{Bond}]";
            }

            return result;
        }
    }
}