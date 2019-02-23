// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

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

            
            _size = GetBoundingBox();

            

            LeftPadding = 0d;
            TopPadding = 0d;
            if(_size.X < 0d)
            {
                LeftPadding = -_size.X;
            }

            if (_size.Y < 0d)
            {
                TopPadding = -_size.Y;
            }
            InteriorPadding = new Thickness(LeftPadding, TopPadding, 0,0);
            return _size.Size;

            
        }



        public Thickness InteriorPadding
        {
            get { return (Thickness)GetValue(InteriorPaddingProperty); }
            set { SetValue(InteriorPaddingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InteriorPadding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InteriorPaddingProperty =
            DependencyProperty.Register("InteriorPadding", typeof(Thickness), typeof(ChemistryCanvas), new PropertyMetadata(default(Thickness)));



        public double TopPadding { get; set; }

        public double LeftPadding { get; set; }


        #region Drawing

        #region Properties

        //properties
        private ViewModel _mychemistry;

        public ViewModel Chemistry
        {
            get { return _mychemistry; }
            set
            {
                if (_mychemistry != null && _mychemistry != value)
                {
                    DisconnectHandlers();
                }
                _mychemistry = value;
                DrawChemistry(_mychemistry);
                ConnectHandlers();
            }
        }

        private void ConnectHandlers()
        {
            _mychemistry.Model.AtomsChanged += Model_AtomsChanged;
            _mychemistry.Model.BondsChanged += Model_BondsChanged;
            _mychemistry.Model.MoleculesChanged += Model_MoleculesChanged;

            _mychemistry.Model.PropertyChanged += Model_PropertyChanged;
        }
        public bool AutoResize = true;


        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (sender)
            {
                case Atom a:
                    RedrawAtom(a);
                    
                    break;
                case Bond b:
                    RedrawBond(b);
                    break;
            }

            if (AutoResize)
            {
                InvalidateMeasure();
            }
        }

        private void RedrawBond(Bond bond)
        {

            int refCount = 1;
            BondVisual bv = null;
            if (chemicalVisuals.ContainsKey(bond))
            {
                bv = (BondVisual) chemicalVisuals[bond];
                refCount = bv.RefCount;
                BondRemoved(bond);
            }

            
            BondAdded(bond);

            bv = (BondVisual)chemicalVisuals[bond];
            bv.RefCount = refCount;
        }

        private void RedrawAtom(Atom atom)
        {
            int refCount = 1;
            AtomVisual av = null;
            if (chemicalVisuals.ContainsKey(atom))
            {
                av = (AtomVisual) chemicalVisuals[atom];
                refCount = av.RefCount;
                AtomRemoved(atom);
            }

            
            AtomAdded(atom);

            av = (AtomVisual)chemicalVisuals[atom];
            av.RefCount = refCount;
        }

        private void Model_MoleculesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            
        }

        private void Model_BondsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems!=null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Bond b = (Bond) eNewItem;

                    BondAdded(b);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Bond b = (Bond)eNewItem;

                    BondRemoved(b);
                }
            }
        }

        private void Model_AtomsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Atom a = (Atom)eNewItem;

                    AtomAdded(a);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Atom a = (Atom)eNewItem;

                    AtomRemoved(a);
                }
            }
        }

        private void DisconnectHandlers()
        {
            _mychemistry.Model.AtomsChanged -= Model_AtomsChanged;
            _mychemistry.Model.BondsChanged -= Model_BondsChanged;
            _mychemistry.Model.MoleculesChanged -= Model_MoleculesChanged;

            _mychemistry.Model.PropertyChanged -= Model_PropertyChanged;
        }

        #endregion Properties

        #region Fields

        private Rect _boundingBox = default(Rect);

        #endregion

        #region DPs

        public bool FitToContents
        {
            get { return (bool) GetValue(FitToContentsProperty); }
            set { SetValue(FitToContentsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FitToContents.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FitToContentsProperty =
            DependencyProperty.Register("FitToContents", typeof(bool), typeof(ChemistryCanvas),
                new PropertyMetadata(default(bool)));


        public Thickness Overhang
        {
            get { return (Thickness) GetValue(OverhangProperty); }
            set { SetValue(OverhangProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Overhang.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverhangProperty =
            DependencyProperty.Register("Overhang", typeof(Thickness), typeof(Display),
                new PropertyMetadata(default(Thickness)));

        private Rect _size;
        private Thickness _interiorPadding;

        #endregion

        #region Methods


      

        private Rect GetBoundingBox()
        {
            Rect currentbounds = new Rect(new Size(0, 0));

            try
            {
                foreach (DrawingVisual element in chemicalVisuals.Values)
                {
                    var bounds = element.ContentBounds;
                    currentbounds.Union(bounds);
                    var descBounds = element.DescendantBounds;
                    currentbounds.Union(descBounds);
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
        private void DrawChemistry(ViewModel vm)
        {
            Clear();

            foreach (Molecule molecule in vm.Model.Molecules.Values)
            {
                MoleculeAdded(molecule);
            }

            var bb = GetBoundingBox();
            var leftOverhang = -Math.Min(0d, bb.Left);
            var topOverhang = Math.Min(0d, bb.Top);


            Overhang = new Thickness(leftOverhang, topOverhang, leftOverhang, topOverhang);
            InvalidateMeasure();
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

        private void MoleculeAdded(Molecule molecule)
        {
            foreach (Atom moleculeAtom in molecule.Atoms.Values)
            {
                AtomAdded(moleculeAtom);
            }

            foreach (Bond moleculeBond in molecule.Bonds)
            {
                BondAdded(moleculeBond);
            }

            foreach (Molecule child in molecule.Molecules.Values)
            {
                MoleculeAdded(child);
            }
        }

        private void MoleculeRemoved(Molecule molecule)
        {
            foreach (Atom moleculeAtom in molecule.Atoms.Values)
            {
                AtomRemoved(moleculeAtom);
            }

            foreach (Bond moleculeBond in molecule.Bonds)
            {
                BondRemoved(moleculeBond);
            }

            foreach (Molecule child in molecule.Molecules.Values)
            {
                MoleculeRemoved(child);
            }
        }



        private void AtomAdded(Atom atom)
        {
            if (!chemicalVisuals.ContainsKey(atom)) //it's not already in the list
            {
                chemicalVisuals[atom] = new AtomVisual(atom);
            }

            var av = (AtomVisual) chemicalVisuals[atom];

            if (av.RefCount == 0) // it hasn't been added before
            {
                av.ChemicalVisuals = chemicalVisuals;

                av.BackgroundColor = Background;

                av.Render();

                AddVisual(av);
            }

            av.RefCount++;
        }

        private void AtomRemoved(Atom atom)
        {
            var av = (AtomVisual) chemicalVisuals[atom];

            if (av.RefCount == 1) //removing this atom will remove the last visual
            {
                DeleteVisual(av);
                chemicalVisuals.Remove(atom);
            }
            else
            {
                av.RefCount--;
            }
        }

        private void BondAdded(Bond bond)
        {
            if (!chemicalVisuals.ContainsKey(bond)) //it's already in the list
            {
                chemicalVisuals[bond] = new BondVisual(bond);
            }

            BondVisual bv = (BondVisual) chemicalVisuals[bond];

            if (bv.RefCount == 0) // it hasn't been added before
            {
                bv.ChemicalVisuals = chemicalVisuals;
                bv.BondThickness = Globals.BondThickness;

                bv.Render();
                AddVisual(bv);
            }

            bv.RefCount ++;
        }

        private void BondRemoved(Bond bond)
        {
            var bv = (BondVisual) chemicalVisuals[bond];

            if (bv.RefCount == 1) //removing this atom will remove the last visual
            {
                DeleteVisual(bv);
                chemicalVisuals.Remove(bond);
            }
            else
            {
                bv.RefCount --;
            }
        }


        #endregion Drawing
    }
}