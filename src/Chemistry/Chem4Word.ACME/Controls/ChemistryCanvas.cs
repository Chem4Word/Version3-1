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
                DrawMolecule(molecule, Rect.Empty);

            }
            InvalidateMeasure();
        }

        private Rect DrawMolecule(Molecule molecule, Rect moleculeBounds)
        {
            var bounds = moleculeBounds;
            foreach (Bond moleculeBond in molecule.Bonds)
            {
                bounds = DrawBond(moleculeBond, bounds);
            }

            foreach (Atom moleculeAtom in molecule.Atoms)
            {
                bounds= DrawAtom(moleculeAtom, bounds);
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

                var groupBox = new DrawingVisual();
                using (DrawingContext dc = groupBox.RenderOpen())
                {
                    Brush bracketBrush =new SolidColorBrush(Colors.Gray);
                    Pen bracketPen = new Pen(bracketBrush, 1d);
                    //bracketPen.DashStyle = new DashStyle(new double[]{2,2});
                    dc.DrawRectangle(null,bracketPen,groupRect);
                    dc.Close();
                }
                AddVisualChild(groupBox);
                AddLogicalChild(groupBox);
                chemicalVisuals.Add(molecule,groupBox);
             
                bounds.Union(groupBox.ContentBounds);
                
            }
            return bounds;
        }

        private Rect DrawAtom(Atom moleculeAtom,Rect moleculeBounds)
        {
            var bounds = moleculeBounds;
            var atomVisual = new AtomVisual(moleculeAtom);
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
    #endregion
}
