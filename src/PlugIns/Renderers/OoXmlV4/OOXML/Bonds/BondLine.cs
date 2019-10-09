// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Renderer.OoXmlV4.Enums;

namespace Chem4Word.Renderer.OoXmlV4.OOXML.Bonds
{
    public class BondLine
    {
        public Bond Bond { get; }

        public string ParentMolecule
        {
            get
            {
                if (Bond != null)
                {
                    return Bond.Parent.Path;
                }

                return string.Empty;
            }
        }

        public string ParentBond
        {
            get
            {
                if (Bond != null)
                {
                    return Bond.Path;
                }

                return string.Empty;
            }
        }

        public string StartAtomPath
        {
            get
            {
                if (Bond != null)
                {
                    return Bond.StartAtom.Path;
                }

                return string.Empty;
            }
        }

        public string EndAtomPath
        {
            get
            {
                if (Bond != null)
                {
                    return Bond.EndAtom.Path;
                }

                return string.Empty;
            }
        }

        public BondLineStyle Style { get; private set; }

        private Point _start;

        public Point Start
        {
            get { return _start; }
            set
            {
                _start = value;
            }
        }

        private Point _end;

        public Point End
        {
            get { return _end; }
            set
            {
                _end = value;
            }
        }

        private Rect _boundingBox = Rect.Empty;

        public Rect BoundingBox
        {
            get
            {
                if (_boundingBox.IsEmpty)
                {
                    _boundingBox = new Rect(_start, _end);
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
                _start = bond.StartAtom.Position;
                _end = bond.EndAtom.Position;
            }
        }

        public BondLine(BondLineStyle style, Point startPoint, Point endPoint)
        {
            Style = style;
            _start = startPoint;
            _end = endPoint;
        }

        public BondLine(BondLineStyle style, Point startPoint, Point endPoint, Bond bond)
        {
            Style = style;
            _start = startPoint;
            _end = endPoint;
            Bond = bond;
        }

        public BondLine GetParallel(double offset)
        {
            double xDifference = _start.X - _end.X;
            double yDifference = _start.Y - _end.Y;
            double length = Math.Sqrt(Math.Pow(xDifference, 2) + Math.Pow(yDifference, 2));

            Point newStartPoint = new Point((float)(_start.X - offset * yDifference / length),
                                            (float)(_start.Y + offset * xDifference / length));
            Point newEndPoint = new Point((float)(_end.X - offset * yDifference / length),
                                          (float)(_end.Y + offset * xDifference / length));

            return new BondLine(Style, newStartPoint, newEndPoint, Bond);
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
            string result = $"{Style} from {PointAsString(_start)} to {PointAsString(_end)}";
            if (Bond != null)
            {
                result += $" [{Bond}]";
            }

            return result;
        }
    }
}