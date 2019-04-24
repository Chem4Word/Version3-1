// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
                var mode = Application.Current.ShutdownMode;

                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var owner = Application.Current.MainWindow;

                var atom = av.ParentAtom;
                var model = new AtomPropertiesModel();
                model.Title = atom.Path;
                model.Symbol = atom.Element.Symbol;
                model.Charge = atom.FormalCharge.ToString();
                model.Isotope = atom.IsotopeNumber.ToString();
                model.Centre = pp;

                var tcs = new TaskCompletionSource<bool>();

                Application.Current.Dispatcher.Invoke(() =>
                                                      {
                                                          try
                                                          {
                                                              var pe = new AtomPropertyEditor(model, owner);
                                                              pe.ShowDialog();
                                                          }
                                                          finally
                                                          {
                                                              tcs.TrySetResult(true);
                                                          }
                                                      });

                Application.Current.ShutdownMode = mode;

                if (model.Save)
                {
                    EditViewModel evm = (EditViewModel)((EditorCanvas)av.Parent).Chemistry;
                    evm.UpdateAtom(atom, model);
                }
            }

            if (ActiveVisual is BondVisual bv)
            {
                var mode = Application.Current.ShutdownMode;

                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var owner = Application.Current.MainWindow;

                var bond = bv.ParentBond;
                var model = new BondPropertiesModel();
                model.Title = bond.Path;
                model.Order = bond.Order;

                if (bond.OrderValue == 2.0)
                {
                    model.IsDouble = true;
                }

                model.PlacementChoice = PlacementChoice.Auto;
                if (model.IsDouble)
                {
                    if (bond.ExplicitPlacement != null)
                    {
                        model.PlacementChoice = (PlacementChoice)bond.ExplicitPlacement.Value;
                    }

                }
                model.Stereo = Globals.GetStereoString(bond.Stereo);
                model.Angle = bond.Angle;
                model.Centre = pp;

                var tcs = new TaskCompletionSource<bool>();

                Application.Current.Dispatcher.Invoke(() =>
                                                      {
                                                          try
                                                          {
                                                              var pe = new BondPropertyEditor(model, owner);
                                                              pe.ShowDialog();
                                                          }
                                                          finally
                                                          {
                                                              tcs.TrySetResult(true);
                                                          }
                                                      });

                Application.Current.ShutdownMode = mode;

                if (model.Save)
                {
                    EditViewModel evm = (EditViewModel)((EditorCanvas)bv.Parent).Chemistry;
                    evm.UpdateBond(bond, model);
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


        public AtomVisual GetAtomVisual(Atom adornedAtom)
        {
           return chemicalVisuals[adornedAtom] as AtomVisual;
        }
    }
}