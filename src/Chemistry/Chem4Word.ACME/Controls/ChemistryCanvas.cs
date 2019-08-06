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
        private const double Spacing = 4.5;

        #region Fields

        private Adorner _highlightAdorner;

        #endregion Fields

        #region Constructors

        public ChemistryCanvas()
        {
            chemicalVisuals = new Dictionary<object, DrawingVisual>();
            MouseMove += Canvas_MouseMove;
        }

        #endregion Constructors

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
                    if (HighlightActive)
                    {
                        SetActiveAdorner(value);
                    }

                    _activeVisual = value;
                }
            }
        }

        public AtomVisual ActiveAtomVisual => (ActiveVisual as AtomVisual);

        public BondVisual ActiveBondVisual => (ActiveVisual as BondVisual);

        public GroupVisual ActiveGroupVisualVisual => (ActiveVisual as GroupVisual);

        public Model2.ChemistryBase ActiveChemistry
        {
            get
            {
                switch (ActiveVisual)
                {
                    case GroupVisual gv:
                        return gv.ParentMolecule;
                    case BondVisual bv:
                        return bv.ParentBond;

                    case AtomVisual av:
                        return av.ParentAtom;

                    default:
                        return null;
                }
            }
            set
            {
                if (value == null)
                {
                    ActiveVisual = null;
                }
                else
                {
                    ActiveVisual = (chemicalVisuals[(value)] as ChemicalVisual);
                }
            }
        }



        public bool ShowGroups
        {
            get { return (bool)GetValue(ShowGroupsProperty); }
            set { SetValue(ShowGroupsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowGroups.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowGroupsProperty =
            DependencyProperty.Register("ShowGroups", typeof(bool), typeof(ChemistryCanvas), new FrameworkPropertyMetadata(true,FrameworkPropertyMetadataOptions.AffectsParentArrange |FrameworkPropertyMetadataOptions.AffectsMeasure, ShowGroupsChanged));

        private static void ShowGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChemistryCanvas cc = (ChemistryCanvas) d;
            bool showGroups = (bool) e.NewValue;
            cc.Clear();
            cc.DrawChemistry(cc.Chemistry);
        }


        public bool HighlightActive
        {
            get { return (bool) GetValue(HighlightActiveProperty); }
            set { SetValue(HighlightActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighlightActive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightActiveProperty =
            DependencyProperty.Register("HighlightActive", typeof(bool), typeof(ChemistryCanvas),
                                        new PropertyMetadata(true));

        #endregion Properties

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
                    case Molecule m:
                        RedrawMolecule(m, GetDrawnBoundingBox(m));
                        break;
                }

                if (AutoResize)
                {
                    InvalidateMeasure();
                }
            }
        }

        private void RedrawMolecule(Molecule molecule, Rect? boundingBox = null)
        {
            if (chemicalVisuals.ContainsKey(molecule)) //it's already in the list
            {
                var doomed = chemicalVisuals[molecule];
                DeleteVisual(doomed);
                chemicalVisuals.Remove(molecule);
            }

            if (molecule.IsGrouped & ShowGroups)
            {
                chemicalVisuals[molecule] = new GroupVisual(molecule, boundingBox);
                var gv = (GroupVisual) chemicalVisuals[molecule];
                gv.ChemicalVisuals = chemicalVisuals;
                gv.Render();
                AddVisual(gv);
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

            bv = (BondVisual) chemicalVisuals[bond];
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

            av = (AtomVisual) chemicalVisuals[atom];
            av.RefCount = refCount;
        }

        private void Model_MoleculesChanged(object sender,
                                            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Molecule a = (Molecule) eNewItem;

                    MoleculeAdded(a);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Molecule b = (Molecule) eNewItem;

                    MoleculeRemoved(b);
                }
            }
        }

        private void Model_BondsChanged(object sender,
                                        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
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
                    Bond b = (Bond) eNewItem;

                    BondRemoved(b);
                }
            }
        }

        private void Model_AtomsChanged(object sender,
                                        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    Atom a = (Atom) eNewItem;

                    AtomAdded(a);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var eNewItem in e.OldItems)
                {
                    Atom a = (Atom) eNewItem;

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
            get { return (bool) GetValue(FitToContentsProperty); }
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
                case GroupVisual gv:
                    _highlightAdorner = new GroupHoverAdorner(this, gv);
                    break;
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
            RemoveLogicalChild(visual);
            RemoveVisualChild(visual);
        }

        private void AddVisual(DrawingVisual visual)
        {
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }

        public Rect GetDrawnBoundingBox(Molecule mol)
        {
            Rect bb = Rect.Empty;
            foreach (var atom in mol.Atoms.Values)
            {
                bb.Union(((AtomVisual) chemicalVisuals[atom]).ContentBounds);
            }

            foreach (var m in mol.Molecules.Values)
            {
                if (chemicalVisuals.TryGetValue(m, out DrawingVisual molVisual))
                {
                    GroupVisual gv = (GroupVisual) molVisual;
                    bb.Union(gv.ContentBounds);
                    bb.Inflate(Spacing, Spacing);
                }
                else
                {
                    bb.Union(GetDrawnBoundingBox(m));
                }
            }

            return bb;
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

            var bb = GetDrawnBoundingBox(molecule);
            //do the final rendering of the group visual on top
            RedrawMolecule(molecule, bb);
        }

        private void MoleculeRemoved(Molecule molecule)
        {
            if (molecule.IsGrouped)
            {
                var gv = (GroupVisual) chemicalVisuals[molecule];

                DeleteVisual(gv);
                chemicalVisuals.Remove(molecule);
            }

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

            bv.RefCount++;
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
                bv.RefCount--;
            }
        }

        #endregion Drawing

        #region Event handlers

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            ActiveVisual = GetTargetedVisual(e.GetPosition(this));
        }

        #endregion Event handlers

        #region Methods

        public ChemicalVisual GetTargetedVisual(Point p)
        {
            _visuals.Clear();
            VisualTreeHelper.HitTest(this, null, ResultCallback, new PointHitTestParameters(p));
            var groupVisual = _visuals.FirstOrDefault(v => v is GroupVisual);
            if (groupVisual != null)
            {
                return groupVisual;
            }

            var visual = _visuals.FirstOrDefault(v => v is AtomVisual);
            if (visual != null)
            {
                return visual;
            }

            return _visuals.FirstOrDefault();
        }

        public HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            _visualHit = result.VisualHit as ChemicalVisual;
            _visuals.Add(_visualHit);

            return HitTestResultBehavior.Continue;
        }

        #endregion Methods
    }
}