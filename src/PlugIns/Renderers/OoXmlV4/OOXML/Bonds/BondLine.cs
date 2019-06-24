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
        private Bond _bond;
        public Bond Bond
        {
            get { return _bond;}
        }

        public string ParentMolecule
        {
            get
            {
                if (_bond != null)
                {
                    return _bond.Parent.Path;
                }

                return string.Empty;
            }
        }

        public string ParentBond
        {
            get
            {
                if (_bond != null)
                {
                    return _bond.Path;
                }

                return string.Empty;
            }
        }

        public string StartAtomPath
        {
            get
            {
                if (_bond != null)
                {
                    return _bond.StartAtom.Path;
                }

                return string.Empty;
            }
        }

        public string EndAtomPath
        {
            get
            {
                if (_bond != null)
                {
                    return _bond.EndAtom.Path;
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
                BoundingBox = new Rect(_start, _end);
            }
        }

        private Point _end;
        public Point End
        {
            get { return _end; }
            set
            {
                _end = value;
                BoundingBox = new Rect(_start, _end);
            }
        }

        public Rect BoundingBox { get; private set; }

        public BondLine(BondLineStyle style, Bond bond)
        {
            Style = style;
            _bond = bond;

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
            _bond = bond;
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

            return new BondLine(Style, newStartPoint, newEndPoint);
        }

        public void SetLineStyle(BondLineStyle style)
        {
            Style = style;
        }
    }
}