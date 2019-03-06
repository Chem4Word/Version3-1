// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2.Geometry;
using static Chem4Word.Model2.Geometry.BasicGeometry;
using static  Chem4Word.ACME.Drawing.BondVisual;
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
            var atomList = new List<Atom>();
            List<Bond> bondList = new List<Bond>();
            adornedMolecule.BuildAtomList(atomList);
            adornedMolecule.BuildBondList(bondList);
            StreamGeometry ghostGeometry = new StreamGeometry();

            double atomRadius = this.Chemistry.Model.XamlBondLength / 7.50;
            using(StreamGeometryContext ghostContext = ghostGeometry.Open())
            {
                Dictionary<Atom, Geometry> lookups = new Dictionary<Atom, Geometry>();
                foreach (Atom atom in atomList)
                {
                    if (atom.SymbolText != "")

                    {
                        EllipseGeometry atomCircle = new EllipseGeometry(atom.Position, atomRadius, atomRadius);
                        DrawGeometry(ghostContext, atomCircle);
                        lookups[atom] = atomCircle;
                    }
                    else
                    {
                        lookups[atom] = Geometry.Empty;
                    }
                }
                foreach (Bond bond in bondList)
                {
                    List<Point> throwaway= new List<Point>();
                    bool ok = GetBondGeometry(bond.StartAtom.Position, bond.EndAtom.Position,
                        lookups[bond.StartAtom], lookups[bond.EndAtom], this.Chemistry.Model.XamlBondLength,
                        out Geometry bondGeom, bond, ref throwaway);
                    DrawGeometry(ghostContext,bondGeom);

                }
                ghostContext.Close();

            }

            return ghostGeometry;
        }
    }
   
}
