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
    public class Model : IChemistryContainer, INotifyPropertyChanged
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
            if (!InhibitEvents)
            {
                var temp = MoleculesChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        private void ChemObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(sender, e);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = PropertyChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        private void Bonds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnBondsChanged(sender, e);
        }

        private void OnBondsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = BondsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        private void Atoms_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAtomsChanged(sender, e);
        }

        private void OnAtomsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitEvents)
            {
                var temp = AtomsChanged;
                if (temp != null)
                {
                    temp.Invoke(sender, e);
                }
            }
        }

        #endregion Event handlers

        #region Properties

        public bool InhibitEvents { get; set; }

        public bool HasFunctionalGroups
        {
            get
            {
                bool result = false;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    if (atom.Element is FunctionalGroup)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

        public bool HasNestedMolecules
        {
            get
            {
                bool result = false;

                foreach (var child in Molecules.Values)
                {
                    if (child.Molecules.Count > 0)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }
        }

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

        public int MinAtomicNumber
        {
            get
            {
                int min = Int32.MaxValue;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    if (atom.Element is Element e)
                    {
                        min = Math.Min(min, e.AtomicNumber);
                    }
                }

                return min;
            }
        }

        public int MaxAtomicNumber
        {
            get
            {
                int max = Int32.MinValue;

                var allAtoms = GetAllAtoms();

                foreach (var atom in allAtoms)
                {
                    if (atom.Element is Element e)
                    {
                        max = Math.Max(max, e.AtomicNumber);
                    }
                }

                return max;
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

        public double MeanBondLength
        {
            get
            {
                double result = 0.0;
                List<double> lengths = new List<double>();

                foreach (var mol in Molecules.Values)
                {
                    lengths.AddRange(mol.BondLengths);
                }

                if (lengths.Any())
                {
                    result = lengths.Average();
                }
                else
                {
                    if (ScaledForXaml)
                    {
                        result = XamlBondLength;
                    }
                }

                return result;
            }
        }

        public double XamlBondLength { get; set; }

        public Rect OverallAtomBoundingBox
        {
            get
            {
                Rect boundingBox = Rect.Empty;

                foreach (var mol in Molecules.Values)
                {
                    boundingBox.Union(mol.BoundingBox);
                }

                return boundingBox;
            }
        }

        public string Path => "/";
        public IChemistryContainer Root => null;

        public bool ScaledForXaml { get; set; }

        private Rect _boundingBox = Rect.Empty;

        public double MinX => BoundingBox.Left;
        public double MaxX => BoundingBox.Right;
        public double MinY => BoundingBox.Top;
        public double MaxY => BoundingBox.Bottom;

        public Rect BoundingBox
        {
            get
            {
                if (_boundingBox == Rect.Empty)
                {
                    var allAtoms = GetAllAtoms();

                    Rect modelRect = Rect.Empty;

                    if (allAtoms.Count > 0)
                    {
                        modelRect = allAtoms[0].BoundingBox(FontSize);
                        for (int i = 1; i < allAtoms.Count; i++)
                        {
                            var atom = allAtoms[i];
                            modelRect.Union(atom.BoundingBox(FontSize));
                        }
                    }

                    _boundingBox = modelRect;
                }

                return _boundingBox;
            }
        }

        //used to calculate the bounds of the atom
        public double FontSize
        {
            get
            {
                var allBonds = GetAllBonds();
                double fontSize = Globals.DefaultFontSize * Globals.ScaleFactorForXaml;

                if (allBonds.Any())
                {
                    fontSize = XamlBondLength * Globals.FontSizePercentageBond;
                }

                return fontSize;
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

        public string ConciseFormula
        {
            get
            {
                string result = "";
                Dictionary<string, int> f = new Dictionary<string, int>();
                foreach (var mol in Molecules.Values)
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

        #endregion Properties

        #region Constructors

        public Model()
        {
            _molecules = new Dictionary<string, Molecule>();
            Molecules = new ReadOnlyDictionary<string, Molecule>(_molecules);
            GeneralErrors = new List<string>();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Drags all Atoms back to the origin by the specified offset
        /// </summary>
        /// <param name="x">X offset</param>
        /// <param name="y">Y offset</param>
        public void RepositionAll(double x, double y)
        {
            foreach (Molecule molecule in Molecules.Values)
            {
                molecule.RepositionAll(x, y);
            }
            _boundingBox = Rect.Empty;
        }

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
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Object {path} not found {ex.Message}");
            }
        }

        public bool RemoveMolecule(Molecule mol)
        {
            var res = _molecules.Remove(mol.InternalId);
            if (res)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        new List<Molecule> { mol });
                OnMoleculesChanged(this, e);
                UpdateMoleculeEventHandlers(e);
            }
            else
            {
                throw new ArgumentException();
            }

            return res;
        }

        public Molecule AddMolecule(Molecule newMol)
        {
            _molecules[newMol.InternalId] = newMol;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new List<Molecule> { newMol });
            OnMoleculesChanged(this, e);
            UpdateMoleculeEventHandlers(e);
            return newMol;
        }

        public void Relabel(bool includeNames)
        {
            int iBondcount = 0, iAtomCount = 0, iMolcount = 0;

            foreach (Molecule m in Molecules.Values)
            {
                m.ReLabel(includeNames, ref iMolcount, ref iAtomCount, ref iBondcount);
            }
        }

        public void Refresh()
        {
            foreach (var molecule in Molecules.Values)
            {
                molecule.Refresh();
            }
        }

        public Model Copy()
        {
            Model copy = new Model();
            foreach (var child in Molecules.Values)
            {
                Molecule m = child.Copy();
                copy.AddMolecule(m);
                m.Parent = copy;
            }

            copy.ScaledForXaml = ScaledForXaml;

            return copy;
        }

        private void ClearMolecules()
        {
            _molecules.Clear();
        }

        public void ScaleToAverageBondLength(double newLength, Point centre)
        {
            if (MeanBondLength > 0)
            {
                double scale = newLength / MeanBondLength;
                var allAtoms = GetAllAtoms();
                foreach (var atom in allAtoms)
                {
                    atom.Position = new Point(atom.Position.X * scale, atom.Position.Y * scale);
                }

                _boundingBox = Rect.Empty;
                var bb = BoundingBox;
                var c = new Point(bb.Left + bb.Width / 2, bb.Top + bb.Height / 2);
                RepositionAll(c.X - centre.X, c.Y - centre.Y);
                _boundingBox = Rect.Empty;
            }
        }

        public void ScaleToAverageBondLength(double newLength)
        {
            if (MeanBondLength > 0)
            {
                double scale = newLength / MeanBondLength;
                var allAtoms = GetAllAtoms();
                foreach (var atom in allAtoms)
                {
                    atom.Position = new Point(atom.Position.X * scale, atom.Position.Y * scale);
                }
                _boundingBox = Rect.Empty;
            }
        }

        public List<Atom> GetAllAtoms()
        {
            List<Atom> allAtoms = new List<Atom>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildAtomList(allAtoms);
            }

            return allAtoms;
        }

        public List<Bond> GetAllBonds()
        {
            List<Bond> allBonds = new List<Bond>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildBondList(allBonds);
            }

            return allBonds;
        }

        public List<Molecule> GetAllMolecules()
        {
            List<Molecule> allMolecules = new List<Molecule>();
            foreach (Molecule mol in Molecules.Values)
            {
                mol.BuildMolList(allMolecules);
            }

            return allMolecules;
        }

        public void RescaleForCml()
        {
            if (ScaledForXaml)
            {
                double newLength = Globals.SingleAtomPseudoBondLength / Globals.ScaleFactorForXaml;

                if (MeanBondLength > 0)
                {
                    newLength = MeanBondLength / Globals.ScaleFactorForXaml;
                }

                ScaleToAverageBondLength(newLength);

                ScaledForXaml = false;
            }
        }

        public void RescaleForXaml(bool forDisplay)
        {
            if (!ScaledForXaml)
            {
                double newLength = Globals.SingleAtomPseudoBondLength * Globals.ScaleFactorForXaml;

                if (MeanBondLength > 0)
                {
                    newLength = MeanBondLength * Globals.ScaleFactorForXaml;
                }

                //var before = OverallAtomBoundingBox;
                //Debug.WriteLine($"ABB1 = {before}");

                ScaleToAverageBondLength(newLength);
                XamlBondLength = newLength;
                ScaledForXaml = true;

                var middle = OverallAtomBoundingBox;
                //Debug.WriteLine($"ABB2 = {middle}");
                if (forDisplay)
                {
                    // Move to (0,0)
                    RepositionAll(middle.Left, middle.Top);
                }

                //var after = OverallAtomBoundingBox;
                //Debug.WriteLine($"ABB3 = {after}");

                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(BoundingBox)));
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlBondLength)));
            }
        }

        /// <summary>
        /// Checks to make sure the internals of the molecule haven't become busted up.
        /// This will throw an Exception if something is wrong. You should be ready to catch it...
        /// </summary>
        public void CheckIntegrity()
        {
            var mols = GetAllMolecules();
            foreach (Molecule mol in mols)
            {
                mol.CheckIntegrity();
            }
        }

        public void CentreInCanvas(Size size)
        {
            // Re-Centre scaled drawing on Canvas, does not need to be undone
            double desiredLeft = (size.Width - BoundingBox.Width) / 2.0;
            double desiredTop = (size.Height - BoundingBox.Height) / 2.0;
            double offsetLeft = BoundingBox.Left - desiredLeft;
            double offsetTop = BoundingBox.Top - desiredTop;

            RepositionAll(offsetLeft, offsetTop);
        }
    }

    #endregion Methods
}