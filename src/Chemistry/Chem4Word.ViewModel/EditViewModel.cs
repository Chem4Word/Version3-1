// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.ViewModel.Adorners;
using Chem4Word.ViewModel.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interactivity;

namespace Chem4Word.ViewModel
{
    public class EditViewModel : DisplayViewModel
    {
        public enum SelectionTypeCode
        {
            None = 0,
            Atom = 1,
            Bond = 2,
            Molecule = 4
        }

        #region Fields

        private Dictionary<object, Adorner> _selectionAdorners = new Dictionary<object, Adorner>();

        #endregion Fields

        #region Properties

        public ObservableCollection<object> SelectedItems { get; }

        public UndoManager UndoManager { get; }

        private BondOption _selectedBondOption;

        public BondOption SelectedBondOption
        {
            get
            {
                var bonds = SelectedBondType;
                if (bonds.Count == 1)
                {
                    return SelectedBondType[0];
                }
                return null;
            }
            set
            {
                _selectedBondOption = value;
                foreach (Bond bond in SelectedItems.OfType<Bond>())
                {
                    // Task 65
                    // ToDo: Implement conversion
                    bond.Order = value.Order.Substring(0,1);
                    //bond.Stereo = value.Stereo;
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

        public List<BondOption> SelectedBondType
        {
            get { return SelectedItems.OfType<BondOption>().Distinct().ToList(); }
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
            OnPropertyChanged(nameof(SelectedBondOption));
        }

        public void RemoveAllAdorners()
        {
            var layer = AdornerLayer.GetAdornerLayer(DrawingSurface);
            var adornerList = _selectionAdorners.Keys.ToList();
            foreach (object oldObject in adornerList)
            {
                layer.Remove(_selectionAdorners[oldObject]);
                _selectionAdorners.Remove(oldObject);
            }
        }

        private void RemoveSelectionAdorners(IList oldObjects)
        {
            var layer = AdornerLayer.GetAdornerLayer(DrawingSurface);
            foreach (object oldObject in oldObjects)
            {
                if (_selectionAdorners.ContainsKey(oldObject))
                {
                    layer.Remove(_selectionAdorners[oldObject]);
                    _selectionAdorners.Remove(oldObject);
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
                    _selectionAdorners[newObject] = atomAdorner;
                }

                if (newObject is Bond)
                {
                    BondSelectionAdorner bondAdorner = new BondSelectionAdorner(DrawingSurface, (newObject as Bond));
                    _selectionAdorners[newObject] = bondAdorner;
                }
            }
        }
    }
}