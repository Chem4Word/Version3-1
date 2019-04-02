// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.Model2.Helpers;
using static Chem4Word.ACME.Drawing.BondVisual;
using static Chem4Word.Model2.Geometry.BasicGeometry;

namespace Chem4Word.ACME.Controls
{
    public class EditorCanvas : ChemistryCanvas
    {
        #region Constructors

        public EditorCanvas() : base()
        {
            MouseRightButtonUp += OnMouseRightButtonUp;
        }

        #endregion Constructors

        #region Event handlers

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var pp = PointToScreen(e.GetPosition(this));

            ActiveVisual = GetTargetedVisual(e.GetPosition(this));

            if (ActiveVisual is AtomVisual av)
            {
                var atom = av.ParentAtom;
                //MessageBox.Show($"Right Click on {atom}");
                AtomPropertyEditor pe = new AtomPropertyEditor();
                var model = new AtomPropertiesModel();
                model.Symbol = atom.Element.Symbol;
                model.Title = atom.Path;
                model.Centre = pp;
                pe.ShowDialog(model);
                if (model.Save)
                {
                    atom.Element = Globals.PeriodicTable[model.Symbol] as ElementBase;
                }
            }

            if (ActiveVisual is BondVisual bv)
            {
                var bond = bv.ParentBond;
                //MessageBox.Show($"Right Click on {bond}");
                BondPropertyEditor pe = new BondPropertyEditor();
                var model = new BondPropertiesModel();
                model.Order = bond.Order;
                model.Title = bond.Path;
                model.Centre = pp;
                pe.ShowDialog(model);
                if (model.Save)
                {
                    bond.Order = model.Order;
                }
            }
        }

        #endregion Event handlers

        #region Methods

        public Rect GetMoleculeBoundingBox(Molecule mol)
        {
            Rect union = Rect.Empty;
            var atomList = new List<Atom>();

            mol.BuildAtomList(atomList);
            foreach (Atom atom in atomList)
            {
                union.Union(chemicalVisuals[atom].ContentBounds);
            }

            return union;
        }

        public Rect GetMoleculeBoundingBox(List<Molecule> adornedMolecules)
        {
            Rect union = Rect.Empty;
            foreach (Molecule molecule in adornedMolecules)
            {
                union.Union(GetMoleculeBoundingBox(molecule));
            }

            return union;
        }

        public Geometry GhostMolecule(List<Molecule> adornedMolecules)
        {
            var atomList = new List<Atom>();
            List<Bond> bondList = new List<Bond>();
            foreach (Molecule mol in adornedMolecules)
            {
                mol.BuildAtomList(atomList);
                mol.BuildBondList(bondList);
            }

            StreamGeometry ghostGeometry = new StreamGeometry();

            double atomRadius = this.Chemistry.Model.XamlBondLength / 7.50;
            using (StreamGeometryContext ghostContext = ghostGeometry.Open())
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
                    List<Point> throwaway = new List<Point>();
                    bool ok = GetBondGeometry(bond.StartAtom.Position, bond.EndAtom.Position,
                        lookups[bond.StartAtom], lookups[bond.EndAtom], this.Chemistry.Model.XamlBondLength,
                        out Geometry bondGeom, bond, ref throwaway);
                    DrawGeometry(ghostContext, bondGeom);
                }
                ghostContext.Close();
            }

            return ghostGeometry;
        }

        #endregion Methods

        #region Overrides

        /// <summary>
        /// Doesn't autosize the chemistry to fit, unlike the display
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            return DesiredSize;
        }

        #endregion Overrides
    }
}