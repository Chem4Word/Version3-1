// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using Chem4Word.Model;
using Chem4Word.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chem4Word.ACME.Controls
{
    public class ChemistryCanvas : Canvas
    {
        public ChemistryCanvas()
        {
            chemicalVisuals = new Dictionary<object, DrawingVisual>();
        }

        /// <summary>
        /// called during WPF layout phase
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size size = new Size();

            size = GetOverhang();

            // add margin
            //size.Width += 10;
            //size.Height += 10;
            return size;
        }


        #region Drawing

        #region Properties

        //properties
        private DisplayViewModel _mychemistry;

        public DisplayViewModel Chemistry
        {
            get { return _mychemistry; }
            set
            {
                _mychemistry = value;
                DrawChemistry(_mychemistry);
            }
        }

        #endregion Properties

        #region DPs

        public Thickness Overhang
        {
            get { return (Thickness)GetValue(OverhangProperty); }
            set { SetValue(OverhangProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Overhang.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverhangProperty =
            DependencyProperty.Register("Overhang", typeof(Thickness), typeof(Display), new PropertyMetadata(default(Thickness)));

        #endregion
        #region Methods


        private Size GetOverhang()
        {
            var currentbounds = GetBoundingBox();
            return currentbounds.Size;
        }

        private Rect GetBoundingBox()
        {
            Rect currentbounds = new Rect(new Size(0, 0));

            try
            {
                foreach (DrawingVisual element in chemicalVisuals.Values)
                {
                    var bounds = element.ContentBounds;
                    currentbounds.Union(bounds);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return currentbounds;
        }

        #endregion
        //overrides
        protected override Visual GetVisualChild(int index)
        {
            return chemicalVisuals.ElementAt(index).Value;
        }

        protected override int VisualChildrenCount => chemicalVisuals.Count;

        //bookkeeping collection
        private Dictionary<object, DrawingVisual> chemicalVisuals { get; }

        /// <summary>
        /// Draws the chemistry
        /// </summary>
        /// <param name="vm"></param>
        private void DrawChemistry(DisplayViewModel vm)
        {
            Clear();

            foreach (Molecule molecule in vm.Model.Molecules)
            {
                DrawMolecule(molecule, Rect.Empty);
            }

            var bb = GetBoundingBox();
            Overhang = new Thickness(-Math.Min(0d, bb.Left),Math.Min(0d, bb.Top),0d, 0d);
            InvalidateMeasure();
        }

        /// <summary>
        /// Draws a single molecule - and its children
        /// </summary>
        /// <param name="molecule"></param>
        /// <param name="moleculeBounds"></param>
        /// <returns></returns>
        private Rect DrawMolecule(Molecule molecule, Rect moleculeBounds)
        {
            var bounds = moleculeBounds;

            foreach (Atom moleculeAtom in molecule.Atoms)
            {
                bounds = DrawAtom(moleculeAtom, bounds);
            }

            foreach (Bond moleculeBond in molecule.Bonds)
            {
                bounds = DrawBond(moleculeBond, bounds);
            }

            //check to see if we have child molecules
            if (molecule.Molecules.Any())
            {
                Rect groupRect = Rect.Empty;
                foreach (Molecule child in molecule.Molecules)
                {
                    var childRect = DrawMolecule(child, bounds);

                    groupRect.Union(childRect);
                }

                DrawGroupBox(molecule, groupRect, ref bounds);
            }
            return bounds;
        }

        private void DrawGroupBox(Molecule molecule, Rect groupRect, ref Rect bounds)
        {
            var groupBox = new DrawingVisual();
            using (DrawingContext dc = groupBox.RenderOpen())
            {
                Brush bracketBrush = new SolidColorBrush(Colors.Gray);
                Pen bracketPen = new Pen(bracketBrush, 1d);
                //bracketPen.DashStyle = new DashStyle(new double[]{2,2});
                dc.DrawRectangle(null, bracketPen, groupRect);
                dc.Close();
            }

            AddVisualChild(groupBox);
            AddLogicalChild(groupBox);
            chemicalVisuals.Add(molecule, groupBox);

            bounds.Union(groupBox.ContentBounds);
        }

        private Rect DrawAtom(Atom moleculeAtom, Rect moleculeBounds)
        {
            var bounds = moleculeBounds;
            var atomVisual = new AtomVisual(moleculeAtom);
            atomVisual.ChemicalVisuals = chemicalVisuals;
            atomVisual.BondThickness = Chemistry.BondThickness;
            atomVisual.BackgroundColor = Background;

            atomVisual.Render();
            chemicalVisuals.Add(moleculeAtom, atomVisual);
            AddVisual(atomVisual);

            bounds.Union(atomVisual.ContentBounds);

            return bounds;
        }

        private Rect DrawBond(Bond moleculeBond, Rect moleculeBounds)
        {
            var bounds = moleculeBounds;
            var bondVisual = new BondVisual(moleculeBond);
            bondVisual.ChemicalVisuals = chemicalVisuals;
            bondVisual.BondThickness = Chemistry.BondThickness;
            bondVisual.Render();
            chemicalVisuals.Add(moleculeBond, bondVisual);
            AddVisual(bondVisual);

            bounds.Union(bondVisual.ContentBounds);

            return bounds;
        }

        public void Clear()
        {
            foreach (var visual in chemicalVisuals.Values)
            {
                DeleteVisual(visual);
            }

            chemicalVisuals.Clear();
        }

        private void DeleteVisual(DrawingVisual visual)
        {
            RemoveVisualChild(visual);
            RemoveLogicalChild(visual);
        }

        private void AddVisual(DrawingVisual visual)
        {
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }
    }

    #endregion Drawing
}