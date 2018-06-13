// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using Chem4Word.Model;
using Chem4Word.Model.Geometry;
using System.Windows;
using static Chem4Word.ViewModel.DisplayViewModel;

namespace Chem4Word.ViewModel
{
    public static class ExtensionMethods
    {
        public static Rect BoundingBox(this Bond bond)
        {
            return new Rect(bond.StartAtom.Position, bond.EndAtom.Position);
        }

        // ToDo: Clyde - Why does this exist in TWO places, but with different signatures ???
        // ToDo: Duplicated Routine
        // HACK: Duplicated Routine

        //tries to get a estimated bounding box for each atom symbol
        //public static Rect BoundingBox(this Atom atom)
        //{
        //    //Debug.WriteLine($"ExtensionMethods.BoundingBox() FontSize: {FontSize}");
        //    double halfBoxSize = FontSize * 0.5;
        //    Point position = atom.Position;
        //    Rect baseAtomBox = new Rect(
        //        new Point(position.X - halfBoxSize, position.Y - halfBoxSize),
        //        new Point(position.X + halfBoxSize, position.Y + halfBoxSize));

        //    if (atom.SymbolText != "")
        //    {
        //        double symbolWidth = atom.SymbolText.Length * FontSize;
        //        Rect mainElementBox = new Rect(
        //            new Point(position.X - symbolWidth/2, position.Y - halfBoxSize),
        //            new Size(symbolWidth, FontSize));

        //        if (atom.ImplicitHydrogenCount > 0)
        //        {
        //            Vector shift = new Vector();
        //            Rect hydrogenBox = baseAtomBox;
        //            switch (atom.GetDefaultHOrientation())
        //            {
        //                case CompassPoints.East:
        //                    shift = BasicGeometry.ScreenEast * FontSize;
        //                    break;

        //                case CompassPoints.North:
        //                    shift = BasicGeometry.ScreenNorth * FontSize;
        //                    break;

        //                case CompassPoints.South:
        //                    shift = BasicGeometry.ScreenSouth * FontSize;
        //                    break;

        //                case CompassPoints.West:
        //                    shift = BasicGeometry.ScreenWest * FontSize;
        //                    break;
        //            }
        //            hydrogenBox.Offset(shift);
        //            mainElementBox.Union(hydrogenBox);
        //        }
        //        //Debug.WriteLine($"ExtensionMethods.BoundingBox() {atom.SymbolText} mainElementBox: {mainElementBox}");
        //        return mainElementBox;
        //    }
        //    else
        //    {
        //        //Debug.WriteLine($"ExtensionMethods.BoundingBox() {atom.SymbolText} baseAtomBox: {baseAtomBox}");
        //        return baseAtomBox;
        //    }
        //}
    }
}