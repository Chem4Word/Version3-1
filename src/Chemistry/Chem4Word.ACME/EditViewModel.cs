// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Adorners.Selectors;
using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Commands;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using static Chem4Word.Model2.Helpers.Globals;
using Constants = Chem4Word.ACME.Resources.Constants;

namespace Chem4Word.ACME
{
    public class EditViewModel : ViewModel, INotifyPropertyChanged
    {
        public event EventHandler<WpfEventArgs> OnFeedbackChange;

        #region Fields

        public readonly Dictionary<object, Adorner> SelectionAdorners = new Dictionary<object, Adorner>();
        public MultiAtomBondAdorner MultiAdorner { get; private set; }
        private Dictionary<int, BondOption> _bondOptions = new Dictionary<int, BondOption>();
        private int? _selectedBondOptionId;

        #endregion Fields

        #region Properties

        public bool Loading { get; set; }

        public string CurrentBondOrder
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Order; }
        }

        public BondStereo CurrentStereo
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Stereo.Value; }
        }

        public double EditBondThickness
        {
            get { return BondThickness * DefaultBondLineFactor; }
        }

        public double EditHalfBondThickness
        {
            get { return EditBondThickness / 2; }
        }

        internal void SendStatus(string value)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = value;
            OnFeedbackChange?.Invoke(this, args);
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

                return result;
            }
        }

        private ObservableCollection<object> _selectedItems;
        public ReadOnlyObservableCollection<object> SelectedItems { get; }

        public UndoHandler UndoManager { get; }

        private double _currentBondLength;

        public double CurrentBondLength
        {
            get { return _currentBondLength; }
            set
            {
                _currentBondLength = value;
                OnPropertyChanged();
                var scaled = value * ScaleFactorForXaml;
                // Decide if we need to rescale to current drawing
                if (!Loading && Math.Abs(Model.MeanBondLength - scaled) > 2.5)
                {
                    SetAverageBondLength(scaled);
                }
            }
        }

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

                if (selElements.Count == 0) //nothing selected so return last value selected
                {
                    return _selectedElement;
                }

                return null;
            }

            set
            {
                _selectedElement = value;

                var selAtoms = SelectedItems.OfType<Atom>().ToList();
                if (value != null)
                {
                    SetElement(value, selAtoms);
                }

                OnPropertyChanged();
            }
        }

        public void SetElement(ElementBase value, List<Atom> selAtoms)
        {
            if (selAtoms.Any())
            {
                UndoManager.BeginUndoBlock();

                Action undo, redo;
                foreach (Atom selectedAtom in selAtoms)
                {
                    if (selectedAtom.Element != value)
                    {
                        redo = () => { selectedAtom.Element = value; };
                        var lastElement = selectedAtom.Element;

                        undo = () => { selectedAtom.Element = lastElement; };
                        UndoManager.RecordAction(undo, redo, $"Set Element to {value?.Symbol ?? "null"}");
                        selectedAtom.Element = value;
                        selectedAtom.UpdateVisual();
                        foreach (Bond bond in selectedAtom.Bonds)
                        {
                            bond.UpdateVisual();
                        }
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
                var singletons = from Molecule m in SelectedItems.OfType<Molecule>()
                                 where m.Atoms.Count == 1
                                 select m;

                var allSelAtoms = (from Atom a in SelectedItems.OfType<Atom>()
                                   select a)
                    .Union<Atom>(
                        from Molecule m in singletons
                        from Atom a1 in m.Atoms.Values
                        select a1);
                var elements = (from selAtom in allSelAtoms
                                select (ElementBase)selAtom.Element).Distinct();
                return elements.ToList();
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

        public Editor EditorControl { get; set; }
        public EditorCanvas CurrentEditor { get; set; }

        public Utils.ClipboardMonitor ClipboardMonitor { get; }

        #endregion Properties

        private BaseEditBehavior _activeMode;
        public List<string> Used1DProperties { get; set; }

        public BaseEditBehavior ActiveMode
        {
            get { return _activeMode; }
            set
            {
                if (_activeMode != null)
                {
                    _activeMode.Detach();
                    _activeMode = null;
                }

                _activeMode = value;
                if (_activeMode != null)
                {
                    _activeMode.Attach(CurrentEditor);
                    SendStatus(_activeMode.CurrentStatus);
                }
                OnPropertyChanged();
            }
        }

        private ObservableCollection<AtomOption> _atomOptions;

        public ObservableCollection<AtomOption> AtomOptions
        {
            get { return _atomOptions; }
            set { _atomOptions = value; }
        }

        #region Commands

        public AddAtomCommand AddAtomCommand { get; set; }
        public UndoCommand UndoCommand { get; set; }
        public RedoCommand RedoCommand { get; set; }
        public CopyCommand CopyCommand { get; set; }
        public CutCommand CutCommand { get; set; }
        public PasteCommand PasteCommand { get; set; }
        public FlipVerticalCommand FlipVerticalCommand { get; set; }
        public FlipHorizontalCommand FlipHorizontalCommand { get; set; }
        public AddHydrogensCommand AddHydrogensCommand { get; set; }
        public RemoveHydrogensCommand RemoveHydrogensCommand { get; set; }
        public FuseCommand FuseCommand { get; set; }
        public GroupCommand GroupCommand { get; set; }
        public UnGroupCommand UnGroupCommand { get; set; }
        public SettingsCommand SettingsCommand { get; set; }

        #endregion Commands

        #region Constructors

        public EditViewModel(Model model, EditorCanvas currentEditor, List<string> _used1DProperties) : base(model)
        {
            AtomOptions = new ObservableCollection<AtomOption>();
            Used1DProperties = _used1DProperties;

            LoadAtomOptions();
            _selectedItems = new ObservableCollection<object>();
            SelectedItems = new ReadOnlyObservableCollection<object>(_selectedItems);
            _selectedItems.CollectionChanged += SelectedItems_Changed;

            UndoManager = new UndoHandler(this);

            SetupCommands();
            CurrentEditor = currentEditor;
            ClipboardMonitor = new ClipboardMonitor();
            ClipboardMonitor.OnClipboardContentChanged += ClipboardMonitor_OnClipboardContentChanged;
            _selectedElement = Globals.PeriodicTable.C;

            _selectedBondOptionId = 1;

            LoadBondOptions();
        }

        private void SetupCommands()
        {
            RedoCommand = new RedoCommand(this);
            UndoCommand = new UndoCommand(this);
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
        }

        private void ClipboardMonitor_OnClipboardContentChanged(object sender, EventArgs e)
        {
            PasteCommand.RaiseCanExecChanged();
        }

        public void LoadAtomOptions()
        {
            ClearAtomOptions();
            LoadStandardAtomOptions();
            LoadModelAtomOptions();
            LoadModelFGs();
        }

        private void ClearAtomOptions()
        {
            int limit = AtomOptions.Count - 1;
            for (int i = limit; i >= 0; i--)
            {
                AtomOptions.RemoveAt(i);
            }
        }

        public void LoadAtomOptions(Element addition)
        {
            ClearAtomOptions();
            LoadStandardAtomOptions();
            LoadModelAtomOptions(addition);
            LoadModelFGs();
        }

        private void LoadModelFGs()
        {
            var modelFGs = (from a in Model.GetAllAtoms()
                            where a.Element is FunctionalGroup && !(from ao in AtomOptions
                                                                    select ao.Element).Contains(a.Element)
                            orderby a.SymbolText
                            select a.Element).Distinct();

            var newOptions = from mfg in modelFGs
                             select new AtomOption((mfg as FunctionalGroup));
            foreach (var newOption in newOptions)
            {
                AtomOptions.Add(newOption);
            }
        }

        private void LoadModelAtomOptions(Element addition = null)
        {
            var modelElements = (from a in Model.GetAllAtoms()
                                 where a.Element is Element && !(from ao in AtomOptions
                                                                 select ao.Element).Contains(a.Element)
                                 orderby a.SymbolText
                                 select a.Element).Distinct();

            var newOptions = from e in Globals.PeriodicTable.ElementsSource
                             join me in modelElements
                                 on e equals me
                             select new AtomOption(e);

            foreach (var newOption in newOptions)
            {
                AtomOptions.Add(newOption);
            }

            if (addition != null && !AtomOptions.Select(ao => ao.Element).Contains(addition))
            {
                AtomOptions.Add(new AtomOption(addition));
            }
        }

        private void LoadStandardAtomOptions()
        {
            foreach (var atom in Constants.StandardAtoms)
            {
                AtomOptions.Add(new AtomOption(Globals.PeriodicTable.Elements[atom]));
            }

            foreach (var fg in Constants.StandardFunctionalGroups)
            {
                AtomOptions.Add(new AtomOption(FunctionalGroupsDictionary[fg]));
            }
        }

        /// <summary>
        /// Loads up the bond options into the main dropdown
        /// </summary>
        private void LoadBondOptions()
        {
            var storedOptions = (BondOption[])CurrentEditor.FindResource("BondOptions");
            for (int i = 1; i <= storedOptions.Length; i++)
            {
                _bondOptions[i] = storedOptions[i - 1];
            }
        }

        #endregion Constructors

        #region Methods

        public void IncreaseBondOrder(Bond existingBond)
        {
            UndoManager.BeginUndoBlock();

            var stereo = existingBond.Stereo;
            var order = existingBond.Order;

            Action redo = () =>
                          {
                              existingBond.Stereo = BondStereo.None;
                              if (existingBond.Order == OrderZero)
                              {
                                  existingBond.Order = OrderSingle;
                                  existingBond.UpdateVisual();
                              }

                              if (existingBond.Order == OrderSingle)
                              {
                                  existingBond.Order = OrderDouble;
                                  existingBond.UpdateVisual();
                              }
                              else if (existingBond.Order == OrderDouble)
                              {
                                  existingBond.Order = OrderTriple;
                                  existingBond.UpdateVisual();
                              }
                              else if (existingBond.Order == OrderTriple)
                              {
                                  existingBond.Order = OrderSingle;
                                  existingBond.UpdateVisual();
                              }

                              existingBond.StartAtom.UpdateVisual();
                              existingBond.EndAtom.UpdateVisual();
                          };
            Action undo = () =>
                          {
                              existingBond.Stereo = stereo;
                              existingBond.Order = order;

                              existingBond.StartAtom.UpdateVisual();
                              existingBond.EndAtom.UpdateVisual();
                          };

            UndoManager.RecordAction(undo, redo);
            redo();

            UndoManager.EndUndoBlock();
        }

        public void DoTransform(Transform operation, List<Atom> transformedAtoms)
        {
            if (transformedAtoms.Any())
            {
                UndoManager.BeginUndoBlock();
                var inverse = operation.Inverse;
                Atom[] transformList = transformedAtoms.ToArray();
                //need an reference to the mol later
                Molecule transformedMol = transformedAtoms[0].Parent;

                Action undo = () =>
                              {
                                  _selectedItems.Clear();
                                  foreach (Atom atom in transformList)
                                  {
                                      atom.Position = inverse.Transform(atom.Position);
                                      atom.UpdateVisual();
                                  }

                                  transformedMol.RootMolecule.UpdateVisual();
                                  foreach (Atom o in transformList)
                                  {
                                      AddToSelection(o);
                                  }
                              };
                Action redo = () =>
                              {
                                  _selectedItems.Clear();
                                  foreach (Atom atom in transformList)
                                  {
                                      atom.Position = operation.Transform(atom.Position);
                                      atom.UpdateVisual();
                                  }

                                  transformedMol.RootMolecule.UpdateVisual();
                                  foreach (Atom o in transformList)
                                  {
                                      AddToSelection(o);
                                  }
                              };

                UndoManager.RecordAction(undo, redo);

                redo();

                UndoManager.EndUndoBlock();
            }
        }

        public void DoTransform(Transform operation, List<Molecule> toList)
        {
            UndoManager.BeginUndoBlock();
            var inverse = operation.Inverse;
            var rootMolecules = from m in toList
                                where m.RootMolecule == m
                                select m;
            Molecule[] transformList = rootMolecules.ToArray();

            Action undo = () =>
                          {
                              CurrentEditor.SuppressRedraw = true;
                              _selectedItems.Clear();
                              foreach (Molecule mol in transformList)
                              {
                                  mol.Transform((Transform)inverse);
                              }

                              CurrentEditor.SuppressRedraw = false;
                              foreach (Molecule mol in transformList)
                              {
                                  mol.UpdateVisual();
                              }

                              foreach (var o in transformList)
                              {
                                  AddToSelection(o);
                              }
                          };
            Action redo = () =>
                          {
                              _selectedItems.Clear();
                              foreach (Molecule mol in transformList)
                              {
                                  mol.Transform((Transform)operation);
                              }

                              CurrentEditor.SuppressRedraw = false;
                              foreach (Molecule mol in transformList)
                              {
                                  mol.UpdateVisual();
                              }

                              foreach (var o in transformList)
                              {
                                  AddToSelection(o);
                              }
                          };

            UndoManager.RecordAction(undo, redo);

            redo();

            UndoManager.EndUndoBlock();
        }

        public void SwapBondDirection(Bond parentBond)
        {
            UndoManager.BeginUndoBlock();

            var startAtom = parentBond.StartAtom;
            var endAtom = parentBond.EndAtom;

            var parentMol = parentBond.Parent;

            Action undo = () =>
                          {
                              parentBond.StartAtomInternalId = startAtom.InternalId;
                              parentBond.EndAtomInternalId = endAtom.InternalId;
                              parentBond.UpdateVisual();
                          };

            Action redo = () =>
                          {
                              parentBond.EndAtomInternalId = startAtom.InternalId;
                              parentBond.StartAtomInternalId = endAtom.InternalId;
                              parentBond.UpdateVisual();
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

        public void AddNewBond(Atom a, Atom b, Molecule mol, string order = null, BondStereo? stereo = null)
        {
            void RefreshAtoms(Atom startAtom, Atom endAtom)
            {
                startAtom.UpdateVisual();
                endAtom.UpdateVisual();
                foreach (Bond bond in startAtom.Bonds)
                {
                    bond.UpdateVisual();
                }

                foreach (Bond bond in endAtom.Bonds)
                {
                    bond.UpdateVisual();
                }
            }

            //keep a handle on some current properties
            int theoreticalRings = mol.TheoreticalRings;
            if (stereo == null)
            {
                stereo = CurrentStereo;
            }

            if (order == null)
            {
                order = CurrentBondOrder;
            }

            //stash the current molecule properties
            MoleculePropertyBag mpb = new MoleculePropertyBag();
            mpb.Store(mol);

            Bond newbond = new Bond();

            newbond.Stereo = stereo.Value;
            newbond.Order = order;
            newbond.Parent = mol;

            UndoManager.BeginUndoBlock();

            Action undo = () =>
                          {
                              Atom startAtom = newbond.StartAtom;
                              Atom endAtom = newbond.EndAtom;
                              mol.RemoveBond(newbond);
                              newbond.Parent = null;
                              if (theoreticalRings != mol.TheoreticalRings)
                              {
                                  mol.RebuildRings();
                                  theoreticalRings = mol.TheoreticalRings;
                              }

                              RefreshAtoms(startAtom, endAtom);

                              mpb.Restore(mol);
                          };
            Action redo = () =>
                          {
                              newbond.StartAtomInternalId = a.InternalId;
                              newbond.EndAtomInternalId = b.InternalId;
                              newbond.Parent = mol;
                              mol.AddBond(newbond);
                              if (theoreticalRings != mol.TheoreticalRings)
                              {
                                  mol.RebuildRings();
                                  theoreticalRings = mol.TheoreticalRings;
                              }

                              RefreshAtoms(newbond.StartAtom, newbond.EndAtom);
                              newbond.UpdateVisual();
                              mol.ClearProperties();
                          };

            UndoManager.RecordAction(undo, redo);

            UndoManager.EndUndoBlock();

            redo();
        }

        /// <summary>
        /// Adds a new atom to an existing molecule separated by one bond
        /// </summary>
        /// <param name="lastAtom">previous atom to which the new one is bonded.  can be null</param>
        /// <param name="newAtomPos">Position of new atom</param>
        /// <param name="dir">ClockDirection in which to add the atom</param>
        /// <param name="elem">Element of atom (can be a FunctionalGroup).  eefaults to current selection</param>
        /// <param name="bondOrder"></param>
        /// <param name="stereo"></param>
        /// <returns></returns>
        public Atom AddAtomChain(Atom lastAtom, Point newAtomPos, ClockDirections dir, ElementBase elem = null,
                                 string bondOrder = null, BondStereo? stereo = null)
        {
            //create the new atom based on the current selection
            Atom newAtom = new Atom { Element = elem ?? _selectedElement, Position = newAtomPos };

            //the tag stores sprout directions chosen for the chain
            object tag = null;

            if (dir != ClockDirections.Nothing)
            {
                tag = dir;
            }

            //stash the last sprout direction
            object oldDir = lastAtom?.Tag;

            //are we drawing an isolated atom?
            if (lastAtom != null) //then it's isolated
            {
                UndoManager.BeginUndoBlock();

                Molecule currentMol = lastAtom.Parent;

                Action undo = () =>
                              {
                                  lastAtom.Tag = oldDir;
                                  currentMol.RemoveAtom(newAtom);
                                  newAtom.Parent = null;
                              };
                Action redo = () =>
                              {
                                  lastAtom.Tag = tag; //save the last sprouted direction in the tag object
                                  newAtom.Parent = currentMol;
                                  currentMol.AddAtom(newAtom);
                                  newAtom.UpdateVisual();
                              };
                UndoManager.RecordAction(undo, redo);

                redo();

                AddNewBond(lastAtom, newAtom, currentMol, bondOrder, stereo);
                lastAtom.UpdateVisual();
                newAtom.UpdateVisual();
                foreach (Bond lastAtomBond in lastAtom.Bonds)
                {
                    lastAtomBond.UpdateVisual();
                }

                UndoManager.EndUndoBlock();
            }
            else
            {
                UndoManager.BeginUndoBlock();

                var _currentMol = new Molecule();

                Action undo = () =>
                              {
                                  Model.RemoveMolecule(_currentMol);
                                  _currentMol.Parent = null;
                              };
                Action redo = () =>
                              {
                                  _currentMol.Parent = Model;
                                  Model.AddMolecule(_currentMol);
                              };
                redo();

                UndoManager.RecordAction(undo, redo);

                Action undo2 = () =>
                               {
                                   newAtom.Tag = null;
                                   _currentMol.RemoveAtom(newAtom);
                                   newAtom.Parent = null;
                               };
                Action redo2 = () =>
                               {
                                   newAtom.Parent = _currentMol;
                                   _currentMol.AddAtom(newAtom);
                                   newAtom.Tag = tag;
                               };
                UndoManager.RecordAction(undo2, redo2);

                redo2();

                UndoManager.EndUndoBlock();
            }

            return newAtom;
        }

        public void SetAverageBondLength(double newLength)
        {
            UndoManager.BeginUndoBlock();
            double currentLength = Model.MeanBondLength;

            var centre = new Point(Model.BoundingBoxWithFontSize.Left + Model.BoundingBoxWithFontSize.Width / 2,
                                   Model.BoundingBoxWithFontSize.Top + Model.BoundingBoxWithFontSize.Height / 2);

            Action redoAction = () =>
                                {
                                    Model.ScaleToAverageBondLength(newLength, centre);
                                    Model.XamlBondLength = newLength;
                                    RefreshMolecules(Model.Molecules.Values.ToList());
                                    Loading = true;
                                    CurrentBondLength = newLength / ScaleFactorForXaml;
                                    Loading = false;
                                };
            Action undoAction = () =>
                                {
                                    Model.ScaleToAverageBondLength(currentLength, centre);
                                    Model.XamlBondLength = currentLength;
                                    RefreshMolecules(Model.Molecules.Values.ToList());
                                    Loading = true;
                                    CurrentBondLength = currentLength / ScaleFactorForXaml;
                                    Loading = false;
                                };

            UndoManager.RecordAction(undoAction, redoAction);

            redoAction();

            UndoManager.EndUndoBlock();
        }

        public void CopySelection()
        {
            CMLConverter converter = new CMLConverter();
            Model tempModel = new Model();
            //if selection isn't null
            if (SelectedItems.Count > 0)
            {
                HashSet<Atom> copiedAtoms = new HashSet<Atom>();
                //iterate through the active selection
                foreach (object selectedItem in SelectedItems)
                {
                    //if the current selection is a molecule
                    if (selectedItem is Molecule molecule)
                    {
                        tempModel.AddMolecule(molecule);
                    }
                    else if (selectedItem is Atom atom)
                    {
                        copiedAtoms.Add(atom);
                    }
                }

                //keep track of added atoms
                Dictionary<string, Atom> aa = new Dictionary<string, Atom>();
                //while the atom copy list isn't empty
                while (copiedAtoms.Any())
                {
                    Atom seedAtom = copiedAtoms.First();
                    //create a new molecule
                    Molecule newMol = new Molecule();
                    Molecule oldParent = seedAtom.Parent;

                    HashSet<Atom> thisAtomGroup = new HashSet<Atom>();

                    //Traverse the molecule, excluding atoms that have been processed and bonds that aren't in the list
                    oldParent.TraverseBFS(seedAtom,
                                          atom =>
                                          {
                                              copiedAtoms.Remove(atom);

                                              thisAtomGroup.Add(atom);
                                          },
                                          atom2 =>
                                          {
                                              return !thisAtomGroup.Contains(atom2) & copiedAtoms.Contains(atom2);
                                          });

                    //add the atoms and bonds to the new molecule

                    foreach (Atom thisAtom in thisAtomGroup)
                    {
                        Atom a = new Atom
                        {
                            Id = thisAtom.Id,
                            Position = thisAtom.Position,
                            Element = thisAtom.Element,
                            FormalCharge = thisAtom.FormalCharge,
                            IsotopeNumber = thisAtom.IsotopeNumber,
                            ShowSymbol = thisAtom.ShowSymbol,
                            Parent = newMol
                        };

                        newMol.AddAtom(a);
                        aa[a.Id] = a;
                    }

                    Bond thisBond = null;
                    List<Bond> copiedBonds = new List<Bond>();
                    foreach (Atom startAtom in thisAtomGroup)
                    {
                        foreach (Atom otherAtom in thisAtomGroup)
                        {
                            if ((thisBond = startAtom.BondBetween(otherAtom)) != null & !copiedBonds.Contains(thisBond))
                            {
                                copiedBonds.Add(thisBond);
                                Atom s = aa[thisBond.StartAtom.Id];
                                Atom e = aa[thisBond.EndAtom.Id];
                                Bond b = new Bond(s, e)
                                {
                                    Id = thisBond.Id,
                                    Order = thisBond.Order,
                                    Stereo = thisBond.Stereo,
                                    ExplicitPlacement = thisBond.ExplicitPlacement,
                                    Parent = newMol
                                };

                                newMol.AddBond(b);
                            }
                        }
                    }

                    newMol.Parent = tempModel;
                    tempModel.AddMolecule(newMol);
                }

                tempModel.RescaleForCml();
                string export = converter.Export(tempModel);
                IDataObject ido = new DataObject();
                ido.SetData(FormatCML, export);
                string header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                ido.SetData(DataFormats.Text, header + export);
                Clipboard.SetDataObject(ido, true);
            }
        }

        public void CutSelection()
        {
            UndoManager.BeginUndoBlock();
            CopySelection();
            DeleteSelection();
            UndoManager.EndUndoBlock();
        }

        private void RemoveAtomBondAdorners(Molecule atomParent)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
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

            foreach (Atom atom in atomParent.Atoms.Values)
            {
                if (SelectionAdorners.ContainsKey(atom))
                {
                    var selectionAdorner = SelectionAdorners[atom];
                    selectionAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                    layer.Remove(selectionAdorner);
                    SelectionAdorners.Remove(atom);
                }
            }

            foreach (var mol in atomParent.Molecules.Values)
            {
                RemoveAtomBondAdorners(mol);
            }
        }

        /// <summary>
        /// The pivotal routine for handling selection in the EditViewModel
        /// All display for selections *must* go through this routine.  No ifs, no buts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedItems_Changed(object sender, NotifyCollectionChangedEventArgs e)
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

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RemoveAllAdorners();
            }

            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedBondOptionId));
            OnPropertyChanged(nameof(SelectionType));
            //tell the editor what commands are allowable
            UpdateCommandStatuses();
        }

        /// <summary>
        /// Updates the command status.  Should generally only be called
        /// after the active selection is changed
        /// </summary>
        private void UpdateCommandStatuses()
        {
            CopyCommand.RaiseCanExecChanged();
            GroupCommand.RaiseCanExecChanged();
            UnGroupCommand.RaiseCanExecChanged();
            CutCommand.RaiseCanExecChanged();
            FlipHorizontalCommand.RaiseCanExecChanged();
            FlipVerticalCommand.RaiseCanExecChanged();
            AddHydrogensCommand.RaiseCanExecChanged();
            RemoveHydrogensCommand.RaiseCanExecChanged();
        }

        public void RemoveAllAdorners()
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            if (layer != null)
            {
                var adornerList = layer.GetAdorners(CurrentEditor);
                if (adornerList != null)
                {
                    foreach (Adorner adorner in adornerList)
                    {
                        layer.Remove(adorner);
                    }
                }
            }
            SelectionAdorners.Clear();
        }

        public void UpdateAtomBondAdorners()
        {
            if (MultiAdorner != null)
            {
                MultiAdorner.MouseLeftButtonDown -= SelAdorner_MouseLeftButtonDown;
                var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                layer.Remove(MultiAdorner);
                MultiAdorner = null;
            }

            var selAtomBonds = (from ChemistryBase sel in _selectedItems
                                where sel is Atom || sel is Bond
                                select sel).ToList();

            if (selAtomBonds.Any())
            {
                MultiAdorner = new MultiAtomBondAdorner(CurrentEditor, selAtomBonds);
                MultiAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
            }
        }

        private void RemoveSelectionAdorners(IList oldObjects)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
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

        /// <summary>
        /// Adds adorners for a list of objects.  Should only be called from
        /// events on the _selectedItems collection AFTER the collection
        /// has been updated
        /// </summary>
        /// <param name="newObjects"></param>
        private void AddSelectionAdorners(IList newObjects)
        {
            var singleAtomMols = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                                  where m.Atoms.Count == 1
                                  select m).ToList();
            var groupMols = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                             where m.IsGrouped
                             select m).ToList();
            var allMolecules = (from m in newObjects.OfType<Molecule>().Union(SelectedItems.OfType<Molecule>())
                                select m).ToList();

            var allSingletons = singleAtomMols.Count() == allMolecules.Count() && singleAtomMols.Any();
            var allGroups = allMolecules.Count() == groupMols.Count() & groupMols.Any();

            if (allSingletons)
            {
                RemoveAllAdorners();
                SingleAtomSelectionAdorner atomAdorner =
                    new SingleAtomSelectionAdorner(CurrentEditor, singleAtomMols);
                foreach (Molecule mol in singleAtomMols)
                {
                    SelectionAdorners[mol] = atomAdorner;
                }

                atomAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                atomAdorner.DragCompleted += AtomAdorner_DragCompleted;
            }
            else if (allGroups)
            {
                RemoveAllAdorners();
                var groupAdorner = new GroupSelectionAdorner(CurrentEditor,
                                                       groupMols);
                foreach (Molecule mol in groupMols)
                {
                    SelectionAdorners[mol] = groupAdorner;
                }
                groupAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                groupAdorner.DragCompleted += AtomAdorner_DragCompleted;
            }
            else if (allMolecules.Any())
            {
                RemoveAllAdorners();
                var molAdorner = new MoleculeSelectionAdorner(CurrentEditor,
                                                             allMolecules);
                foreach (Molecule mol in allMolecules)
                {
                    SelectionAdorners[mol] = molAdorner;
                }
                molAdorner.MouseLeftButtonDown += SelAdorner_MouseLeftButtonDown;
                molAdorner.DragCompleted += AtomAdorner_DragCompleted;
            }
        }

        #endregion Methods

        #region Event Handlers

        private void MolAdorner_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            var moleculeSelectionAdorner = ((MoleculeSelectionAdorner)sender);

            foreach (Molecule mol in moleculeSelectionAdorner.AdornedMolecules)
            {
                RemoveFromSelection(mol);
                //and add in a new one
                AddToSelection(mol);
            }
        }

        private void AtomAdorner_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            var moleculeSelectionAdorner = ((SingleAtomSelectionAdorner)sender);
            foreach (Molecule mol in moleculeSelectionAdorner.AdornedMolecules)
            {
                RemoveFromSelection(mol);
                //and add in a new one
                AddToSelection(mol);
            }
        }

        private void MolAdorner_ResizeCompleted(object sender, DragCompletedEventArgs e)
        {
            //we've completed the drag operation
            //remove the existing molecule adorner
            var moleculeSelectionAdorner = ((SingleAtomSelectionAdorner)sender);
            foreach (Molecule mol in moleculeSelectionAdorner.AdornedMolecules)
            {
                RemoveFromSelection(mol);
                //and add in a new one
                AddToSelection(mol);
            }
        }

        private void SelAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                _selectedItems.Clear();
                Molecule mol = null;
                var visual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));
                if (visual is AtomVisual av)
                {
                    mol = av.ParentAtom.Parent;
                }
                else if (visual is BondVisual bv)
                {
                    mol = bv.ParentBond.Parent;
                }

                RemoveAtomBondAdorners(mol);
                if (mol != null)
                {
                    AddToSelection(mol);
                }
            }
        }

        #endregion Event Handlers

        public bool IsDirty => UndoManager.CanUndo;

        /// <summary>
        /// Draws a ring as specfied by the new atom placements
        /// </summary>
        /// <param name="newAtomPlacements"></param>
        /// <param name="unsaturated"></param>
        public void DrawRing(List<NewAtomPlacement> newAtomPlacements, bool unsaturated, int startAt = 0)
        {
            var pt = new PeriodicTable();
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

                    insertAtom = AddAtomChain(previousAtom, currentPlacement.Position, ClockDirections.Nothing, pt.C,
                                              OrderSingle, BondStereo.None);
                    if (insertAtom == null)
                    {
                        Debugger.Break();
                    }

                    currentPlacement.ExistingAtom = insertAtom;
                }
                else if (previousAtom != null && previousAtom.BondBetween(currentAtom) == null)
                {
                    AddNewBond(previousAtom, currentAtom, previousAtom.Parent, OrderSingle, BondStereo.None);
                }
            }

            //join up the ring if there is no last bond
            Atom firstAtom = newAtomPlacements[0].ExistingAtom;
            Atom nextAtom = newAtomPlacements[1].ExistingAtom;
            if (firstAtom.BondBetween(nextAtom) == null)
            {
                AddNewBond(firstAtom, nextAtom, firstAtom.Parent, OrderSingle, BondStereo.None);
            }
            //set the alternating single and double bonds if unsaturated
            if (unsaturated)
            {
                MakeRingUnsaturated(newAtomPlacements);
            }

            //firstAtom.ParentMolecule.UpdateVisual();
            firstAtom.Parent.RebuildRings();
            Action undo = () =>
                          {
                              firstAtom.Parent.Refresh();
                              firstAtom.Parent.UpdateVisual();
                              _selectedItems.Clear();
                          };
            Action redo = () =>
                          {
                              firstAtom.Parent.Refresh();
                              firstAtom.Parent.UpdateVisual();
                          };

            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();

            //just refresh the atoms to be on the safe side
            foreach (var atomPlacement in newAtomPlacements)
            {
                atomPlacement.ExistingAtom.UpdateVisual();
            }

            //local function
            void MakeRingUnsaturated(List<NewAtomPlacement> list)
            {
                for (int i = startAt; i < list.Count + startAt; i++)
                {
                    var firstIndex = i % list.Count;
                    var secondIndex = (i + 1) % list.Count;

                    Atom thisAtom = list[firstIndex].ExistingAtom;
                    Atom otherAtom = list[secondIndex].ExistingAtom;

                    if (!thisAtom.IsUnsaturated & thisAtom.AvailableValences > 0 & !otherAtom.IsUnsaturated &
                        otherAtom.AvailableValences > 0)
                    {
                        Bond bondBetween = thisAtom.BondBetween(otherAtom);
                        SetBondAttributes(bondBetween,
                                          OrderDouble, Globals.BondStereo.None);
                        bondBetween.ExplicitPlacement = null;
                        bondBetween.UpdateVisual();
                        thisAtom.UpdateVisual();
                    }
                }
            }
        }

        public void DrawChain(List<Point> placements, Atom startAtom = null)
        {
            UndoManager.BeginUndoBlock();
            Atom lastAtom = startAtom;
            if (startAtom == null) //we're drawing an isolated chain
            {
                lastAtom = AddAtomChain(null, placements[0], ClockDirections.Nothing, bondOrder: OrderSingle,
                                        stereo: BondStereo.None);
            }

            foreach (Point placement in placements.Skip(1))
            {
                lastAtom = AddAtomChain(lastAtom, placement, ClockDirections.Nothing, bondOrder: OrderSingle,
                                        stereo: BondStereo.None);
                if (placement != placements.Last())
                {
                    lastAtom.ShowSymbol = null;
                }
            }

            if (startAtom != null)
            {
                startAtom.UpdateVisual();
                foreach (var bond in startAtom.Bonds)
                {
                    bond.UpdateVisual();
                }
            }

            UndoManager.EndUndoBlock();
        }

        public void DeleteMolecules(IEnumerable<Molecule> mols)
        {
            UndoManager.BeginUndoBlock();

            foreach (Molecule mol in mols)
            {
                DeleteMolecule(mol);
            }

            UndoManager.EndUndoBlock();
        }

        public void DeleteMolecule(Molecule mol)
        {
            UndoManager.BeginUndoBlock();

            var atomList = mol.Atoms.ToList();
            var bondList = mol.Bonds.ToList();

            Action redo = () =>
                          {
                              RemoveFromSelection(mol);
                              Model.RemoveMolecule(mol);
                              mol.Parent = null;
                          };

            Action undo = () =>
                          {
                              mol.Parent = Model;
                              Model.AddMolecule(mol);
                              AddToSelection(mol);
                          };

            RemoveFromSelection(mol);
            Model.RemoveMolecule(mol);
            mol.Parent = null;

            UndoManager.RecordAction(undo, redo);
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
                    foreach (var atom in mol.Atoms.Values)
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
                foreach (var atom in Model.GetAllAtoms())
                {
                    if (atom.ImplicitHydrogenCount > 0)
                    {
                        targetAtoms.Add(atom);
                    }
                }
            }

            if (targetAtoms.Any())
            {
                List<Atom> newAtoms = new List<Atom>();
                List<Bond> newBonds = new List<Bond>();
                Dictionary<string, Molecule> parents = new Dictionary<string, Molecule>();
                foreach (var atom in targetAtoms)
                {
                    double seperation = 90.0;
                    if (atom.Bonds.Count() > 1)
                    {
                        seperation = 30.0;
                    }

                    int hydrogenCount = atom.ImplicitHydrogenCount;
                    var vector = atom.BalancingVector();

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
                            Position = atom.Position +
                                                vector * (Model.XamlBondLength * ExplicitHydrogenBondPercentage)
                        };
                        newAtoms.Add(aa);
                        if (!parents.ContainsKey(aa.InternalId))
                        {
                            parents.Add(aa.InternalId, atom.Parent);
                        }

                        var bb = new Bond
                        {
                            StartAtomInternalId = atom.InternalId,
                            EndAtomInternalId = aa.InternalId,
                            Stereo = BondStereo.None,
                            Order = "S"
                        };
                        newBonds.Add(bb);
                        if (!parents.ContainsKey(bb.InternalId))
                        {
                            parents.Add(bb.InternalId, atom.Parent);
                        }
                    }
                }

                UndoManager.BeginUndoBlock();

                Action undoAction = () =>
                                    {
                                        foreach (var bond in newBonds)
                                        {
                                            bond.Parent.RemoveBond(bond);
                                        }

                                        foreach (var atom in newAtoms)
                                        {
                                            atom.Parent.RemoveAtom(atom);
                                        }

                                        if (mols.Any())
                                        {
                                            RefreshMolecules(mols);
                                        }
                                        else
                                        {
                                            RefreshMolecules(Model.Molecules.Values.ToList());
                                        }

                                        _selectedItems.Clear();
                                    };

                Action redoAction = () =>
                                    {
                                        Model.InhibitEvents = true;

                                        foreach (var atom in newAtoms)
                                        {
                                            parents[atom.InternalId].AddAtom(atom);
                                            atom.Parent = parents[atom.InternalId];
                                        }

                                        foreach (var bond in newBonds)
                                        {
                                            parents[bond.InternalId].AddBond(bond);
                                            bond.Parent = parents[bond.InternalId];
                                        }

                                        Model.InhibitEvents = false;

                                        if (mols.Any())
                                        {
                                            RefreshMolecules(mols);
                                        }
                                        else
                                        {
                                            RefreshMolecules(Model.Molecules.Values.ToList());
                                        }

                                        _selectedItems.Clear();
                                    };

                UndoManager.RecordAction(undoAction, redoAction);

                redoAction();

                UndoManager.EndUndoBlock();
            }
        }

        private void RefreshMolecules(List<Molecule> mols)
        {
            foreach (var mol in mols)
            {
                mol.UpdateVisual();
            }
        }

        public void RemoveHydrogens()
        {
            HydrogenTargets targets;
            var molecules = SelectedItems.OfType<Molecule>().ToList();
            if (molecules.Any())
            {
                targets = Model.GetHydrogenTargets(molecules);
            }
            else
            {
                targets = Model.GetHydrogenTargets();
            }

            if (targets.Atoms.Any())
            {
                UndoManager.BeginUndoBlock();
                Action undoAction = () =>
                                    {
                                        Model.InhibitEvents = true;

                                        foreach (var atom in targets.Atoms)
                                        {
                                            targets.Molecules[atom.InternalId].AddAtom(atom);
                                            atom.Parent = targets.Molecules[atom.InternalId];
                                        }

                                        foreach (var bond in targets.Bonds)
                                        {
                                            targets.Molecules[bond.InternalId].AddBond(bond);
                                            bond.Parent = targets.Molecules[bond.InternalId];
                                        }

                                        Model.InhibitEvents = false;

                                        if (molecules.Any())
                                        {
                                            RefreshMolecules(molecules);
                                        }
                                        else
                                        {
                                            RefreshMolecules(Model.Molecules.Values.ToList());
                                        }

                                        _selectedItems.Clear();
                                    };

                Action redoAction = () =>
                                    {
                                        foreach (var bond in targets.Bonds)
                                        {
                                            bond.Parent.RemoveBond(bond);
                                        }

                                        foreach (var atom in targets.Atoms)
                                        {
                                            atom.Parent.RemoveAtom(atom);
                                        }

                                        if (molecules.Any())
                                        {
                                            RefreshMolecules(molecules);
                                        }
                                        else
                                        {
                                            RefreshMolecules(Model.Molecules.Values.ToList());
                                        }

                                        _selectedItems.Clear();
                                    };

                UndoManager.RecordAction(undoAction, redoAction);
                redoAction();

                UndoManager.EndUndoBlock();
            }
        }

        public bool SingleMolSelected
        {
            get { return SelectedItems.Count == 1 && SelectedItems[0] is Molecule; }
        }

        public void FlipMolecule(Molecule selMolecule, bool flipVertically, bool flipStereo)
        {
            int scaleX = 1;
            int scaleY = 1;

            if (flipVertically)
            {
                scaleY = -1;
            }
            else
            {
                scaleX = -1;
            }

            var bb = selMolecule.BoundingBox;

            double cx = bb.Left + (bb.Right - bb.Left) / 2;
            double cy = bb.Top + (bb.Bottom - bb.Top) / 2;

            ScaleTransform flipTransform = new ScaleTransform(scaleX, scaleY, cx, cy);

            UndoManager.BeginUndoBlock();

            Action undo = () =>
                          {
                              selMolecule.Transform(flipTransform);

                              InvertPlacements(selMolecule);
                              selMolecule.UpdateVisual();
                          };

            Action redo = () =>
                          {
                              selMolecule.Transform(flipTransform);

                              InvertPlacements(selMolecule);
                              selMolecule.UpdateVisual();
                          };

            UndoManager.RecordAction(undo, redo, flipVertically ? "Flip Vertical" : "Flip Horizontal");
            redo();

            UndoManager.EndUndoBlock();

            //local function
            void InvertPlacements(Molecule m)
            {
                var ringBonds = from b in m.Bonds
                                where b.Rings.Any()
                                      && b.OrderValue <= 2.5 & b.OrderValue >= 1.5
                                select b;
                foreach (Bond ringBond in ringBonds)
                {
                    if (ringBond.ExplicitPlacement != null)
                    {
                        ringBond.ExplicitPlacement = (BondDirection)(-(int)ringBond.ExplicitPlacement);
                    }
                }
            }
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

        public void ClearSelection()
        {
            _selectedItems.Clear();
        }

        public void AddToSelection(List<object> thingsToAdd)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            DebugHelper.WriteLine($"Started at {DateTime.Now}");

            //take a snapshot of the current selection
            var currentSelection = SelectedItems.ToList();
            //add all the new items to the existing selection
            var allItems = currentSelection.Union(thingsToAdd).ToList();

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //phase one - group atoms into the molecules
            //grab all parent molecules for selected atoms
            var allParents = (from a in allItems.OfType<Atom>()
                              group a by a.Parent
                               into parent
                              select new
                              {
                                  Parent = parent.Key,
                                  Count = parent.Count()
                              }).ToList();

            //and grab all of those that have all atoms selected
            var fullParents = (from m in allParents
                               where m.Count == m.Parent.AtomCount
                               select m.Parent).ToList();

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //now add all the molecules that haven't been selected
            //first clear out the atoms
            foreach (var fm in fullParents)
            {
                foreach (var atom in fm.Atoms.Values)
                {
                    _selectedItems.Remove(atom);
                    thingsToAdd.Remove(atom);
                }

                foreach (Bond bond in fm.Bonds)
                {
                    _selectedItems.Remove(bond);
                    thingsToAdd.Remove(bond);
                }
                //and add in the selected parent
                if (!_selectedItems.Contains(fm.RootMolecule))
                {
                    _selectedItems.Add(fm.RootMolecule);
                }
            }

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            var newMols = thingsToAdd.OfType<Molecule>().ToList();
            foreach (var molecule in newMols)
            {
                if (!_selectedItems.Contains(molecule.RootMolecule))
                {
                    _selectedItems.Add(molecule);
                }
                thingsToAdd.Remove(molecule);
            }

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //now we need to process remaining individual atoms
            var newAtoms = thingsToAdd.OfType<Atom>().ToList();
            foreach (var newAtom in newAtoms)
            {
                if (!_selectedItems.Contains(newAtom))
                {
                    _selectedItems.Add(newAtom);
                    thingsToAdd.Remove(newAtom);
                    //add in the bonds between this atom and any other selected atoms
                    foreach (Bond bond in newAtom.Bonds)
                    {
                        if (_selectedItems.Contains(bond.OtherAtom(newAtom)))
                        {
                            _selectedItems.Add(bond);
                        }
                    }
                }
            }

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");

            //now add in any remaining bonds
            var newBonds = thingsToAdd.OfType<Bond>().ToList();

            foreach (Bond newBond in newBonds)
            {
                if (!(_selectedItems.Contains(newBond) | _selectedItems.Contains(newBond.Parent.RootMolecule)))
                {
                    _selectedItems.Add(newBond);
                }
            }

            UpdateAtomBondAdorners();

            DebugHelper.WriteLine($"Timing: {sw.ElapsedMilliseconds}ms");
            DebugHelper.WriteLine($"Finished at {DateTime.Now}");
        }

        public void RemoveFromSelection(List<object> thingsToAdd)
        {
            //grab all the molecules that contain selected objects
            var molsInSelection = (from dynamic selItem in SelectedItems
                                   where ((selItem is Atom) | (selItem is Bond))
                                   select selItem.Parent).Distinct();
            foreach (object o in thingsToAdd)
            {
                switch (o)
                {
                    case Atom atom:
                        {
                            if (atom.Singleton) //it's a single atom molecule
                            {
                                _selectedItems.Remove(atom.Parent);
                            }

                            if (_selectedItems.Contains(atom))
                            {
                                _selectedItems.Remove(atom);
                            }

                            break;
                        }

                    case Bond bond:
                        {
                            if (_selectedItems.Contains(bond))
                            {
                                _selectedItems.Remove(bond);
                            }

                            break;
                        }

                    case Molecule mol:
                        {
                            if (_selectedItems.Contains(mol))
                            {
                                _selectedItems.Remove(mol);
                            }

                            break;
                        }
                }
            }
            UpdateAtomBondAdorners();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void JoinMolecules(Atom a, Atom b, string currentOrder, BondStereo currentStereo)
        {
            Molecule molA = a.Parent;
            Molecule molB = b.Parent;
            Molecule newMol = null;
            var parent = molA.Parent;

            UndoManager.BeginUndoBlock();
            Action redo = () =>
                          {
                              Bond bond = new Bond(a, b);
                              bond.Order = currentOrder;
                              bond.Stereo = currentStereo;
                              newMol = Molecule.Join(molA, molB, bond);
                              newMol.Parent = parent;
                              parent.AddMolecule(newMol);
                              parent.RemoveMolecule(molA);
                              molA.Parent = null;
                              parent.RemoveMolecule(molB);
                              molB.Parent = null;
                              newMol.Model.Relabel(false);
                              newMol.UpdateVisual();
                          };
            redo();
            Action undo = () =>
                          {
                              molA.Parent = parent;
                              molA.Reparent();
                              parent.AddMolecule(molA);
                              molB.Parent = parent;
                              molB.Reparent();
                              parent.AddMolecule(molB);
                              parent.RemoveMolecule(newMol);
                              newMol.Parent = null;

                              molA.UpdateVisual();
                              molB.UpdateVisual();
                          };
            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
        }

        public void DeleteAtoms(IEnumerable<Atom> atoms)
        {
            var atomList = atoms.ToArray();
            //Add all the selected atoms to a set A
            if (atomList.Count() == 1 && atomList[0].Singleton)
            {
                var delAtom = atomList[0];
                UndoManager.BeginUndoBlock();
                var molecule = delAtom.Parent;
                Model model = molecule.Model;
                Action redo = () => { model.RemoveMolecule(molecule); };
                Action undo = () => { model.AddMolecule(molecule); };
                UndoManager.RecordAction(undo, redo);
                redo();
                UndoManager.EndUndoBlock();
            }
            else
            {
                DeleteAtomsAndBonds(atoms);
            }
        }

        /// <summary>
        /// Deletes a list of atoms and bonds, splitting them into separate molecules if required
        /// </summary>
        /// <param name="atomlist"></param>
        /// <param name="bondList"></param>
        ///
        public void DeleteAtomsAndBonds(IEnumerable<Atom> atomlist = null, IEnumerable<Bond> bondList = null)
        {
            void RefreshRingBonds(int theoreticalRings, Molecule molecule, Bond deleteBond)
            {
                if (theoreticalRings != molecule.TheoreticalRings)
                {
                    molecule.RebuildRings();
                    foreach (Ring bondRing in deleteBond.Rings)
                    {
                        foreach (Bond bond in bondRing.Bonds)
                        {
                            bond.UpdateVisual();
                        }
                    }
                }
            }

            HashSet<Atom> deleteAtoms = new HashSet<Atom>();
            HashSet<Bond> deleteBonds = new HashSet<Bond>();
            HashSet<Atom> neighbours = new HashSet<Atom>();

            if (atomlist != null)
            {
                //Add all the selected atoms to a set A
                foreach (Atom atom in atomlist)
                {
                    deleteAtoms.Add(atom);

                    foreach (Bond bond in atom.Bonds)
                    {
                        //Add all the selected atoms' bonds to B
                        deleteBonds.Add(bond);
                        //Add start and end atoms B1s and B1E to neighbours
                        neighbours.Add(bond.StartAtom);
                        neighbours.Add(bond.EndAtom);
                    }
                }
            }

            if (bondList != null)
            {
                foreach (var bond in bondList)
                {
                    //Add all the selected bonds to deleteBonds
                    deleteBonds.Add(bond);
                    //Add start and end atoms B1s and B1E to neighbours
                    neighbours.Add(bond.StartAtom);
                    neighbours.Add(bond.EndAtom);
                }
            }

            //ignore the atoms we are going to delete anyway
            neighbours.ExceptWith(deleteAtoms);
            HashSet<Atom> updateAtoms = new HashSet<Atom>(neighbours);

            List<HashSet<Atom>> atomGroups = new List<HashSet<Atom>>();
            Molecule mol = null;
            //take a copy of the neighbouring atoms
            HashSet<Atom> neighboursCopy = new HashSet<Atom>(neighbours);

            //now, take groups of connected atoms from the remaining graph ignoring the excluded bonds
            while (neighbours.Count > 0)
            {
                HashSet<Atom> atomGroup = new HashSet<Atom>();

                //TODO: sort out the grouping of atoms
                var firstAtom = neighbours.First();
                mol = (Molecule)firstAtom.Parent;
                mol.TraverseBFS(firstAtom, a1 => { atomGroup.Add(a1); }, a2 => !atomGroup.Contains(a2), deleteBonds);
                atomGroups.Add(atomGroup);
                //remove the list of atoms from the atom group
                neighbours.ExceptWith(atomGroup);
            }

            //now, check to see whether there is a single atomgroup.  If so, then we still have one molecule
            if (atomGroups.Count == 1)
            {
                MoleculePropertyBag mpb = new MoleculePropertyBag();
                mpb.Store(mol);

                UndoManager.BeginUndoBlock();
                Action redo = () =>
                              {
                                  _selectedItems.Clear();
                                  int theoreticalRings = mol.TheoreticalRings;
                                  foreach (Bond deleteBond in deleteBonds)
                                  {
                                      mol.RemoveBond(deleteBond);
                                      RefreshRingBonds(theoreticalRings, mol, deleteBond);

                                      deleteBond.StartAtom.UpdateVisual();
                                      deleteBond.EndAtom.UpdateVisual();

                                      foreach (Bond atomBond in deleteBond.StartAtom.Bonds)
                                      {
                                          atomBond.UpdateVisual();
                                      }

                                      foreach (Bond atomBond in deleteBond.EndAtom.Bonds)
                                      {
                                          atomBond.UpdateVisual();
                                      }
                                  }

                                  foreach (Atom deleteAtom in deleteAtoms)
                                  {
                                      mol.RemoveAtom(deleteAtom);
                                  }

                                  mol.ClearProperties();
                                  RefreshAtomVisuals(updateAtoms);
                              };
                Action undo = () =>
                              {
                                  _selectedItems.Clear();
                                  foreach (Atom restoreAtom in deleteAtoms)
                                  {
                                      mol.AddAtom(restoreAtom);
                                      restoreAtom.UpdateVisual();
                                      AddToSelection(restoreAtom);
                                  }

                                  foreach (Bond restoreBond in deleteBonds)
                                  {
                                      int theoreticalRings = mol.TheoreticalRings;
                                      mol.AddBond(restoreBond);
                                      RefreshRingBonds(theoreticalRings, mol, restoreBond);

                                      restoreBond.StartAtom.UpdateVisual();
                                      restoreBond.EndAtom.UpdateVisual();
                                      foreach (Bond atomBond in restoreBond.StartAtom.Bonds)
                                      {
                                          atomBond.UpdateVisual();
                                      }

                                      foreach (Bond atomBond in restoreBond.EndAtom.Bonds)
                                      {
                                          atomBond.UpdateVisual();
                                      }

                                      restoreBond.UpdateVisual();

                                      AddToSelection(restoreBond);
                                  }

                                  mpb.Restore(mol);
                              };
                UndoManager.RecordAction(undo, redo);
                redo();
                UndoManager.EndUndoBlock();
            }
            else //we have multiple fragments
            {
                List<Molecule> newMolList = new List<Molecule>();
                List<Molecule> oldmolList = new List<Molecule>();
                //add all the relevant atoms and bonds to a new molecule;
                //grab the model for future reference
                Model parentModel = null;
                foreach (HashSet<Atom> atomGroup in atomGroups)
                {
                    //assume that all atoms share the same parent model & molecule
                    var parent = atomGroup.First().Parent;
                    if (parentModel == null)
                    {
                        parentModel = parent.Model;
                    }

                    if (!oldmolList.Contains(parent))
                    {
                        oldmolList.Add(parent);
                    }

                    Molecule newMolecule = new Molecule();

                    foreach (Atom atom in atomGroup)
                    {
                        newMolecule.AddAtom(atom);
                        var bondsToAdd = from Bond bond in atom.Bonds
                                         where !newMolecule.Bonds.Contains(bond) && !deleteBonds.Contains(bond)
                                         select bond;
                        foreach (Bond bond in bondsToAdd)
                        {
                            newMolecule.AddBond(bond);
                        }
                    }

                    newMolecule.Parent = parentModel;
                    newMolecule.Reparent();
                    newMolList.Add(newMolecule);
                    newMolecule.RebuildRings();
                    //add the molecule to the model
                    parentModel.AddMolecule(newMolecule);
                }

                foreach (Molecule oldMolecule in oldmolList)
                {
                    parentModel.RemoveMolecule(oldMolecule);
                    oldMolecule.Parent = null;
                }

                //refresh the neighbouring atoms
                RefreshAtomVisuals(updateAtoms);
                UndoManager.BeginUndoBlock();
                Action undo = () =>
                              {
                                  _selectedItems.Clear();
                                  foreach (Molecule oldMol in oldmolList)
                                  {
                                      oldMol.Reparent();
                                      oldMol.Parent = parentModel;
                                      parentModel.AddMolecule(oldMol);
                                      oldMol.UpdateVisual();
                                  }

                                  foreach (Molecule newMol in newMolList)
                                  {
                                      parentModel.RemoveMolecule(newMol);
                                      newMol.Parent = null;
                                  }

                                  RefreshAtomVisuals(updateAtoms);
                              };

                Action redo = () =>
                              {
                                  _selectedItems.Clear();
                                  foreach (Molecule newmol in newMolList)
                                  {
                                      newmol.Reparent();
                                      newmol.Parent = parentModel;
                                      parentModel.AddMolecule(newmol);
                                      newmol.UpdateVisual();
                                  }

                                  foreach (Molecule oldMol in oldmolList)
                                  {
                                      parentModel.RemoveMolecule(oldMol);
                                      oldMol.Parent = null;
                                  }

                                  RefreshAtomVisuals(updateAtoms);
                              };
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
            }
        }

        private void RefreshAtomVisuals(HashSet<Atom> updateAtoms)
        {
            foreach (Atom updateAtom in updateAtoms)
            {
                updateAtom.UpdateVisual();
                foreach (Bond updateAtomBond in updateAtom.Bonds)
                {
                    updateAtomBond.UpdateVisual();
                }
            }
        }

        public void DeleteBonds(IEnumerable<Bond> bonds)
        {
            DeleteAtomsAndBonds(bondList: bonds);
        }

        public void UpdateAtom(Atom atom, AtomPropertiesModel model)
        {
            UndoManager.BeginUndoBlock();

            ElementBase elementBaseBefore = atom.Element;
            int? chargeBefore = atom.FormalCharge;
            int? isotopeBefore = atom.IsotopeNumber;
            bool? showSymbolBefore = atom.ShowSymbol;

            ElementBase elementBaseAfter = model.Element;
            int? chargeAfter = null;
            int? isotopeAfter = null;
            bool? showSymbolAfter = null;

            if (elementBaseAfter is Element)
            {
                chargeAfter = model.Charge;
                showSymbolAfter = model.ShowSymbol;
                if (!string.IsNullOrEmpty(model.Isotope))
                {
                    isotopeAfter = int.Parse(model.Isotope);
                }
            }

            Action redo = () =>
                          {
                              atom.Element = elementBaseAfter;
                              atom.FormalCharge = chargeAfter;
                              atom.IsotopeNumber = isotopeAfter;
                              atom.ShowSymbol = showSymbolAfter;
                              atom.Parent.UpdateVisual();
                          };

            redo();

            Action undo = () =>
                          {
                              atom.Element = elementBaseBefore;
                              atom.FormalCharge = chargeBefore;
                              atom.IsotopeNumber = isotopeBefore;
                              atom.ShowSymbol = showSymbolBefore;
                              atom.Parent.UpdateVisual();
                          };

            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
        }

        public void UpdateMolecule(Molecule molecule, Molecule changedMolecule)
        {
            UndoManager.BeginUndoBlock();

            bool? showBefore = molecule.ShowMoleculeBrackets;
            bool? showAfter = changedMolecule.ShowMoleculeBrackets;
            int? chargeBefore = molecule.FormalCharge;
            int? chargeAfter = changedMolecule.FormalCharge;
            int? countBefore = molecule.Count;
            int? countAfter = changedMolecule.Count;
            int? spinBefore = molecule.SpinMultiplicity;
            int? spinAfter = changedMolecule.SpinMultiplicity;

            var beforeNames = molecule.Names.ToList();
            var beforeFormulas = molecule.Formulas.ToList();
            var beforeLabels = molecule.Labels.ToList();
            var afterNames = changedMolecule.Names.ToList();
            var afterFormulas = changedMolecule.Formulas.ToList();
            var afterLabels = changedMolecule.Labels.ToList();

            var childMolecules = molecule.GetChildMolecules();
            var afterChildren = changedMolecule.GetChildMolecules();

            var beforeChildNames = new Dictionary<string, List<TextualProperty>>();
            foreach (var child in childMolecules)
            {
                beforeChildNames.Add(child.Path, child.Names.ToList());
            }

            var afterChildNames = new Dictionary<string, List<TextualProperty>>();
            foreach (var child in afterChildren)
            {
                afterChildNames.Add(child.Path, child.Names.ToList());
            }

            var beforeChildFormulas = new Dictionary<string, List<TextualProperty>>();
            foreach (var child in childMolecules)
            {
                beforeChildFormulas.Add(child.Path, child.Formulas.ToList());
            }

            var afterChildFormulas = new Dictionary<string, List<TextualProperty>>();
            foreach (var child in afterChildren)
            {
                afterChildFormulas.Add(child.Path, child.Formulas.ToList());
            }

            var beforeChildLabels = new Dictionary<string, List<TextualProperty>>();
            foreach (var child in childMolecules)
            {
                beforeChildLabels.Add(child.Path, child.Labels.ToList());
            }

            var afterChildLabels = new Dictionary<string, List<TextualProperty>>();
            foreach (var child in afterChildren)
            {
                afterChildLabels.Add(child.Path, child.Labels.ToList());
            }

            Action redo = () =>
            {
                molecule.ShowMoleculeBrackets = showAfter;
                molecule.FormalCharge = chargeAfter;
                molecule.Count = countAfter;
                molecule.SpinMultiplicity = spinAfter;

                UpdateLabels(molecule, afterNames, afterFormulas, afterLabels);
                foreach (var child in childMolecules)
                {
                    UpdateLabels(child, afterChildNames[child.Path], afterChildFormulas[child.Path], afterChildLabels[child.Path]);
                }
            };

            redo();

            Action undo = () =>
            {
                molecule.ShowMoleculeBrackets = showBefore;
                molecule.FormalCharge = chargeBefore;
                molecule.Count = countBefore;
                molecule.SpinMultiplicity = spinBefore;

                UpdateLabels(molecule, beforeNames, beforeFormulas, beforeLabels);
                foreach (var child in childMolecules)
                {
                    UpdateLabels(child, beforeChildNames[child.Path], beforeChildFormulas[child.Path], beforeChildLabels[child.Path]);
                }
            };

            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();

            // Local Function
            void UpdateLabels(Molecule mol, List<TextualProperty> names, List<TextualProperty> formulas, List<TextualProperty> labels)
            {
                mol.Names.Clear();
                foreach (var property in names)
                {
                    mol.Names.Add(property);
                }

                mol.Formulas.Clear();
                foreach (var property in formulas)
                {
                    mol.Formulas.Add(property);
                }

                mol.Labels.Clear();
                foreach (var property in labels)
                {
                    mol.Labels.Add(property);
                }
            }
        }

        public void UpdateBond(Bond bond, BondPropertiesModel model)
        {
            UndoManager.BeginUndoBlock();

            double bondOrderBefore = bond.OrderValue.Value;
            BondStereo stereoBefore = bond.Stereo;
            BondDirection? directionBefore = bond.ExplicitPlacement;

            double bondOrderAfter = model.BondOrderValue;
            BondStereo stereoAfter = BondStereo.None;
            BondDirection? directionAfter = null;

            var startAtom = bond.StartAtom;
            var endAtom = bond.EndAtom;

            bool swapAtoms = false;

            if (model.IsSingle)
            {
                switch (model.SingleBondChoice)
                {
                    case SingleBondType.None:
                        stereoAfter = BondStereo.None;
                        break;

                    case SingleBondType.Wedge:
                        stereoAfter = BondStereo.Wedge;
                        break;

                    case SingleBondType.BackWedge:
                        stereoAfter = BondStereo.Wedge;
                        swapAtoms = true;
                        break;

                    case SingleBondType.Hatch:
                        stereoAfter = BondStereo.Hatch;
                        break;

                    case SingleBondType.BackHatch:
                        stereoAfter = BondStereo.Hatch;
                        swapAtoms = true;
                        break;

                    case SingleBondType.Indeterminate:
                        stereoAfter = BondStereo.Indeterminate;
                        break;

                    default:
                        stereoAfter = BondStereo.None;
                        break;
                }
            }

            if (model.Is1Point5 | model.Is2Point5 | model.IsDouble)
            {
                if (model.DoubleBondChoice == DoubleBondType.Indeterminate)
                {
                    stereoAfter = BondStereo.Indeterminate;
                }
                else
                {
                    stereoAfter = BondStereo.None;
                    if (model.DoubleBondChoice != DoubleBondType.Auto)
                    {
                        directionAfter = (BondDirection)model.DoubleBondChoice;
                    }
                }
            }

            Action redo = () =>
                          {
                              bond.Order = OrderValueToOrder(bondOrderAfter);
                              bond.Stereo = stereoAfter;
                              bond.ExplicitPlacement = directionAfter;
                              bond.Parent.UpdateVisual();
                              if (swapAtoms)
                              {
                                  bond.EndAtomInternalId = startAtom.InternalId;
                                  bond.StartAtomInternalId = endAtom.InternalId;
                              }

                              bond.UpdateVisual();
                          };

            redo();

            Action undo = () =>
                          {
                              bond.Order = OrderValueToOrder(bondOrderBefore);
                              bond.Stereo = stereoBefore;
                              bond.ExplicitPlacement = directionBefore;
                              bond.Parent.UpdateVisual();
                              if (swapAtoms)
                              {
                                  bond.StartAtomInternalId = startAtom.InternalId;
                                  bond.EndAtomInternalId = endAtom.InternalId;
                              }

                              bond.UpdateVisual();
                          };

            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
        }

        public void PasteCML(string pastedCml)
        {
            CMLConverter cc = new CMLConverter();
            Model buffer = cc.Import(pastedCml);
            PasteModel(buffer);
        }

        public void PasteModel(Model buffer)
        {
            // Match to current model's settings
            buffer.Relabel(true);
            // above should be buffer.StripLabels(true);
            buffer.ScaleToAverageBondLength(Model.XamlBondLength);
            if (buffer.Molecules.Count > 1)
            {
                Packer packer = new Packer();
                packer.Model = buffer;
                packer.Pack(Model.XamlBondLength * 2);
            }

            var molList = buffer.Molecules.Values.ToList();
            var abb = buffer.BoundingBoxWithFontSize;
            //grab the metrics of the editor's viewport
            var editorControlHorizontalOffset = EditorControl.HorizontalOffset;
            var editorControlViewportWidth = EditorControl.ViewportWidth;
            var editorControlVerticalOffset = EditorControl.VerticalOffset;
            var editorControlViewportHeight = EditorControl.ViewportHeight;
            //to center on the X coordinate, we need to set the left extent of the model to the horizontal offset
            //plus half the viewport width, minus half the model width
            //Similar for the height
            double leftCenter = editorControlHorizontalOffset + editorControlViewportWidth / 2;
            double topCenter = editorControlVerticalOffset + editorControlViewportHeight / 2;
            //these two coordinates now give us the point where the new model should be centered
            buffer.CenterOn(new Point(leftCenter, topCenter));

            UndoManager.BeginUndoBlock();

            Action undo = () =>
                          {
                              foreach (var mol in molList)
                              {
                                  RemoveFromSelection(mol);
                                  Model.RemoveMolecule(mol);
                                  mol.Parent = null;
                              }
                          };
            Action redo = () =>
                          {
                              _selectedItems.Clear();
                              foreach (var mol in molList)
                              {
                                  mol.Parent = Model;
                                  Model.AddMolecule(mol);
                                  AddToSelection(mol);
                              }
                          };

            redo();
            UndoManager.RecordAction(undo, redo);

            UndoManager.EndUndoBlock();
        }

        public void DeleteSelection()
        {
            var atoms = SelectedItems.OfType<Atom>().ToList();
            var bonds = SelectedItems.OfType<Bond>().ToList();
            var mols = SelectedItems.OfType<Molecule>().ToList();

            if (mols.Any())
            {
                DeleteMolecules(mols);
            }
            else if (atoms.Any() | bonds.Any())
            {
                UndoManager.BeginUndoBlock();

                DeleteAtomsAndBonds(atoms, bonds);

                UndoManager.EndUndoBlock();
                _selectedItems.Clear();
            }
        }

        /// <summary>
        /// Ungroups selected molecules
        /// </summary>
        /// <param name="selection">Active selection within the editor</param>
        public void UnGroup(IEnumerable<object> selection)
        {
            List<Molecule> selGroups;
            //grab just the grouped molecules first
            selGroups = (from Molecule mol in selection.OfType<Molecule>()
                         where mol.IsGrouped
                         select mol).ToList();
            UnGroup(selGroups);
        }

        private void UnGroup(List<Molecule> selGroups)
        {
            //keep track of parent child relationships for later
            Dictionary<Molecule, List<Molecule>> parentsAndChildren = new Dictionary<Molecule, List<Molecule>>();

            foreach (Molecule selGroup in selGroups)
            {
                parentsAndChildren[selGroup] = new List<Molecule>();
                foreach (Molecule child in selGroup.Molecules.Values)
                {
                    parentsAndChildren[selGroup].Add(child);
                }
            }

            UndoManager.BeginUndoBlock();

            Action redo = () =>
                          {
                              //selected groups are always top level objects
                              foreach (Molecule parent in parentsAndChildren.Keys)
                              {
                                  RemoveFromSelection(parent);
                                  Model.RemoveMolecule(parent);
                                  foreach (var child in parentsAndChildren[parent])
                                  {
                                      child.Parent = Model;
                                      Model.AddMolecule(child);
                                      child.UpdateVisual();
                                  }
                              }

                              foreach (List<Molecule> molecules in parentsAndChildren.Values)
                              {
                                  foreach (Molecule child in molecules)
                                  {
                                      AddToSelection(child);
                                  }
                              }

                              ActiveMode.CurrentStatus = "Ungrouped.";
                          };
            Action undo = () =>
                          {
                              foreach (var oldParent in parentsAndChildren)
                              {
                                  Model.AddMolecule(oldParent.Key);
                                  foreach (Molecule child in oldParent.Value)
                                  {
                                      RemoveFromSelection(child);
                                      Model.RemoveMolecule(child);
                                      child.Parent = oldParent.Key;
                                      oldParent.Key.AddMolecule(child);
                                      child.UpdateVisual();
                                  }

                                  oldParent.Key.UpdateVisual();
                              }

                              foreach (Molecule parent in parentsAndChildren.Keys)
                              {
                                  AddToSelection(parent);
                              }

                              ActiveMode.CurrentStatus = "Grouped.";
                          };
            redo();
            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
        }

        /// <summary>
        ///  Creates a parent molecule and makes all the selected molecules children
        /// </summary>
        /// <param name="selection">Observable collection of ChemistryBase objects</param>
        public void Group(IEnumerable<object> selection)
        {
            List<Molecule> children;
            //grab just the grouped molecules first
            children = (from Molecule mol in selection.OfType<Molecule>()
                        select mol).ToList();
            Group(children);
        }

        /// <summary>
        /// Creates a parent molecule and makes all the selected molecules children
        /// </summary>
        /// <param name="children">List of child molecules</param>
        public void Group(List<Molecule> children)
        {
            UndoManager.BeginUndoBlock();

            Molecule parent = new Molecule();
            Action redo = () =>
                          {
                              _selectedItems.Clear();
                              parent.Parent = Model;
                              Model.AddMolecule(parent);
                              var kids = children.ToArray();
                              foreach (var molecule in kids)
                              {
                                  if (Model.Molecules.Values.Contains(molecule))
                                  {
                                      Model.RemoveMolecule(molecule);
                                      molecule.Parent = parent;
                                      parent.AddMolecule(molecule);
                                  }
                              }

                              parent.UpdateVisual();
                              AddToSelection(parent);
                          };
            Action undo = () =>
                          {
                              _selectedItems.Clear();

                              Model.RemoveMolecule(parent);
                              parent.Parent = null;
                              var kids = parent.Molecules.Values.ToArray();
                              foreach (var child in kids)
                              {
                                  if (parent.Molecules.Values.Contains(child))
                                  {
                                      parent.RemoveMolecule(child);

                                      child.Parent = Model;
                                      Model.AddMolecule(child);
                                      child.UpdateVisual();
                                      AddToSelection(child);
                                  }
                              }
                          };
            redo();
            UndoManager.RecordAction(undo, redo);
            UndoManager.EndUndoBlock();
        }

        /// <summary>
        /// Selects all visible molecules
        /// </summary>
        public void SelectAll()
        {
            ClearSelection();
            foreach (var mol in Model.Molecules.Values)
            {
                AddToSelection(mol);
            }
            
        }
    }
}