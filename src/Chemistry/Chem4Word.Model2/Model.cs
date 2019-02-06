// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Chem4Word.Model2
{
    public class Model : IChemistryContainer
    {
        #region Fields

        public event NotifyCollectionChangedEventHandler AtomsChanged;

        public event NotifyCollectionChangedEventHandler BondsChanged;

        public event NotifyCollectionChangedEventHandler MoleculesChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Fields

        #region Event handlers

        private void UpdateMoleculeEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var mol = ((Molecule)oldItem);
                    mol.AtomsChanged -= Atoms_CollectionChanged;
                    mol.BondsChanged -= Bonds_CollectionChanged;
                    mol.PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var mol = ((Molecule)newItem);
                    mol.AtomsChanged += Atoms_CollectionChanged;
                    mol.BondsChanged += Bonds_CollectionChanged;
                    mol.PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void OnMoleculesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var temp = MoleculesChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void ChemObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(sender, e);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var temp = PropertyChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void Bonds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnBondsChanged(sender, e);
        }

        private void OnBondsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var temp = BondsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void Atoms_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAtomsChanged(sender, e);
        }

        private void OnAtomsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var temp = AtomsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        #endregion Event handlers

        #region Properties

        public int TotalAtomsCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.AtomCount;
                }

                return count;
            }
        }

        public int TotalBondsCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.BondCount;
                }

                return count;
            }
        }

        public double AverageBondLength
        {
            get
            {
                List<double> lengths = new List<double>();

                foreach (var mol in Molecules.Values)
                {
                    lengths.AddRange(mol.BondLengths);
                }

                return lengths.Average();
            }
        }

        public Rect OverallBoundingBox
        {
            get
            {
                bool isNew = true;
                Rect boundingBox = new Rect();

                foreach (var mol in Molecules.Values)
                {
                    if (isNew)
                    {
                        boundingBox = mol.BoundingBox;
                        isNew = false;
                    }
                    else
                    {
                        boundingBox.Union(mol.BoundingBox);
                    }
                }

                return boundingBox;
            }
        }

        public string Path => "/";
        public IChemistryContainer Root => null;

        public ChemistryBase GetFromPath(string path)
        {
            try
            {
                //first part of the path has to be a molecule
                if (path.StartsWith("/"))
                {
                    path = path.Substring(1); //strip off the first separator
                }

                string molID = path.UpTo("/");

                if (!Molecules.ContainsKey(molID))
                {
                    throw new ArgumentException("First child is not a molecule");
                }

                string relativepath = Helpers.Utils.GetRelativePath(molID, path);
                if (relativepath != "")
                {
                    return Molecules[molID].GetFromPath(relativepath);
                }
                else
                {
                    return Molecules[molID];
                }
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Object {path} not found");
            }
        }

        private Dictionary<string, Molecule> _molecules { get; }

        //wraps up the above Molecules collection
        public ReadOnlyDictionary<string, Molecule> Molecules;

        public string CustomXmlPartGuid { get; set; }

        public List<string> GeneralErrors { get; set; }

        public List<string> AllWarnings
        {
            get
            {
                var list = new List<string>();
                foreach (var molecule in Molecules.Values)
                {
                    list.AddRange(molecule.Warnings);
                }

                return list;
            }
        }

        public List<string> AllErrors
        {
            get
            {
                var list = new List<string>();
                foreach (var molecule in Molecules.Values)
                {
                    list.AddRange(molecule.Errors);
                }
                return list;
            }
        }

        public string ConciseFormula { get; set; }

        #endregion Properties

        #region Constructors

        public Model()
        {
            _molecules = new Dictionary<string, Molecule>();
            Molecules = new ReadOnlyDictionary<string, Molecule>(_molecules);
            GeneralErrors = new List<string>();
        }

        #endregion Constructors

        public bool RemoveMolecule(Molecule mol)
        {
            var res = _molecules.Remove(mol.InternalId);
            if (res)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<Molecule> { mol });
                OnMoleculesChanged(this, e);
                UpdateMoleculeEventHandlers(e);
            }

            return res;
        }

        public Molecule AddMolecule(Molecule newMol)
        {
            _molecules[newMol.Id] = newMol;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Molecule> { newMol });
            OnMoleculesChanged(this, e);
            UpdateMoleculeEventHandlers(e);
            return newMol;
        }

        public void Relabel(bool b)
        {
            int iBondcount = 0, iAtomCount = 0, iMolcount = 0;

            foreach (Molecule m in Molecules.Values)
            {
                m.ReLabel(b, ref iMolcount, ref iAtomCount, ref iBondcount);
            }
        }

        public void Refresh()
        {
            foreach (var molecule in Molecules.Values)
            {
                molecule.Refresh();
            }
        }

        public void ScaleToAverageBondLength(double newLength)
        {
            var current = AverageBondLength;
            if (current > 0)
            {
                double scale = newLength / current;
                foreach (var molecule in Molecules.Values)
                {
                    molecule.ScaleBonds(scale);
                }
            }
        }
    }
}