// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model.Annotations;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Chem4Word.Model
{
    /// <summary>
    /// Abstract class from which Model and Molecule both derive.
    /// This allows changes in the atoms and bonds membership to bubble up
    /// the molecule hierarchy
    /// </summary>
    [Serializable]
    public abstract class ChemistryContainer : INotifyPropertyChanged
    {
        public ObservableCollection<Bond> AllBonds { get; protected set; }
        public ObservableCollection<Atom> AllAtoms { get; protected set; }

        public ObservableCollection<Molecule> Molecules { get; protected set; }

        public ChemistryContainer Parent { get; set; }

        protected ChemistryContainer()
        {
            ResetCollections();
        }

        protected virtual void ResetCollections()
        {
            AllAtoms = new ObservableCollection<Atom>();

            AllAtoms.CollectionChanged += AllAtoms_CollectionChanged;
            AllBonds = new ObservableCollection<Bond>();
            AllBonds.CollectionChanged += AllBonds_CollectionChanged;
            Molecules = new ObservableCollection<Molecule>();
            Molecules.CollectionChanged += Molecules_CollectionChanged;
        }

        private void Molecules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddNewAtomsAndBonds(e);
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveOldAtomsAndBond(e);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    AddNewAtomsAndBonds(e);
                    RemoveOldAtomsAndBond(e);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void RemoveOldAtomsAndBond(NotifyCollectionChangedEventArgs e)
        {
            foreach (Molecule child in e.OldItems)
            {
                child.Parent = null;

                foreach (Atom atom in child.AllAtoms.ToList())
                {
                    AllAtoms.Remove(atom);
                }

                foreach (Bond bond in child.AllBonds.ToList())
                {
                    AllBonds.Remove(bond);
                }
            }
        }

        private void AddNewAtomsAndBonds(NotifyCollectionChangedEventArgs e)
        {
            foreach (Molecule child in e.NewItems)
            {
                child.Parent = this;

                foreach (Atom atom in child.AllAtoms)
                {
                    if (!AllAtoms.Contains(atom))
                    {
                        AllAtoms.Add(atom);
                    }
                }

                foreach (Bond bond in child.AllBonds)
                {
                    if (!AllBonds.Contains(bond))
                    {
                        AllBonds.Add(bond);
                    }
                }
            }
        }

        private void AllBonds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Parent != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (Bond bond in e.NewItems)
                        {
                            this.Parent.AllBonds.Add(bond);
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (Bond bond in e.OldItems)
                        {
                            this.Parent.AllBonds.Remove(bond);
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        foreach (Bond bond in e.NewItems)
                        {
                            this.Parent.AllBonds.Add(bond);
                        }
                        foreach (Bond bond in e.OldItems)
                        {
                            this.Parent.AllBonds.Remove(bond);
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        break;
                }
            }
        }

        private void AllAtoms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Parent != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (Atom atom in e.NewItems)
                        {
                            this.Parent.AllAtoms.Add(atom);
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (Atom atom in e.OldItems)
                        {
                            this.Parent.AllAtoms.Remove(atom);
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        foreach (Atom atom in e.NewItems)
                        {
                            this.Parent.AllAtoms.Add(atom);
                        }
                        foreach (Atom atom in e.OldItems)
                        {
                            this.Parent.AllAtoms.Remove(atom);
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        break;
                }
            }
        }

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void CheckForMerge(Molecule mol2)
        {
        }
    }
}