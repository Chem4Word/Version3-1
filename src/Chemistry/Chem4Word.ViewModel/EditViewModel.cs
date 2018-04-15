// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using Chem4Word.Model;
using Chem4Word.Model.Enums;
using Chem4Word.ViewModel.Adorners;
using Chem4Word.ViewModel.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interactivity;

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
        private int? _selectedBondOptionId;

        #endregion Fields

        #region Properties


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

        public UndoManager UndoManager { get; }

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
                foreach (Atom selectedAtom in SelectedItems.OfType<Atom>())
                {
                    selectedAtom.Element = value;
                }
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
                foreach (Bond bond in SelectedItems.OfType<Bond>())
                {
                    bond.Order = _bondOptions[_selectedBondOptionId.Value].Order;
                    bond.Stereo = _bondOptions[_selectedBondOptionId.Value].Stereo.Value;
                }
            }
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
                _activeMode.Attach(DrawingSurface);
            }
        }

        #region Commands

        public DeleteCommand DeleteCommand { get; }
        public AddAtomCommand AddAtomCommand { get; }
        public UndoCommand UndoCommand { get; }
        public RedoCommand RedoCommand { get; }
        public CopyCommand CopyCommand { get;  }
        public CutCommand CutCommand { get; }

        #endregion Commands

        #region Constructors

        public EditViewModel(Model.Model model) : base(model)
        {
            RedoCommand = new RedoCommand(this);
            UndoCommand = new UndoCommand(this);

            SelectedItems = new ObservableCollection<object>();
            SelectedItems.CollectionChanged += SelectedItemsOnCollectionChanged;

            UndoManager = new UndoManager(this);

            DeleteCommand = new DeleteCommand(this);
            AddAtomCommand = new AddAtomCommand(this);
            CopyCommand = new CopyCommand(this);
            CutCommand = new CutCommand(this);

            PeriodicTable pt = new PeriodicTable();
            _selectedElement = pt.C;

            _selectedBondOptionId = 1;

            LoadBondOptions();
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
                    AtomSelectionAdorner atomAdorner = new AtomSelectionAdorner(DrawingSurface, (newObject as Atom));
                    SelectionAdorners[newObject] = atomAdorner;
                }

                if (newObject is Bond)
                {
                    BondSelectionAdorner bondAdorner = new BondSelectionAdorner(DrawingSurface, (newObject as Bond));
                    SelectionAdorners[newObject] = bondAdorner;
                }

                if (newObject is Molecule)
                {
                    MoleculeSelectionAdorner molAdorner = new MoleculeSelectionAdorner(DrawingSurface, (newObject as Molecule));
                    SelectionAdorners[newObject] = molAdorner;
                }
            }
        }

        public void CopySelection()
        {
            MessageBox.Show("Copy code goes here");
        }
    }
}