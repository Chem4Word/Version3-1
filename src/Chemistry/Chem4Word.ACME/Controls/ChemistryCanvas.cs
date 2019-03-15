// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Drawing;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Controls
{
    public class ChemistryCanvas : Canvas
    {
        #region Fields
        private Adorner _highlightAdorner;
         
        #endregion

        #region Constructors
        public ChemistryCanvas()
        {
            chemicalVisuals = new Dictionary<object, DrawingVisual>();
            MouseMove += Canvas_MouseMove;
        }
        #endregion

        #region Properties

        private ChemicalVisual _activeVisual = null;
        public ChemicalVisual ActiveVisual
        {
            get { return _activeVisual; }
            set
            {
                if (_activeVisual != value)
                {
                    RemoveActiveAdorner();
                    SetActiveAdorner(value);
                    _activeVisual = value;
                }
            }
       
        }

      

        #endregion

        /// <summary>
        /// called during WPF layout phase
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            var size = GetBoundingBox();

            if (_mychemistry != null && _mychemistry.Model != null)
            {
                // Only need to do this on "small" structures
                if (_mychemistry.Model.TotalAtomsCount < 100)
                {
                    var abb = _mychemistry.Model.OverallAtomBoundingBox;

                    double leftPadding = 0;
                    double topPadding = 0;

                    if (size.Left < abb.Left)
                    {
                        leftPadding = abb.Left - size.Left;
                    }

                    if (size.Top < abb.Top)
                    {
                        topPadding = abb.Top - size.Top;
                    }

                    _mychemistry.Model.RepositionAll(-leftPadding, -topPadding);
                    DrawChemistry(_mychemistry);
                }
            }

            return size.Size;
        }

        #region Drawing

        #region Properties

        //properties
        private ViewModel _mychemistry;

        public bool SuppressRedraw { get; set; }

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
            if (!SuppressRedraw)
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
        }

        private void RedrawBond(Bond bond)
        {
            int refCount = 1;
            BondVisual bv = null;
            if (chemicalVisuals.ContainsKey(bond))
            {
                bv = (BondVisual)chemicalVisuals[bond];
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
                av = (AtomVisual)chemicalVisuals[atom];
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
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Bond b = (Bond)eNewItem;

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

        //private Rect _boundingBox = default(Rect);
        private ChemicalVisual _visualHit;

        private List<ChemicalVisual> _visuals = new List<ChemicalVisual>();

        #endregion Fields

        #region DPs

        public bool FitToContents
        {
            get { return (bool)GetValue(FitToContentsProperty); }
            set { SetValue(FitToContentsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FitToContents.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FitToContentsProperty =
            DependencyProperty.Register("FitToContents", typeof(bool), typeof(ChemistryCanvas),
                new PropertyMetadata(default(bool)));

        #endregion DPs

        #region Methods

        private Rect GetBoundingBox()
        {
            Rect currentbounds = Rect.Empty;

            try
            {
                foreach (DrawingVisual element in chemicalVisuals.Values)
                {
                    var bounds = element.ContentBounds;
                    currentbounds.Union(bounds);
                    var descBounds = element.DescendantBounds;
                    currentbounds.Union(descBounds);
                    //Debug.WriteLine($"CB: {currentbounds}, B:{bounds}, D:{descBounds}");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return currentbounds;
        }

        #endregion Methods

        private void RemoveActiveAdorner()
        {
            if (_highlightAdorner != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(this);
                layer.Remove(_highlightAdorner);
                _highlightAdorner = null;
            }
        }

        private void SetActiveAdorner(ChemicalVisual value)
        {
            switch (value)
            {
                case FunctionalGroupVisual fv:
                    _highlightAdorner = new AtomHoverAdorner(this, fv);
                    break;

                case AtomVisual av:
                    _highlightAdorner = new AtomHoverAdorner(this, av);
                    break;

                case BondVisual bv:
                    _highlightAdorner = new BondHoverAdorner(this, bv);
                    break;

                default:
                    _highlightAdorner = null;
                    break;
            }

            ;
        }
        //overrides
        protected override Visual GetVisualChild(int index)
        {
            return chemicalVisuals.ElementAt(index).Value;
        }

        protected override int VisualChildrenCount => chemicalVisuals.Count;

        //bookkeeping collection
        protected Dictionary<object, DrawingVisual> chemicalVisuals { get; }

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
                if (atom.Element is FunctionalGroup fg)
                {
                    chemicalVisuals[atom] = new FunctionalGroupVisual(atom);
                }
                else
                {
                    chemicalVisuals[atom] = new AtomVisual(atom);
                }
            }

            var cv = chemicalVisuals[atom];

            if (cv is FunctionalGroupVisual fgv)
            {
                if (fgv.RefCount == 0) // it hasn't been added before
                {
                    fgv.ChemicalVisuals = chemicalVisuals;

                    fgv.BackgroundColor = Background;

                    fgv.Render();

                    AddVisual(fgv);
                }
                fgv.RefCount++;
            }
            else if (cv is AtomVisual av)
            {
                if (av.RefCount == 0) // it hasn't been added before
                {
                    av.ChemicalVisuals = chemicalVisuals;

                    av.BackgroundColor = Background;

                    av.Render();

                    AddVisual(av);
                }
                av.RefCount++;
            }
        }

        private void AtomRemoved(Atom atom)
        {
            var av = (AtomVisual)chemicalVisuals[atom];

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

            BondVisual bv = (BondVisual)chemicalVisuals[bond];

            if (bv.RefCount == 0) // it hasn't been added before
            {
                bv.ChemicalVisuals = chemicalVisuals;
                bv.BondThickness = Globals.BondThickness;

                bv.Render();
                AddVisual(bv);
            }

            bv.RefCount++;
        }

        private void BondRemoved(Bond bond)
        {
            var bv = (BondVisual)chemicalVisuals[bond];

            if (bv.RefCount == 1) //removing this atom will remove the last visual
            {
                DeleteVisual(bv);
                chemicalVisuals.Remove(bond);
            }
            else
            {
                bv.RefCount--;
            }
        }

        #endregion Drawing

        #region Event handlers

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this is EditorCanvas ec)
            {
                Debug.WriteLine($"EC: @ {e.GetPosition(this)}");
            }
            else
            {
                Debug.WriteLine($"CC: @ {e.GetPosition(this)}");
            }

            ActiveVisual = GetTargetedVisual(e.GetPosition(this));

           
        }

        #endregion Event handlers

        #region Methods

        private ChemicalVisual GetTargetedVisual(Point p)
        {
            _visuals.Clear();
            VisualTreeHelper.HitTest(this, null, ResultCallback, new PointHitTestParameters(p));
            var selAtomVisual = _visuals.FirstOrDefault(v => v is AtomVisual);
            if(selAtomVisual!=null)
            {
                return selAtomVisual;
            }
            else
            {
                return _visuals.FirstOrDefault();
            }

        }

        private HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            _visualHit = result.VisualHit as ChemicalVisual;
          _visuals.Add(_visualHit);

            return HitTestResultBehavior.Continue;
        }

        #endregion Methods
    }
}