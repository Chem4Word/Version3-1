// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Enums;
using Chem4Word.Model.Geometry;
using Chem4Word.ViewModel.Adorners;
using Chem4Word.ViewModel.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace Chem4Word.ViewModel
{
    public class NewAtomPlacement
    {
        public Point Position { get; set; }
        public Atom ExistingAtom { get; set; }
    }

    public class EditViewModel : DisplayViewModel
    {
        [Flags]
        public enum SelectionTypeCode
        {
            None = 0,
            Atom = 1,
            Bond = 2,
            Molecule = 4,
            Reaction = 8
        }

        #region Fields

        public readonly Dictionary<object, Adorner> SelectionAdorners = new Dictionary<object, Adorner>();
        private Dictionary<int, BondOption> _bondOptions = new Dictionary<int, BondOption>();
        private int? _selectedBondOptionId;

        public ComboBox BondLengthCombo { get; set; }

        #endregion Fields

        #region Properties

        public List<BondLengthOption> BondLengthOptions { get; } = new List<BondLengthOption>();
        public BondLengthOption SelectedBondLengthOption { get; set; }

        public double EditBondThickness
        {
            get
            {
                return BondThickness * Globals.DefaultBondLineFactor;
            }
        }

        public double EditHalfBondThickness
        {
            get
            {
                return EditBondThickness / 2;
            }
        }

        public SelectionTypeCode SelectionType
        {
            get
            {
                SelectionTypeCode result = SelectionTypeCode.None;

                if (SelectedItems.OfType<Atom>().Any())
                {
                    result |= SelectionTypeCode.Atom;
                }
                if (SelectedItems.OfType<Bond>().Any())
                {
                    result |= SelectionTypeCode.Bond;
                }
                if (SelectedItems.OfType<Molecule>().Any())
                {
                    result |= SelectionTypeCode.Molecule;
                }
                if (SelectedItems.OfType<Reaction>().Any())
                {
                    result |= SelectionTypeCode.Reaction;
                }
                return result;
            }
        }

        public ObservableCollection<object> SelectedItems { get; }

        public UndoHandler UndoManager { get; }

        private ElementBase _selectedElement;

        public ElementBase SelectedElement
        {
            get
            {
                var selElements = SelectedElementType;
                if (selElements.Count == 1)
                {
                    return selElements[0];
                }

                if (selElements.Count == 0) //nothing selected so return null
                {
                    return _selectedElement;
                }

                return null;
            }

            set
            {
                _selectedElement = value;

                var selAtoms = SelectedItems.OfType<Atom>().ToList();
                SelectedItems.Clear();
                SetElement(value, selAtoms);
            }
        }

        private void SetElement(ElementBase value, List<Atom> selAtoms)
        {
            if (selAtoms.Any())
            {
                UndoManager.BeginUndoBlock();

                Action undo, redo;
                foreach (Atom selectedAtom in selAtoms)
                {
                    if (selectedAtom.Element != value)
                    {
                        redo = () =>
                        {
                            selectedAtom.Element = value;
                        };
                        var lastElement = selectedAtom.Element;

                        undo = () =>
                        {
                            selectedAtom.Element = lastElement;
                        };
                        UndoManager.RecordAction(undo, redo, $"Set Element to {value.Symbol}");
                        selectedAtom.Element = value;
                    }
                }

                UndoManager.EndUndoBlock();
            }
        }

        /// <summary>
        /// returns a distinct list of selected elements
        /// </summary>
        public List<ElementBase> SelectedElementType
        {
            get
            {
                return SelectedItems.OfType<Atom>().Select(a => a.Element).Distinct().ToList();
            }
        }

        public int? SelectedBondOptionId
        {
            get
            {
                var btList = (from bt in SelectedBondOptions
                              select bt.Id).Distinct();

                if (btList.Count() == 1)
                {
                    return btList.ToList()[0];
                }

                if (btList.Count() == 0)
                {
                    return _selectedBondOptionId;
                }

                return null;
            }

            set
            {
                _selectedBondOptionId = value;
                if (value != null)
                {
                    SetBondOption(value.Value);
                }
            }
        }

        private void SetBondOption(int bondOptionId)
        {
            var bondOption = _bondOptions[_selectedBondOptionId.Value];
            if (SelectedItems.OfType<Bond>().Any())
            {
                UndoManager.BeginUndoBlock();
                foreach (Bond bond in SelectedItems.OfType<Bond>())
                {
                    Action redo = () =>
                    {
                        bond.Stereo = bondOption.Stereo.Value;
                        bond.Order = bondOption.Order;
                    };

                    var bondStereo = bond.Stereo;
                    var bondOrder = bond.Order;
                    Action undo = () =>
                    {
                        bond.Stereo = bondStereo;
                        bond.Order = bondOrder;
                    };
                    UndoManager.RecordAction(undo, redo);
                    bond.Order = bondOption.Order;
                    bond.Stereo = bondOption.Stereo.Value;
                }

                UndoManager.EndUndoBlock();
            }
        }

        public List<BondOption> SelectedBondOptions
        {
            get
            {
                var selectedBonds = SelectedItems.OfType<Bond>();

                var selbonds = (from Bond selbond in selectedBonds
                                select new BondOption { Order = selbond.Order, Stereo = selbond.Stereo }).Distinct();

                var selOptions = from BondOption bo in _bondOptions.Values
                                 join selbond1 in selbonds
                        on new { bo.Order, bo.Stereo } equals new { selbond1.Order, selbond1.Stereo }
                                 select new BondOption { Id = bo.Id, Order = bo.Order, Stereo = bo.Stereo };
                return selOptions.ToList();
            }
        }

        public Canvas DrawingSurface { get; set; }

        #endregion Properties

        private Behavior _activeMode;

        public Behavior ActiveMode
        {
            get { return _activeMode; }
            set
            {
                if (_activeMode != null)
                {
                    _activeMode.Detach();
                }
                _activeMode = value;
                _activeMode?.Attach(DrawingSurface);
            }
        }

        #region Commands

        public DeleteCommand DeleteCommand { get; }
        public AddAtomCommand AddAtomCommand { get; }
        public UndoCommand UndoCommand { get; }
        public RedoCommand RedoCommand { get; }
        public CopyCommand CopyCommand { get; }
        public CutCommand CutCommand { get; }
        public PasteCommand PasteCommand { get; }
        public FlipVerticalCommand FlipVerticalCommand { get; }
        public FlipHorizontalCommand FlipHorizontalCommand { get; }
        public AddHydrogensCommand AddHydrogensCommand { get; }
        public RemoveHydrogensCommand RemoveHydrogensCommand { get; }
        public FuseCommand FuseCommand { get; }
        public GroupCommand GroupCommand { get; }
        public UnGroupCommand UnGroupCommand { get; }
        public SettingsCommand SettingsCommand { get; }

        #endregion Commands

        #region Constructors

        public EditViewModel(Model.Model model) : base(model)
        {
            RedoCommand = new RedoCommand(this);
            UndoCommand = new UndoCommand(this);

            SelectedItems = new ObservableCollection<object>();
            SelectedItems.CollectionChanged += SelectedItemsOnCollectionChanged;

            UndoManager = new UndoHandler(this);

            DeleteCommand = new DeleteCommand(this);
            AddAtomCommand = new AddAtomCommand(this);
            CopyCommand = new CopyCommand(this);
            CutCommand = new CutCommand(this);
            PasteCommand = new PasteCommand(this);
            FlipVerticalCommand = new FlipVerticalCommand(this);
            FlipHorizontalCommand = new FlipHorizontalCommand(this);
            AddHydrogensCommand = new AddHydrogensCommand(this);
            RemoveHydrogensCommand = new RemoveHydrogensCommand(this);
            FuseCommand = new FuseCommand(this);
            GroupCommand = new GroupCommand(this);
            UnGroupCommand = new UnGroupCommand(this);
            SettingsCommand = new SettingsCommand(this);

            PeriodicTable pt = new PeriodicTable();
            _selectedElement = pt.C;

            _selectedBondOptionId = 1;

            LoadBondOptions();
            LoadBondLengthOptions();
        }

        private void LoadBondLengthOptions()
        {
            for (int i = 5; i <= 95; i += 5)
            {
                var option = new BondLengthOption
                {
                    ChosenValue = (int)(i * Globals.ScaleFactorForXaml),
                    DisplayAs = i.ToString("0")
                };
                BondLengthOptions.Add(option);
                if (Math.Abs(i * Globals.ScaleFactorForXaml - Model.XamlBondLength) < 2.5 * Globals.ScaleFactorForXaml)
                {
                    SelectedBondLengthOption = option;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void LoadBondOptions()
        {
            //if you add more bond options then you *MUST* update ACMEResources.XAML to correspond
            _bondOptions[1] = new BondOption { Id = 1, Order = "S", Stereo = BondStereo.None };
            _bondOptions[2] = new BondOption { Id = 2, Order = "D", Stereo = BondStereo.None };
            _bondOptions[3] = new BondOption { Id = 3, Order = "T", Stereo = BondStereo.None };
            _bondOptions[4] = new BondOption { Id = 4, Order = "S", Stereo = BondStereo.Wedge };
            _bondOptions[5] = new BondOption { Id = 5, Order = "S", Stereo = BondStereo.Hatch };
        }

        #endregion Constructors

        private void SelectedItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var newObjects = e.NewItems;
            var oldObject = e.OldItems;

            if (newObjects != null)
            {
                AddSelectionAdorners(newObjects);
            }

            if (oldObject != null)
            {
                RemoveSelectionAdorners(oldObject);
            }
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    RemoveAllAdorners();
                    break;
            }

            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedBondOptionId));
            OnPropertyChanged(nameof(SelectionType));

            UpdateCommandStatuses();
        }

        private void UpdateCommandStatuses()
        {
            CopyCommand.RaiseCanExecChanged();
            CutCommand.RaiseCanExecChanged();
            DeleteCommand.RaiseCanExecChanged();
            FlipHorizontalCommand.RaiseCanExecChanged();
            FlipVerticalCommand.RaiseCanExecChanged();
            AddHydrogensCommand.RaiseCanExecChanged();
            RemoveHydrogensCommand.RaiseCanExecChanged();
        }

        public void RemoveAllAdorners()
        {
            var layer = AdornerLayer.GetAdornerLayer(DrawingSurface);
            var adornerList = SelectionAdorners.Keys.ToList();
            foreach (object oldObject in adornerList)
            {
                layer.Remove(SelectionAdorners[oldObject]);
                SelectionAdorners.Remove(oldObject);
            }
        }

        private void RemoveSelectionAdorners(IList oldObjects)
        {
            var layer = AdornerLayer.GetAdornerLayer(DrawingSurface);
            foreach (object oldObject in oldObjects)
            {
                if (SelectionAdorners.ContainsKey(oldObject))
                {
                    var selectionAdorner = SelectionAdorners[oldObject];
                    if (selectionAdorner is MoleculeSelectionAdorner)
                    {
                        var msAdorner = (MoleculeSelectionAdorner)selectionAdorner;

                        msAdorner.DragCompleted -= MolAdorner_ResizeCompleted;
                        msAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                        (msAdorner as SingleAtomSelectionAdorner).DragCompleted -= MolAdorner_DragCompleted;
                    }
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(oldObject);
                }
            }
        }

        private void AddSelectionAdorners(IList newObjects)
        {
            foreach (object newObject in newObjects)
            {
                if (newObject is Atom)
                {
                    var atom = (Atom)newObject;

                    AtomSelectionAdorner atomAdorner = new AtomSelectionAdorner(DrawingSurface, atom);
                    SelectionAdorners[newObject] = atomAdorner;
                    atomAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;

                    //if all atoms are selected then select the mol
                    if (AllAtomsSelected(atom.Parent))
                    {
                        RemoveAtomBondAdorners(atom.Parent);
                        MoleculeSelectionAdorner molAdorner = new MoleculeSelectionAdorner(DrawingSurface, atom.Parent, this);
                        SelectionAdorners[newObject] = molAdorner;
                    }
                }
                else if (newObject is Bond)
                {
                    BondSelectionAdorner bondAdorner = new BondSelectionAdorner(DrawingSurface, (newObject as Bond));
                    SelectionAdorners[newObject] = bondAdorner;
                    bondAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                }
                else if (newObject is Molecule)
                {
                    Molecule mol = (Molecule)newObject;
                    if (mol.Atoms.Count == 1)
                    {
                        SingleAtomSelectionAdorner atomAdorner =
                            new SingleAtomSelectionAdorner(DrawingSurface, mol, this);
                        SelectionAdorners[newObject] = atomAdorner;
                        atomAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                        atomAdorner.DragCompleted += AtomAdorner_DragCompleted;
                    }
                    else
                    {
                        MoleculeSelectionAdorner molAdorner =
                            new MoleculeSelectionAdorner(DrawingSurface, (newObject as Molecule), this);
                        SelectionAdorners[newObject] = molAdorner;
                        molAdorner.ResizeCompleted += MolAdorner_ResizeCompleted;
                        molAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                        (molAdorner as SingleAtomSelectionAdorner).DragCompleted += MolAdorner_DragCompleted;
                    }
                }
            }
        }

        private void MolAdorner_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var moleculeSelectionAdorner = ((MoleculeSelectionAdorner)sender);
            var movedMolecule = moleculeSelectionAdorner.AdornedMolecule;
            RemoveFromSelection(movedMolecule);

            //and add in a new one
            AddToSelection(movedMolecule);
        }

        private void AtomAdorner_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner

            var moleculeSelectionAdorner = ((SingleAtomSelectionAdorner)sender);
            var movedMolecule = moleculeSelectionAdorner.AdornedMolecule;
            RemoveFromSelection(movedMolecule);

            //and add in a new one
            AddToSelection(movedMolecule.Atoms[0]);
        }

        private void MolAdorner_ResizeCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            var movedMolecule = (sender as MoleculeSelectionAdorner).AdornedMolecule;
            RemoveFromSelection(movedMolecule);

            //and add in a new one
            AddToSelection(movedMolecule);
        }

        private void SelAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                SelectedItems.Clear();
                if (sender is AtomSelectionAdorner)
                {
                    Molecule mol = (sender as AtomSelectionAdorner).AdornedAtom.Parent as Molecule;
                    RemoveAtomBondAdorners(mol);
                    AddToSelection(mol);
                }
                else if (sender is BondSelectionAdorner)
                {
                    Molecule mol = (sender as BondSelectionAdorner).AdornedBond.Parent as Molecule;
                    RemoveAtomBondAdorners(mol);
                    AddToSelection(mol);
                }
                else if (sender is MoleculeSelectionAdorner)
                {
                    Molecule mol = (sender as MoleculeSelectionAdorner).AdornedMolecule;
                }
                else if (sender is SingleAtomSelectionAdorner)
                {
                    Molecule mol = (sender as SingleAtomSelectionAdorner).AdornedMolecule.Parent as Molecule;
                }
            }
        }

        private void RemoveAtomBondAdorners(Molecule atomParent)
        {
            var layer = AdornerLayer.GetAdornerLayer(DrawingSurface);
            foreach (Bond bond in atomParent.Bonds)
            {
                if (SelectionAdorners.ContainsKey(bond))
                {
                    var selectionAdorner = SelectionAdorners[bond];
                    selectionAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(bond);
                }
            }

            foreach (Atom atom in atomParent.Atoms)
            {
                if (SelectionAdorners.ContainsKey(atom))
                {
                    var selectionAdorner = SelectionAdorners[atom];
                    selectionAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(atom);
                }
            }
        }

        private bool AllAtomsSelected(Molecule atomParent)
        {
            Debug.WriteLine($"Atom count = {atomParent.Atoms.Count()}, Adornder Count = {MolAtomAdorners(atomParent).Count()}");
            return atomParent.Atoms.Count() == MolAtomAdorners(atomParent).Count();
        }

        private IEnumerable<AtomSelectionAdorner> MolAtomAdorners(Molecule atomParent)
        {
            return SelectionAdorners.Values.OfType<AtomSelectionAdorner>()
                .Where(asl => asl.AdornedAtom.Parent == atomParent);
        }

        public void CutSelection()
        {
            MessageBox.Show("Cut code goes here");
        }

        public void CopySelection()
        {
            MessageBox.Show("Copy code goes here");
        }

        public void SetAverageBondLength(double newLength, Size canvas)
        {
            UndoManager.BeginUndoBlock();
            double currentLength = Model.MeanBondLength;
            SelectedBondLengthOption = null;
            BondLengthOption blo = null;
            foreach (var option in BondLengthOptions)
            {
                if (Math.Abs(option.ChosenValue - currentLength) < 2.5 * Globals.ScaleFactorForXaml)
                {
                    blo = option;
                    break;
                }
            }

            Action undoAction = () =>
            {
                Model.ScaleToAverageBondLength(currentLength);
                Model.CentreInCanvas(canvas);

                FontSize = currentLength * Globals.FontSizePercentageBond;
                SelectedBondLengthOption = blo;
                // Hack: Couldn't find a better way to do this
                BondLengthCombo.SelectedItem = blo;
            };
            Action redoAction = () =>
            {
                Model.ScaleToAverageBondLength(newLength);
                Model.CentreInCanvas(canvas);

                FontSize = newLength * Globals.FontSizePercentageBond;
            };

            UndoManager.RecordAction(undoAction, redoAction);

            redoAction.Invoke();

            UndoManager.EndUndoBlock();
        }

        public void DeleteAtom(Atom atom)
        {
            UndoManager.BeginUndoBlock();
            var bondlist = atom.Bonds.ToList();
            var parent = atom.Parent;

            Dictionary<Bond, Atom> startAtoms = new Dictionary<Bond, Atom>();
            Dictionary<Bond, Atom> endAtoms = new Dictionary<Bond, Atom>();

            Action undoAction = () =>
            {
                parent.Atoms.Add(atom);
                //SelectedItems.Clear();
                foreach (Bond bond in bondlist)
                {
                    bond.StartAtom = startAtoms[bond];
                    bond.EndAtom = endAtoms[bond];
                    parent.Bonds.Add(bond);
                }
                //if (!SelectedItems.Contains(atom))
                //{
                //    AddToSelection(atom);
                //}
            };
            Action redoAction = () =>
            {
                foreach (Bond bond in bondlist)
                {
                    startAtoms[bond] = bond.StartAtom;
                    endAtoms[bond] = bond.EndAtom;
                    bond.StartAtom = null;
                    bond.EndAtom = null;
                    parent.Bonds.Remove(bond);
                }
                parent.Atoms.Remove(atom);
                if (SelectedItems.Contains(atom))
                {
                    RemoveFromSelection(atom);
                }
            };

            UndoManager.RecordAction(undoAction, redoAction);
            UndoManager.EndUndoBlock();
            redoAction();
        }

        public void DeleteBond(Bond bond)
        {
            UndoManager.BeginUndoBlock();
            var a1 = bond.StartAtom;
            var a2 = bond.EndAtom;
            Molecule parent = bond.Parent;

            Model.Model mod = bond.Model;

            bool isTopLevel = UndoManager.TransactionLevel == 1;
            Action redoAction = () =>
            {
                //if (SelectedItems.Contains(bond))
                //{
                //    RemoveFromSelection(bond);
                //}
                bond.StartAtom = null;
                bond.EndAtom = null;
                parent?.Bonds.Remove(bond);

                parent?.Split(a1, a2);
                if (isTopLevel)
                {
                    parent?.RebuildRings();
                }
            };

            Action undoAction = () =>
            {
                bond.StartAtom = a1;
                bond.EndAtom = a2;
                a1.Parent.Bonds.Add(bond);
                //AddToSelection(bond);

                if (a2.Parent != a1.Parent)
                {
                    a1.Parent.Merge(a2.Parent);
                }
                if (isTopLevel)
                {
                    a1.Parent.RebuildRings();
                }
            };

            redoAction();

            UndoManager.RecordAction(undoAction, redoAction);
            UndoManager.EndUndoBlock();
        }

        public Atom AddAtomChain(Atom lastAtom, Point newAtomPos, ClockDirections dir, Element elem = null)
        {
            Atom newAtom = new Atom();
            newAtom.Element = elem ?? _selectedElement;
            newAtom.Position = newAtomPos;
            object tag = null;
            if (dir != ClockDirections.Nothing)
            {
                tag = dir;
            }

            object oldDir = lastAtom?.Tag;

            if (lastAtom != null)
            {
                UndoManager.BeginUndoBlock();

                Molecule currentMol = lastAtom.Parent;

                Action undo = () =>
                {
                    lastAtom.Tag = oldDir;
                    currentMol.Atoms.Remove(newAtom);
                };
                Action redo = () =>
                {
                    lastAtom.Tag = tag;//save the last sprouted direction in the tag object
                    currentMol.Atoms.Add(newAtom);
                };
                UndoManager.RecordAction(undo, redo);

                redo();

                AddNewBond(lastAtom, newAtom, currentMol);

                UndoManager.EndUndoBlock();
            }
            else
            {
                UndoManager.BeginUndoBlock();

                var _currentMol = new Molecule();

                Action undo = () =>
                {
                    Model.Molecules.Remove(_currentMol);
                };
                Action redo = () =>
                {
                    Model.Molecules.Add(_currentMol);
                };
                redo();

                UndoManager.RecordAction(undo, redo);

                Action undo2 = () =>
                {
                    newAtom.Tag = null;
                    _currentMol.Atoms.Remove(newAtom);
                };
                Action redo2 = () =>
                {
                    _currentMol.Atoms.Add(newAtom);
                    newAtom.Tag = tag;
                };
                UndoManager.RecordAction(undo2, redo2);

                redo2();

                UndoManager.EndUndoBlock();
            }

            return newAtom;
        }

        public void AddNewBond(Atom a, Atom b, Molecule mol)
        {
            UndoManager.BeginUndoBlock();

            var stereo = CurrentStereo;
            var order = CurrentBondOrder;

            Bond newbond = new Bond();

            newbond.Stereo = stereo;
            newbond.Order = order;

            newbond.StartAtom = a;
            newbond.EndAtom = b;

            Action undo = () =>
            {
                var isCyclic = newbond.IsCyclic();
                mol.Bonds.Remove(newbond);
                newbond.StartAtom = null;
                newbond.EndAtom = null;
                if (isCyclic)
                {
                    mol.RebuildRings();
                }
            };
            Action redo = () =>
            {
                newbond.StartAtom = a;
                newbond.EndAtom = b;
                mol.Bonds.Add(newbond);
                if (newbond.StartAtom.Parent != newbond.EndAtom.Parent)
                {
                    newbond.StartAtom.Parent.Merge(newbond.EndAtom.Parent);
                }
                else
                {
                    mol.RebuildRings();
                }
            };

            UndoManager.RecordAction(undo, redo);

            UndoManager.EndUndoBlock();

            redo();
        }

        public string CurrentBondOrder
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Order; }
        }

        public BondStereo CurrentStereo
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Stereo.Value; }
        }

        public void IncreaseBondOrder(Bond existingBond)
        {
            UndoManager.BeginUndoBlock();

            var stereo = existingBond.Stereo;
            var order = existingBond.Order;

            Action redo = () =>
            {
                existingBond.Stereo = BondStereo.None;
                if (existingBond.Order == Bond.OrderZero)
                {
                    existingBond.Order = Bond.OrderSingle;
                    existingBond.NotifyPlacementChanged();
                }
                if (existingBond.Order == Bond.OrderSingle)
                {
                    existingBond.Order = Bond.OrderDouble;
                    existingBond.NotifyPlacementChanged();
                }
                else if (existingBond.Order == Bond.OrderDouble)
                {
                    existingBond.Order = Bond.OrderTriple;
                    existingBond.NotifyPlacementChanged();
                }
                else if (existingBond.Order == Bond.OrderTriple)
                {
                    existingBond.Order = Bond.OrderSingle;
                    existingBond.NotifyPlacementChanged();
                }
                existingBond.StartAtom.NotifyBondingChanged();
                existingBond.EndAtom.NotifyBondingChanged();
            };
            Action undo = () =>
            {
                existingBond.Stereo = stereo;
                existingBond.Order = order;
                existingBond.NotifyPlacementChanged();
                existingBond.StartAtom.NotifyBondingChanged();
                existingBond.EndAtom.NotifyBondingChanged();
            };

            UndoManager.RecordAction(undo, redo);
            redo();

            UndoManager.EndUndoBlock();
        }

        public void DoOperation(Transform lastOperation, List<Atom> toList)
        {
            UndoManager.BeginUndoBlock();
            foreach (Atom atom in toList)
            {
                var lastPosition = atom.Position;
                var newPosition = lastOperation.Transform(lastPosition);
                Action undo = () =>
                {
                    SelectedItems.Clear();
                    (atom as Atom).Position = (Point)lastPosition;
                };

                Action redo = () =>
                {
                    SelectedItems.Clear();
                    (atom as Atom).Position = (Point)newPosition;
                };

                UndoManager.RecordAction(undo, redo);

                redo();
            }

            UndoManager.EndUndoBlock();
        }

        public void SwapBondDirection(Bond parentBond)
        {
            UndoManager.BeginUndoBlock();

            var startAtom = parentBond.StartAtom;
            var endAtom = parentBond.EndAtom;

            Action undo = () =>
            {
                parentBond.StartAtom = startAtom;
                parentBond.EndAtom = endAtom;
            };

            Action redo = () =>
            {
                parentBond.StartAtom = endAtom;
                parentBond.EndAtom = startAtom;
            };
            UndoManager.RecordAction(undo, redo);

            redo();
            UndoManager.EndUndoBlock();
        }

        public void SetBondAttributes(Bond parentBond, string newOrder = null, BondStereo? newStereo = null)
        {
            UndoManager.BeginUndoBlock();

            var order = parentBond.Order;
            var stereo = parentBond.Stereo;

            Action undo = () =>
            {
                parentBond.Order = order;
                parentBond.Stereo = stereo;
            };

            Action redo = () =>
            {
                parentBond.Order = newOrder ?? CurrentBondOrder;
                parentBond.Stereo = newStereo ?? CurrentStereo;
            };
            UndoManager.RecordAction(undo, redo);

            redo();
            UndoManager.EndUndoBlock();
        }

        public bool Dirty =>
            UndoManager.CanUndo;

        public void DrawRing(List<NewAtomPlacement> newAtomPlacements, bool unsaturated)
        {
            void MakeRingUnsaturated(List<NewAtomPlacement> list)
            {
                string bondOrder =
                    list[0].ExistingAtom.BondBetween(list[1].ExistingAtom).Order;

                int startpos = 0;

                while (startpos < list.Count)
                {
                    var nextPos = (startpos + 1) % list.Count;

                    while (startpos < list.Count && list[startpos].ExistingAtom.IsUnsaturated &&
                           list[nextPos].ExistingAtom.IsUnsaturated)
                    {
                        startpos++;
                    }

                    if (startpos == list.Count)
                    {
                        break;
                    }
                    nextPos = (startpos + 1) % list.Count;

                    if (!list[startpos].ExistingAtom.IsUnsaturated & !list[nextPos].ExistingAtom.IsUnsaturated)
                    {
                        SetBondAttributes(list[startpos].ExistingAtom.BondBetween(list[nextPos].ExistingAtom),
                            Bond.OrderDouble, BondStereo.None);

                        startpos += 2;
                    }
                    else
                    {
                        startpos++;
                    }
                }
            }

            UndoManager.BeginUndoBlock();

            //work around the ring adding atoms
            for (int i = 1; i <= newAtomPlacements.Count; i++)
            {
                int currIndex = i % newAtomPlacements.Count;
                NewAtomPlacement currentPlacement = newAtomPlacements[currIndex];
                NewAtomPlacement previousPlacement = newAtomPlacements[i - 1];

                Atom previousAtom = previousPlacement.ExistingAtom;
                Atom currentAtom = currentPlacement.ExistingAtom;

                if (currentAtom == null)
                {
                    Atom insertAtom = null;

                    insertAtom = AddAtomChain(previousAtom, currentPlacement.Position, ClockDirections.Nothing);
                    if (insertAtom == null)
                    {
                        Debugger.Break();
                    }
                    currentPlacement.ExistingAtom = insertAtom;
                }
                else if (previousAtom != null && previousAtom.BondBetween(currentAtom) == null)
                {
                    AddNewBond(previousAtom, currentAtom, previousAtom.Parent);
                }
            }
            //join up the ring if there is no last bond
            if (newAtomPlacements[0].ExistingAtom
                    .BondBetween(newAtomPlacements[1].ExistingAtom) == null)
            {
                AddNewBond(newAtomPlacements[0].ExistingAtom, newAtomPlacements[1].ExistingAtom, newAtomPlacements[0].ExistingAtom.Parent);
            }
            //set the alternating single and double bonds if unsaturated
            if (unsaturated)
            {
                MakeRingUnsaturated(newAtomPlacements);
            }

            newAtomPlacements[0].ExistingAtom.Parent.Refresh();

            Action undo = () =>
            {
                newAtomPlacements[0].ExistingAtom.Parent.Refresh();
                SelectedItems.Clear();
            };
            Action redo = () =>
            {
                newAtomPlacements[0].ExistingAtom.Parent.Refresh();
            };

            UndoManager.RecordAction(undo, redo, "Molecule refresh");
            UndoManager.EndUndoBlock();
        }

        public void DeleteMolecule(Molecule mol)
        {
            UndoManager.BeginUndoBlock();

            Model.Model theModel = mol.Model;
            var atomList = mol.Atoms.ToList();
            var bondList = mol.Bonds.ToList();

            bool isTopLevel = UndoManager.TransactionLevel == 1;

            Action redoAction = () =>
            {
                mol.Parent = null;
                theModel.Molecules.Remove(mol);
                RemoveFromSelection(mol);
            };

            Action undoAction = () =>
            {
                mol.Parent = theModel;
                theModel.Molecules.Add(mol);
                //AddToSelection(mol);
            };

            redoAction();

            UndoManager.RecordAction(undoAction, redoAction);
            UndoManager.EndUndoBlock();
        }

        public void AddHydrogens()
        {
            List<Atom> targetAtoms = new List<Atom>();
            var mols = SelectedItems.OfType<Molecule>().ToList();
            if (mols.Any())
            {
                foreach (var mol in mols)
                {
                    foreach (var atom in mol.AllAtoms)
                    {
                        if (atom.ImplicitHydrogenCount > 0)
                        {
                            targetAtoms.Add(atom);
                        }
                    }
                }
            }
            else
            {
                foreach (var atom in AllAtoms)
                {
                    if (atom.ImplicitHydrogenCount > 0)
                    {
                        targetAtoms.Add(atom);
                    }
                }
            }

            //if (targetAtoms.Any())
            //{
            //    List<NewAtomPlacement> newAtomPlacements = new List<NewAtomPlacement>();
            //    foreach (var atom in targetAtoms)
            //    {
            //        double seperation = 90.0;
            //        if (atom.Bonds.Count > 1)
            //        {
            //            seperation = 30.0;
            //        }

            //        int hydrogenCount = atom.ImplicitHydrogenCount;
            //        var vector = atom.BalancingVector;
            //        switch (hydrogenCount)
            //        {
            //            case 1:
            //                // Use balancing vector as is
            //                break;

            //            case 2:
            //                Matrix matrix1 = new Matrix();
            //                matrix1.Rotate(-seperation / 2);
            //                vector = vector * matrix1;
            //                break;

            //            case 3:
            //                Matrix matrix2 = new Matrix();
            //                matrix2.Rotate(-seperation);
            //                vector = vector * matrix2;
            //                break;

            //            case 4:
            //                // Use default balancing vector (Screen.North) as is
            //                break;
            //        }

            //        Matrix matrix3 = new Matrix();
            //        matrix3.Rotate(seperation);

            //        for (int i = 0; i < hydrogenCount; i++)
            //        {
            //            if (i > 0)
            //            {
            //                vector = vector * matrix3;
            //            }

            //            var x = new NewAtomPlacement();
            //            x.ExistingAtom = atom;
            //            x.Position = atom.Position +
            //                         vector * (Model.XamlBondLength * Globals.ExplicitHydrogenBondPercentage);
            //            newAtomPlacements.Add(x);
            //        }
            //    }

            //    //UndoManager.BeginUndoBlock();

            //    //List<Atom> addedAtoms = new List<Atom>();
            //    //List<Molecule> parents = new List<Molecule>();

            //    //Action undoAction = () =>
            //    //{
            //    //    foreach (var atom in addedAtoms)
            //    //    {
            //    //        foreach (var parent in parents)
            //    //        {
            //    //            if (parent.Atoms.Contains(atom))
            //    //            {
            //    //                parent.Atoms.Remove(atom);
            //    //            }
            //    //        }
            //    //    }
            //    //};

            //    //Action redoAction = () =>
            //    //{
            //    //    int idx = 0;
            //    //    foreach (var placement in newAtomPlacements)
            //    //    {
            //    //        if (!parents.Contains(placement.ExistingAtom.Parent))
            //    //        {
            //    //            parents.Add(placement.ExistingAtom.Parent);
            //    //        }
            //    //        addedAtoms.Add(AddAtomChain(placement.ExistingAtom, placement.Position, ClockDirections.Nothing, Globals.PeriodicTable.H));
            //    //    }
            //    //};

            //    //UndoManager.RecordAction(undoAction, redoAction, "Add Explicit Hydrogens");

            //    //redoAction();

            //    //UndoManager.EndUndoBlock();
            //}

            if (targetAtoms.Any())
            {
                List<Atom> newAtoms = new List<Atom>();
                List<Bond> newBonds = new List<Bond>();
                Dictionary<string, Molecule> parents = new Dictionary<string, Molecule>();
                foreach (var atom in targetAtoms)
                {
                    double seperation = 90.0;
                    if (atom.Bonds.Count > 1)
                    {
                        seperation = 30.0;
                    }

                    int hydrogenCount = atom.ImplicitHydrogenCount;
                    var vector = atom.BalancingVector;
                    switch (hydrogenCount)
                    {
                        case 1:
                            // Use balancing vector as is
                            break;

                        case 2:
                            Matrix matrix1 = new Matrix();
                            matrix1.Rotate(-seperation / 2);
                            vector = vector * matrix1;
                            break;

                        case 3:
                            Matrix matrix2 = new Matrix();
                            matrix2.Rotate(-seperation);
                            vector = vector * matrix2;
                            break;

                        case 4:
                            // Use default balancing vector (Screen.North) as is
                            break;
                    }

                    Matrix matrix3 = new Matrix();
                    matrix3.Rotate(seperation);

                    for (int i = 0; i < hydrogenCount; i++)
                    {
                        if (i > 0)
                        {
                            vector = vector * matrix3;
                        }

                        var aa = new Atom
                        {
                            Element = Globals.PeriodicTable.H,
                            Position = atom.Position + vector * (Model.XamlBondLength * Globals.ExplicitHydrogenBondPercentage)
                        };
                        newAtoms.Add(aa);
                        if (!parents.ContainsKey(aa.Id))
                        {
                            parents.Add(aa.Id, atom.Parent);
                        }
                        var bb = new Bond
                        {
                            StartAtom = atom,
                            EndAtom = aa,
                            Stereo = BondStereo.None,
                            Order = "S"
                        };
                        newBonds.Add(bb);
                        if (!parents.ContainsKey(bb.Id))
                        {
                            parents.Add(bb.Id, atom.Parent);
                        }
                    }
                }

                UndoManager.BeginUndoBlock();

                Action undoAction = () =>
                {
                    //foreach (var atom in newAtoms)
                    //{
                    //    foreach (var bond in newBonds)
                    //    {
                    //        foreach (var atomBond in atom.Bonds.ToList())
                    //        {
                    //            if (atomBond.Id.Equals(bond.Id))
                    //            {
                    //                atomBond.StartAtom = null;
                    //                atomBond.EndAtom = null;
                    //            }
                    //        }
                    //        atom.Bonds.Remove(bond);
                    //    }
                    //}
                    foreach (var bond in newBonds)
                    {
                        // BUG: Removing a bond does not remove it from an Atom's Bond Collection
                        bond.Parent.Bonds.Remove(bond);
                    }
                    foreach (var atom in targetAtoms)
                    {
                        foreach (var bond in newBonds)
                        {
                            if (atom.Bonds.Contains(bond))
                            {
                                atom.Bonds.Remove(bond);
                            }
                        }
                    }
                    foreach (var atom in newAtoms)
                    {
                        atom.Parent.Atoms.Remove(atom);
                    }
                    SelectedItems.Clear();
                };

                Action redoAction = () =>
                {
                    foreach (var atom in newAtoms)
                    {
                        parents[atom.Id].Atoms.Add(atom);
                    }
                    foreach (var bond in newBonds)
                    {
                        parents[bond.Id].Bonds.Add(bond);
                    }
                    SelectedItems.Clear();
                };

                UndoManager.RecordAction(undoAction, redoAction);

                redoAction();

                UndoManager.EndUndoBlock();
            }
        }

        public void RemoveHydrogens()
        {
            List<Atom> targetAtoms = new List<Atom>();
            List<Bond> targetBonds = new List<Bond>();
            Dictionary<string, Molecule> parents = new Dictionary<string, Molecule>();

            var mols = SelectedItems.OfType<Molecule>().ToList();
            if (mols.Any())
            {
                foreach (var mol in mols)
                {
                    var allHydrogens = mol.AllAtoms.Where(a => a.Element.Symbol.Equals("H")).ToList();
                    if (allHydrogens.Any())
                    {
                        foreach (var hydrogen in allHydrogens)
                        {
                            // Terminal Atom?
                            if (hydrogen.Degree == 1)
                            {
                                // Not Stereo
                                if (hydrogen.Bonds[0].Stereo == BondStereo.None)
                                {
                                    if (!parents.ContainsKey(hydrogen.Id))
                                    {
                                        parents.Add(hydrogen.Id, hydrogen.Parent);
                                    }
                                    targetAtoms.Add(hydrogen);
                                    if (!parents.ContainsKey(hydrogen.Bonds[0].Id))
                                    {
                                        parents.Add(hydrogen.Bonds[0].Id, hydrogen.Parent);
                                    }
                                    targetBonds.Add(hydrogen.Bonds[0]);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var allHydrogens = AllAtoms.Where(a => a.Element.Symbol.Equals("H")).ToList();
                if (allHydrogens.Any())
                {
                    foreach (var hydrogen in allHydrogens)
                    {
                        // Terminal Atom?
                        if (hydrogen.Degree == 1)
                        {
                            // Not Stereo
                            if (hydrogen.Bonds[0].Stereo == BondStereo.None)
                            {
                                if (!parents.ContainsKey(hydrogen.Id))
                                {
                                    parents.Add(hydrogen.Id, hydrogen.Parent);
                                }
                                targetAtoms.Add(hydrogen);
                                if (!parents.ContainsKey(hydrogen.Bonds[0].Id))
                                {
                                    parents.Add(hydrogen.Bonds[0].Id, hydrogen.Parent);
                                }
                                targetBonds.Add(hydrogen.Bonds[0]);
                            }
                        }
                    }
                }
            }

            if (targetAtoms.Any())
            {
                UndoManager.BeginUndoBlock();
                Action undoAction = () =>
                {
                    foreach (var atom in targetAtoms)
                    {
                        parents[atom.Id].Atoms.Add(atom);
                    }
                    foreach (var bond in targetBonds)
                    {
                        parents[bond.Id].Bonds.Add(bond);
                    }
                };

                Action redoAction = () =>
                {
                    //foreach (var atom in targetAtoms)
                    //{
                    //    foreach (var bond in targetBonds)
                    //    {
                    //        foreach (var atomBond in atom.Bonds.ToList())
                    //        {
                    //            if (atomBond.Id.Equals(bond.Id))
                    //            {
                    //                atomBond.StartAtom = null;
                    //                atomBond.EndAtom = null;
                    //            }
                    //        }
                    //        atom.Bonds.Remove(bond);
                    //    }
                    //}
                    foreach (var bond in targetBonds)
                    {
                        // BUG: Removing a bond does not remove it from an Atom's Bond Collection
                        bond.StartAtom = null;
                        bond.EndAtom = null;
                        bond.Parent.Bonds.Remove(bond);
                    }
                    foreach (var atom in targetAtoms)
                    {
                        atom.Parent.Atoms.Remove(atom);
                    }
                };

                UndoManager.RecordAction(undoAction, redoAction);
                UndoManager.EndUndoBlock();

                redoAction();
            }
            SelectedItems.Clear();
        }

        public override Rect BoundingBox
        {
            get
            {
                return CalcBoundingBox();
            }
        }

        public bool SingleMolSelected
        {
            get { return SelectedItems.Count == 1 && SelectedItems[0] is Molecule; }
        }

        public void FlipMolecule(Molecule selMolecule, bool flipVertically, bool flipStereo)
        {
            Point centroid = selMolecule.Centroid;
            int scaleX = 1, scaleY = 1;

            if (flipVertically)
            {
                scaleY = -1;
            }
            else
            {
                scaleX = -1;
            }
            ScaleTransform flipTransform = new ScaleTransform(scaleX, scaleY, centroid.X, centroid.Y);

            UndoManager.BeginUndoBlock();

            foreach (Atom atomToFlip in selMolecule.Atoms)
            {
                Point currentPos = atomToFlip.Position;
                Point newPos = flipTransform.Transform(currentPos);
                Action undo = () =>
                {
                    atomToFlip.Position = currentPos;
                };
                Action redo = () =>
                {
                    atomToFlip.Position = newPos;
                };
                atomToFlip.Position = newPos;

                UndoManager.RecordAction(undo, redo, "Flip Atom");
            }

            UndoManager.EndUndoBlock();
        }

        public void AddToSelection(object thingToAdd)
        {
            var parent = (thingToAdd as Atom)?.Parent ?? (thingToAdd as Bond)?.Parent;

            if (!SelectedItems.Contains(parent))
            {
                AddToSelection(new List<object> { thingToAdd });
            }
        }

        public void RemoveFromSelection(object thingToRemove)
        {
            RemoveFromSelection(new List<object> { thingToRemove });
        }

        public void AddToSelection(List<object> thingsToAdd)
        {
            //grab all the molecules that contain selected objects
            var molsInSelection = new HashSet<object>(SelectedItems.Where(o => (o is Atom | o is Bond))
                .Select((dynamic obj) => obj.Parent as Molecule).Distinct());
            foreach (object o in thingsToAdd)
            {
                if (o is Atom atom)
                {
                    Molecule parent = atom.Parent;

                    if (atom.Singleton)
                    {
                        SelectedItems.Add(parent);
                    }
                    else if (molsInSelection.Contains(atom.Parent))
                    {
                        if (SelectedItems.Contains(parent))
                        {
                            return;//the molecule itself is selected
                        }

                        var allObjects = new HashSet<object>(parent.Atoms);
                        allObjects.Add(parent.Bonds);

                        var selobjects =
                            new HashSet<object>(SelectedItems.OfType<Atom>().Where(a => a.Parent == parent)) { atom };
                        selobjects.Add(parent.Bonds);
                        if (allObjects.SetEquals(selobjects))
                        {
                            foreach (Atom a in parent.Atoms)
                            {
                                SelectedItems.Remove(a);
                            }

                            foreach (Bond b in parent.Bonds)
                            {
                                SelectedItems.Remove(b);
                            }

                            SelectedItems.Add(parent);
                        }
                        else
                        {
                            SelectedItems.Add(atom);
                        }
                    }
                    else
                    {
                        SelectedItems.Add(atom);
                    }
                }
                else if (o is Bond bond)
                {
                    if (molsInSelection.Contains(bond.Parent))
                    {
                        Molecule parent = bond.Parent;

                        if (SelectedItems.Contains(parent))
                        {
                            return;//the molecule itself is selected
                        }

                        var allObjects = new HashSet<object>(parent.Atoms);
                        allObjects.Add(parent.Bonds);

                        var selobjects =
                            new HashSet<object>(SelectedItems.OfType<Atom>().Where(a => a.Parent == parent)) { bond };

                        selobjects.Add(parent.Bonds);

                        if (allObjects.SetEquals(selobjects))
                        {
                            foreach (Bond b in parent.Bonds)
                            {
                                SelectedItems.Remove(b);
                            }

                            foreach (Atom a in parent.Atoms)
                            {
                                SelectedItems.Remove(a);
                            }

                            SelectedItems.Add(parent);
                        }
                        else
                        {
                            SelectedItems.Add(bond);
                        }
                    }
                    else
                    {
                        SelectedItems.Add(bond);
                    }
                }
                else if (o is Molecule)
                {
                    SelectedItems.Add(o);
                }
            }
        }

        public void RemoveFromSelection(List<object> thingsToAdd)
        {
            //grab all the molecules that contain selected objects
            var molsInSelection = SelectedItems.Where(o => (o is Atom | o is Bond))
                .Select((dynamic obj) => obj.Parent as Molecule).Distinct();
            foreach (object o in thingsToAdd)
            {
                if (o is Atom atom)
                {
                    if (atom.Singleton)
                    {
                        SelectedItems.Remove(atom.Parent);
                    }
                    if (SelectedItems.Contains(atom))
                    {
                        SelectedItems.Remove(atom);
                    }
                }
                else if (o is Bond)
                {
                    var bond = (Bond)o;
                    if (SelectedItems.Contains(bond))
                    {
                        SelectedItems.Remove(bond);
                    }
                }
                else if (o is Molecule mol)
                {
                    if (SelectedItems.Contains(mol))
                    {
                        SelectedItems.Remove(mol);
                    }
                }
            }
        }
    }
}