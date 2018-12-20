// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Linq;
using Chem4Word.Model;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME.Controls
{
    public class ChemistryCanvas : Canvas
    {
        public ChemistryCanvas()
        {
            chemicalVisuals = new Dictionary<object, DrawingVisual>();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size size = new Size();

            Rect currentbounds = new Rect(new Size(0, 0));


            try
            {
                
                foreach (DrawingVisual element in chemicalVisuals.Values)
                {

                    var bounds = element.ContentBounds;

                    currentbounds.Union(bounds);
                    //measure desired size for each child
                 
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            size = currentbounds.Size;
            
            // add margin
            size.Width += 10;
            size.Height += 10;
            return size;
        }



        #region Drawing

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

        //overrides 
        protected override Visual GetVisualChild(int index)
        {
            return chemicalVisuals.ElementAt(index).Value;
        }
      
        protected override int VisualChildrenCount => chemicalVisuals.Count;

        //bookkeeping collection
        private Dictionary<object, DrawingVisual> chemicalVisuals { get; }
        private void DrawChemistry(DisplayViewModel vm)
        {
            Clear();

            foreach (Molecule molecule in vm.Model.Molecules)
            {
                DrawMolecule(molecule);

            }
            InvalidateMeasure();
        }

        private Rect? DrawMolecule(Molecule molecule, Rect? moleculeBounds = null)
        {

            foreach (Bond moleculeBond in molecule.Bonds)
            {
                moleculeBounds = DrawBond(moleculeBond, moleculeBounds);
            }

            foreach (Atom moleculeAtom in molecule.Atoms)
            {
                moleculeBounds= DrawAtom(moleculeAtom, moleculeBounds);
            }


            if (molecule.Molecules.Any())
            {
                Rect? groupRect = null;
                foreach (Molecule child in molecule.Molecules)
                {

                    var childRect = DrawMolecule(child, moleculeBounds);
                    if (groupRect == null)
                    {
                        groupRect = childRect;
                    }
                    else
                    {
                        groupRect.Value.Union(childRect.Value);
                    }
                }

                var groupBox = new DrawingVisual();
                using (DrawingContext dc = groupBox.RenderOpen())
                {
                    Brush bracketBrush =new SolidColorBrush(Colors.Gray);
                    Pen bracketPen = new Pen(bracketBrush, 1d);
                    //bracketPen.DashStyle = new DashStyle(new double[]{2,2});
                    dc.DrawRectangle(null,bracketPen,groupRect.Value);

                    dc.Close();

                }
                AddVisualChild(groupBox);
                AddLogicalChild(groupBox);
                chemicalVisuals.Add(molecule,groupBox);
                if (moleculeBounds == null)
                {
                    moleculeBounds = groupBox.ContentBounds;
                }
                else
                {
                    moleculeBounds.Value.Union(groupBox.ContentBounds);
                }
            }
            return moleculeBounds;
        }

        private Rect DrawAtom(Atom moleculeAtom,Rect? moleculeBounds)
        {
            var atomVisual = new AtomVisual(moleculeAtom);
            atomVisual.Render();
            chemicalVisuals.Add(moleculeAtom, atomVisual);
            AddVisual(atomVisual);
            if (moleculeBounds == null)
            {
                moleculeBounds = atomVisual.ContentBounds;
            }
            else
            {
                moleculeBounds.Value.Union(atomVisual.ContentBounds);
            }

            return moleculeBounds.Value;
        }

        private Rect DrawBond(Bond moleculeBond, Rect? moleculeBounds)
        {
            var bondVisual = new BondVisual(moleculeBond);
            bondVisual.Render();
            chemicalVisuals.Add(moleculeBond, bondVisual);
            AddVisual(bondVisual);
            if (moleculeBounds == null)
            {
                moleculeBounds = bondVisual.ContentBounds;
            }
            else
            {
                moleculeBounds.Value.Union(bondVisual.ContentBounds);
            }

            return moleculeBounds.Value;
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
    #endregion
}
