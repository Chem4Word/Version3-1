// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Chem4Word.Model2.Geometry.BasicGeometry;
namespace Chem4Word.ACME.Controls
{
    public class EditorCanvas : ChemistryCanvas
    {
        /// <summary>
        /// Doesn't autosize the chemistry to fit, unlike the display
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            return DesiredSize;
        }
        public Rect GetMoleculeBoundingBox(Molecule mol)
        {
            Rect union = Rect.Empty;
            var atomList = new List<Atom>();

            mol.BuildAtomList(atomList);
            foreach (Atom atom in atomList )
            {
                union.Union(chemicalVisuals[atom].ContentBounds);
            }

            return union;
        }

        

        public Geometry GhostMolecule(Molecule adornedMolecule)
        {
            CombinedGeometry cg = null;

            var atomList = new List<Atom>();
            List<Bond> bondList = new List<Bond>();
            adornedMolecule.BuildAtomList(atomList);
            adornedMolecule.BuildBondList(bondList);
            foreach (Atom atom in atomList)
            {
                CombineGeometries(chemicalVisuals[atom].Drawing, GeometryCombineMode.Union, ref cg);
            }
            foreach (Bond bond in bondList)
            {
                CombineGeometries(chemicalVisuals[bond].Drawing, GeometryCombineMode.Union, ref cg);
            }

            return cg;
        }
    }
   
}
