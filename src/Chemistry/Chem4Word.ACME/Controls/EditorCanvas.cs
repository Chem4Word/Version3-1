// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Chem4Word.ACME.Drawing.BondVisual;
using static Chem4Word.Model2.Geometry.BasicGeometry;

namespace Chem4Word.ACME.Controls

{
    public class EditorCanvas : ChemistryCanvas
    {
        #region Fields

        private Adorner _highlightAdorner;

        #endregion Fields

        #region Constructors

        public EditorCanvas() : base()
        {
            this.MouseMove += EditorCanvas_MouseMove;
        }

        #endregion Constructors

        #region Methods

        private ChemicalVisual GetTargetedVisual(Point p)
        {
            return (VisualTreeHelper.HitTest(this, p).VisualHit as ChemicalVisual);
        }

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

        public Geometry GhostMolecule(Molecule adornedMolecule)
        {
            CombinedGeometry cg = null;

            var atomList = new List<Atom>();
            List<Bond> bondList = new List<Bond>();
            adornedMolecule.BuildAtomList(atomList);
            adornedMolecule.BuildBondList(bondList);
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

        #region Event Handlers

        private void EditorCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            ChemicalVisual cv = GetTargetedVisual(e.GetPosition(this));

            if (_highlightAdorner != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(this);
                layer.Remove(_highlightAdorner);
                _highlightAdorner = null;
            }
            if (cv is AtomVisual av)
            {
                _highlightAdorner = new AtomHoverAdorner(this, av);
            }
            else if (cv is BondVisual bv)
            {
                _highlightAdorner = new BondHoverAdorner(this, bv);
            }
            else
            {
                _highlightAdorner = null;
            }
        }

        #endregion Event Handlers
    }
}