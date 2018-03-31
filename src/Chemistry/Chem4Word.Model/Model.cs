﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Data;

namespace Chem4Word.Model
{
    /// <summary>
    /// Overall container for Atoms, Bonds and other objects.
    /// Please limit rendering-specific code in these classes.
    /// Sometimes it will be unavoidable, but the less, the better
    /// </summary>
    [Serializable]
    public class Model : ChemistryContainer, INotifyPropertyChanged
    {
        private const int Padding = 25;

        public string CustomXmlPartGuid { get; set; }

        public string ConciseFormula
        {
            get
            {
                string result = "";
                Dictionary<string, int> f = new Dictionary<string, int>();
                foreach (var mol in Molecules)
                {
                    if (string.IsNullOrEmpty(mol.ConciseFormula))
                    {
                        mol.ConciseFormula = mol.CalculatedFormula();
                    }

                    if (f.ContainsKey(mol.ConciseFormula))
                    {
                        f[mol.ConciseFormula]++;
                    }
                    else
                    {
                        f.Add(mol.ConciseFormula, 1);
                    }
                }

                foreach (KeyValuePair<string, int> kvp in f)
                {
                    if (kvp.Value == 1)
                    {
                        result += $"{kvp.Key} . ";
                    }
                    else
                    {
                        result += $"{kvp.Value} {kvp.Key} . ";
                    }
                }

                if (result.EndsWith(" . "))
                {
                    result = result.Substring(0, result.Length - 3);
                }

                return result;
            }
        }

        public List<string> AllWarnings
        {
            get
            {
                return Molecules.SelectMany(m => m.Warnings).ToList();
            }
        }

        public List<string> AllErrors
        {
            get
            {
                return Molecules.SelectMany(m => m.Errors).ToList();
            }
        }

        /// <summary>
        /// Rolls up all those objects exposed to the view model so they can be displayed
        /// This is only Atoms and Bonds for now
        /// </summary>
        public CompositeCollection AllObjects
        {
            get
            {
                CompositeCollection theLot = new CompositeCollection();
                CollectionContainer cc1 = new CollectionContainer();
                cc1.Collection = AllAtoms;

                CollectionContainer cc2 = new CollectionContainer();
                cc2.Collection = AllBonds;

                theLot.Add(cc2);
                //Atoms MUST be added after Bonds to ensure they get z-indexed properly.
                theLot.Add(cc1);
                return theLot;
            }
        }

        public List<Ring> Rings
        {
            get { return Molecules.SelectMany(m => m.Rings).ToList(); }
        }

        /// <summary>
        /// Adding molecules to  or removing from the model also adds the Atoms and Bonds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Molecules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (Molecule m in e.NewItems)
                    {
                        foreach (Atom atom in m.Atoms)
                        {
                            if (!AllAtoms.Contains(atom))
                            {
                                AllAtoms.Add(atom);
                            }
                        }

                        foreach (Bond bond in m.Bonds)
                        {
                            if (!AllBonds.Contains(bond))
                            {
                                AllBonds.Add(bond);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Molecule m in e.OldItems)
                    {
                        foreach (Atom atom in m.Atoms.ToList())
                        {
                            AllAtoms.Remove(atom);
                        }

                        foreach (Bond bond in m.Bonds.ToList())
                        {
                            AllBonds.Remove(bond);
                        }
                    }
                    break;
            }
            OnPropertyChanged("BoundingBox");
        }

        public void Relabel(bool includeNames)
        {
            int iBondcount = 0, iAtomCount = 0, iMolcount = 0;

            foreach (Molecule m in Molecules)
            {
                m.Id = $"m{++iMolcount}";
                foreach (Atom a in m.Atoms)
                {
                    a.Id = $"a{(++iAtomCount)}";
                }
                foreach (Bond b in m.Bonds)
                {
                    b.Id = $"b{++iBondcount}";
                }

                if (includeNames)
                {
                    int formulaCount = 0;
                    int nameCount = 0;
                    string prefix = $"{m.Id}.f";

                    foreach (Formula f in m.Formulas)
                    {
                        if (!string.IsNullOrEmpty(f.Id) && f.Id.StartsWith(prefix))
                        {
                            string temp = f.Id.Substring(prefix.Length);
                            int value = 0;
                            int.TryParse(temp, out value);
                            formulaCount = Math.Max(formulaCount, value);
                        }
                    }

                    formulaCount++;

                    foreach (Formula f in m.Formulas)
                    {
                        if (string.IsNullOrEmpty(f.Id) || !f.Id.StartsWith(prefix))
                        {
                            f.Id = $"{prefix}{formulaCount++}";
                        }
                    }

                    prefix = $"{m.Id}.n";

                    foreach (ChemicalName n in m.ChemicalNames)
                    {
                        if (!string.IsNullOrEmpty(n.Id) && n.Id.StartsWith(prefix))
                        {
                            string temp = n.Id.Substring(prefix.Length);
                            int value = 0;
                            int.TryParse(temp, out value);
                            nameCount = Math.Max(nameCount, value);
                        }
                    }

                    nameCount++;

                    foreach (ChemicalName n in m.ChemicalNames)
                    {
                        if (string.IsNullOrEmpty(n.Id) || !n.Id.StartsWith(prefix))
                        {
                            n.Id = $"{prefix}{nameCount++}";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Re-labels the model from scratch.  Molecules, Atoms and Bonds all get a new label.
        /// </summary>
        public void Relabel()
        {
            Relabel(false);
        }

        /// <summary>
        /// Regenerates molecule collections from the bottom up
        ///
        /// </summary>
        public void RebuildMolecules()
        {
            foreach (Atom atom in AllAtoms)
            {
                atom.Parent = null;
            }

            foreach (Bond bond in AllBonds)
            {
                bond.Parent = null;
            }

            Molecules.Clear();

            AddNewMols();
        }

        private void AddNewMols()
        {
            while (AllAtoms.Count(a => a.Parent == null) > 0)
            {
                Atom seed = AllAtoms.First(a => a.Parent == null);
                Molecule m = new Molecule(seed);
                Molecules.Add(m);
            }
            OnPropertyChanged("BoundingBox");
        }

        /// <summary>
        /// Refereshes molecules, leaving those already assigned intact
        /// </summary>
        public void RefreshMolecules()
        {
            foreach (Molecule molecule in Molecules.ToList())
            {
                if (molecule.Atoms.Count == 0)
                {
                    //it's empty, trash it
                    Molecules.Remove(molecule);
                }
                else
                {
                    molecule.Refresh();
                }
            }
            AddNewMols();
        }

        /// <summary>
        /// Deep clones the molecule
        /// all the way down to the atoms
        /// </summary>
        /// <returns></returns>
        public Model Clone1()
        {
            //v important:  the labels are used to match up
            //old and new objects
            this.Relabel();

            Model clone = (Model)this.MemberwiseClone();
            clone.ResetCollections();
            foreach (var mol in Molecules)
            {
                clone.Molecules.Add(mol.Clone());
            }
            return clone;
        }

        public Model Clone()
        {
            Model clone = new Model();

            clone.CustomXmlPartGuid = CustomXmlPartGuid;

            foreach (var molecule in Molecules)
            {
                Molecule m = molecule.Clone();
                m.ConciseFormula = m.CalculatedFormula();
                m.RebuildRings();
                clone.Molecules.Add(m);
            }
            clone.RefreshMolecules();

            return clone;
        }
        public Model Clone2()
        {
            BinaryFormatter deserializer = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            deserializer.Serialize(ms, this);
            ms.Seek(0, 0);
            var clone = (Model)deserializer.Deserialize(ms);
            clone.RefreshMolecules();
            foreach (Molecule toRefresh in Molecules)
            {
                toRefresh.RebuildRings();
            }
            Debug.Assert(clone.Molecules.Count == this.Molecules.Count);
            Debug.Assert(clone.Molecules.SelectMany(m => m.Bonds).ToList().Count == this.Molecules.SelectMany(m => m.Bonds).ToList().Count);
            Debug.Assert(clone.Molecules.SelectMany(m => m.Rings).ToList().Count == this.Molecules.SelectMany(m => m.Rings).ToList().Count);
            Debug.Assert(clone.Molecules.SelectMany(m => m.ChemicalNames).ToList().Count == this.Molecules.SelectMany(m => m.ChemicalNames).ToList().Count);
            Debug.Assert(clone.Molecules.SelectMany(m => m.Formulas).ToList().Count == this.Molecules.SelectMany(m => m.Formulas).ToList().Count);
            return clone;
        }

        #region Layout

        public double ActualWidth
        {
            get { return BoundingBox.Width; }
        }

        public double ActualHeight
        {
            get { return BoundingBox.Height; }
        }

        //used to calculate the bounds of the atom
        public double FontSize { get; set; }

        public Rect BoundingBox
        {
            get
            {
                var modelRect = AllAtoms[0].BoundingBox(FontSize);
                for (int i = 1; i < AllAtoms.Count; i++)
                {
                    var atom = AllAtoms[i];
                    modelRect.Union(atom.BoundingBox(FontSize));
                }
                return modelRect;
            }
        }

        public double MinX => BoundingBox.Left;
        public double MaxX => BoundingBox.Right;
        public double MinY => BoundingBox.Top;
        public double MaxY => BoundingBox.Bottom;

        public double MeanBondLength
        {
            get
            {
                double result = 0;
                if (AllBonds.Any())
                {
                    result = AllBonds.Average(b => b.BondVector.Length);
                }
                return result;
            }
        }

        /// <summary>
        /// Drags all Atoms back to the origin by the specified offset
        /// </summary>
        /// <param name="x"> X offset</param>
        /// <param name="y"> Y offset</param>
        public void RepositionAll(double x, double y)
        {
            foreach (Molecule molecule in Molecules)
            {
                molecule.RepositionAll(x, y);
            }
        }

        /// <summary>
        /// Rescale to new preferred length
        /// </summary>
        /// <param name="newLength"></param>
        public void ScaleToAverageBondLength(double newLength)
        {
            foreach (Molecule molecule in Molecules)
            {
                molecule.ScaleToAverageBondLength(newLength, this);
            }
        }

        /// <summary>
        /// Rescale to new preferred length, to be used in xaml code behind, not normal cs
        /// </summary>
        /// <param name="preferredLength"></param>
        public void RescaleForXaml(double preferredLength)
        {
            ScaleToAverageBondLength(preferredLength);
            RepositionAll(MinX, MinY);
            OnPropertyChanged("BoundingBox");
        }

        #endregion Layout

        #region Interface implementations

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Interface implementations

        //public static PeriodicTable PeriodicTable = new PeriodicTable();

        #region Diagnostics

        public void DumpModel(string comment)
        {
            Debug.WriteLine(comment);
            Debug.WriteLine($"Model.Molecules.Count: {Molecules.Count}");
            Debug.WriteLine($"Model.Atoms.Count: {AllAtoms.Count}");
            Debug.WriteLine($"Model.Bonds.Count: {AllBonds.Count}");
            Debug.WriteLine($"Model.Rings.Count: {Rings.Count}");

            foreach (var molecule in Molecules)
            {
                Debug.WriteLine($" Molecule.Id: {molecule.Id}");
                Debug.WriteLine($" Molecule.Atoms.Count: {molecule.Atoms.Count}");
                Debug.WriteLine($" Molecule.AllAtoms.Count: {molecule.AllAtoms.Count}");
                Debug.WriteLine($" Molecule.Bonds.Count: {molecule.Bonds.Count}");
                Debug.WriteLine($" Molecule.AllBonds.Count: {molecule.AllBonds.Count}");
                Debug.WriteLine($" Molecule.Rings.Count: {molecule.Rings.Count}");

                foreach (var atom in molecule.Atoms)
                {
                    Debug.WriteLine($"  Atom.Id: {atom.Id}");
                }
                foreach (var atom in molecule.AllAtoms)
                {
                    Debug.WriteLine($"  AllAtom.Id: {atom.Id}");
                }
                foreach (var bond in molecule.Bonds)
                {
                    Debug.WriteLine($"  Bond.Id: {bond.Id} Rings: {bond.Rings.Count}");
                }
                foreach (var bond in molecule.AllBonds)
                {
                    Debug.WriteLine($"  AllBond.Id: {bond.Id} Rings: {bond.Rings.Count}");
                }
            }
        }

        #endregion Diagnostics
    }
}