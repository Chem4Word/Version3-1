// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Enums;
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
        private List<BondLengthOption> _bondLengthOptions = new List<BondLengthOption>();
        private BondLengthOption _selectedBondLengthOption;
        private int? _selectedBondOptionId;

        #endregion Fields

        #region Properties

        public List<BondLengthOption> BondLengthOptions
        {
            get { return _bondLengthOptions; }
        }

        public BondLengthOption SelectedBondLengthOption
        {
            get { return _selectedBondLengthOption; }
            set { _selectedBondLengthOption = value; }
        }

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
            UndoManager.BeginUndoBlock();
            var bondOption = _bondOptions[_selectedBondOptionId.Value];
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

        public List<BondOption> SelectedBondOptions
        {
            get
            {
                var dictionary = new Dictionary<string, BondOption>();
                var selectedBondTypes = new List<BondOption>();
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
                _bondLengthOptions.Add(option);
                if (Math.Abs(i * Globals.ScaleFactorForXaml - Model.XamlBondLength) < 2.5 * Globals.ScaleFactorForXaml)
                {
                    _selectedBondLengthOption = option;
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
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddSelectionAdorners(newObjects);
                    break;

                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Remove:
                    RemoveSelectionAdorners(oldObject);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    RemoveAllAdorners();
                    break;
            }

            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedBondOptionId));
            OnPropertyChanged(nameof(SelectionType));

            CopyCommand.RaiseCanExecChanged();
            CutCommand.RaiseCanExecChanged();
            DeleteCommand.RaiseCanExecChanged();
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
                    layer.Remove(SelectionAdorners[oldObject]);
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
                        RemoveAdorners(atom.Parent);
                        MoleculeSelectionAdorner molAdorner = new MoleculeSelectionAdorner(DrawingSurface, atom.Parent, this);
                        SelectionAdorners[newObject] = molAdorner;
                    }
                }

                if (newObject is Bond)
                {
                    BondSelectionAdorner bondAdorner = new BondSelectionAdorner(DrawingSurface, (newObject as Bond));
                    SelectionAdorners[newObject] = bondAdorner;
                    bondAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                }

                if (newObject is Molecule)
                {
                    MoleculeSelectionAdorner molAdorner =
                        new MoleculeSelectionAdorner(DrawingSurface, (newObject as Molecule), this);
                    SelectionAdorners[newObject] = molAdorner;
                    molAdorner.DragResizeCompleted += MolAdorner_DragResizeCompleted;
                    molAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                }
            }
        }

        private void MolAdorner_DragResizeCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            var movedMolecule = (sender as MoleculeSelectionAdorner).AdornedMolecule;
            SelectedItems.Remove(movedMolecule);

            //and add in a new one
            SelectedItems.Add(movedMolecule);
        }

        private void SelAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (sender is AtomSelectionAdorner)
                {
                    Molecule mol = (sender as AtomSelectionAdorner).AdornedAtom.Parent as Molecule;
                    RemoveAdorners(mol);
                    SelectedItems.Add(mol);
                }
                else if (sender is BondSelectionAdorner)
                {
                    Molecule mol = (sender as BondSelectionAdorner).AdornedBond.Parent as Molecule;
                    RemoveAdorners(mol);
                    SelectedItems.Add(mol);
                }
                else if (sender is MoleculeSelectionAdorner)
                {
                    Molecule mol = (sender as MoleculeSelectionAdorner).AdornedMolecule.Parent as Molecule;
                }
            }
        }

        private void RemoveAdorners(Molecule atomParent)
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

        public void DeleteAtom(Atom atom)
        {
            UndoManager.BeginUndoBlock();
            var bondlist = atom.Bonds.ToList();
            foreach (Bond bond in bondlist)
            {
                DeleteBond(bond);
            }

            SelectedItems.Remove(atom);

            var parent = atom.Parent;

            Action undoAction = () =>
            {
                parent.Atoms.Add(atom);
            };
            Action redoAction = () =>
            {
                parent.Atoms.Remove(atom);
            };

            UndoManager.RecordAction(undoAction, redoAction);
            atom.Parent.Atoms.Remove(atom);

            UndoManager.EndUndoBlock();
        }

        public void DeleteBond(Bond bond)
        {
            UndoManager.BeginUndoBlock();
            var a1 = bond.StartAtom;
            var a2 = bond.EndAtom;

            Action redoAction = () =>
            {
                bond.StartAtom = null;
                bond.EndAtom = null;
                bond.Parent.RebuildRings();
            };

            Action undoAction = () =>
            {
                bond.StartAtom = a1;
                bond.EndAtom = a2;
                bond.Parent.RebuildRings();
            };
            SelectedItems.Remove(bond);
            UndoManager.RecordAction(undoAction, redoAction);

            bond.StartAtom = null;
            bond.EndAtom = null;

            Molecule parent = bond.Parent;

            if (parent != null)
            {
                undoAction = () =>
                {
                    parent.Bonds.Add(bond);
                };
                redoAction = () =>
                {
                    parent.Bonds.Remove(bond);
                };

                UndoManager.RecordAction(undoAction, redoAction);
                parent.Bonds.Remove(bond);
            }

            UndoManager.EndUndoBlock();
        }

        public void AddAtomChain(Atom lastAtom, Point newAtomPos)
        {
            Atom newAtom = new Atom();
            newAtom.Element = _selectedElement;
            newAtom.Position = newAtomPos;

            if (lastAtom != null)
            {
                UndoManager.BeginUndoBlock();

                Molecule currentMol = lastAtom.Parent;

                Action undo = () =>
                {
                    currentMol.Atoms.Remove(newAtom);
                };
                Action redo = () =>
                {
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
                redo.Invoke();

                UndoManager.RecordAction(undo, redo);

                Action undo2 = () =>
                {
                    _currentMol.Atoms.Remove(newAtom);
                };
                Action redo2 = () =>
                {
                    _currentMol.Atoms.Add(newAtom);
                };
                UndoManager.RecordAction(undo2, redo2);

                redo2.Invoke();

                UndoManager.EndUndoBlock();
            }
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
                mol.RebuildRings();
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
                atom.Position = newPosition;
            }

            UndoManager.EndUndoBlock();
        }
    }
}