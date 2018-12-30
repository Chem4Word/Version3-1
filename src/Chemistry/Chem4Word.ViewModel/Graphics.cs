// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using System.Windows.Media;

namespace Chem4Word.ViewModel
{
    public static class Graphics
    {
        /// <summary>
        /// Returns a ghost image of the molecule for dragging, resizing and rotating
        /// </summary>
        /// <param name="mol">optional List of coordinates by atoms that have become displaced by an operation</param>
        /// <returns></returns>
        public static System.Windows.Media.Geometry Ghost(this Molecule mol)
        {
            System.Windows.Media.GeometryGroup ghostGeometry = new GeometryGroup();
            foreach (Bond b in mol.Bonds)
            {
                ghostGeometry.Children.Add(BondGeometry.SingleBondGeometry(b.StartAtom.Position, b.EndAtom.Position));
            }
            //ghostGeometry.Freeze();
            return ghostGeometry;
        }
    }
}