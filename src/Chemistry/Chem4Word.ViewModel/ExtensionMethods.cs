// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Geometry;
using System.Windows;

namespace Chem4Word.ViewModel
{
    public static class ExtensionMethods
    {
        //tries to get a bounding box for each atom symbol
        public static Rect BoundingBox(this Atom atom)
        {
            if (atom.SymbolText != "")
            {
                double halfSize = ViewModel.FontSize / 2;
                Point position = atom.Position;
                Rect baseAtomBox = new Rect(
                    new Point(position.X - halfSize, position.Y - halfSize),
                    new Point(position.X + halfSize, position.Y + halfSize));
                double symbolWidth = atom.SymbolText.Length * ViewModel.FontSize * 0.8;
                Rect mainElementBox = new Rect(new Point(position.X - halfSize, position.Y - halfSize),
                    new Size(symbolWidth, ViewModel.FontSize));

                if (atom.ImplicitHydrogenCount > 0)

                {
                    Vector shift = new Vector();
                    Rect hydrogenBox = baseAtomBox;
                    switch (atom.GetDefaultHOrientation())
                    {
                        case CompassPoints.East:
                            shift = BasicGeometry.ScreenEast * ViewModel.FontSize;
                            break;

                        case CompassPoints.North:
                            shift = BasicGeometry.ScreenNorth * ViewModel.FontSize;
                            break;

                        case CompassPoints.South:
                            shift = BasicGeometry.ScreenSouth * ViewModel.FontSize;
                            break;

                        case CompassPoints.West:
                            shift = BasicGeometry.ScreenWest * ViewModel.FontSize;
                            break;
                    }
                    hydrogenBox.Offset(shift);
                    mainElementBox.Union(hydrogenBox);
                }
                return mainElementBox;
            }
            else
            {
                return new Rect(atom.Position, atom.Position);//empty rect
            }
        }
    }
}