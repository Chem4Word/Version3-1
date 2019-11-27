// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;

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

        /// <summary>
        /// True is this model has functional groups
        /// </summary>
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

        /// <summary>
        /// True if this model has nested molecules
        /// </summary>
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

        public List<TextualProperty> AllTextualProperties
        {
            get
            {
                var list = new List<TextualProperty>();

                // Add 2D if relevant
                if (TotalAtomsCount > 0)
                {
                    list.Add(new TextualProperty
                    {
                        Id = "2D",
                        TypeCode = "2D",
                        FullType = "2D",
                        Value = "2D"
                    });
                    list.Add(new TextualProperty
                    {
                        Id = "c0",
                        TypeCode = "F",
                        FullType = "ConciseFormula",
                        Value = ConciseFormula
                    });
                    list.Add(new TextualProperty
                    {
                        Id = "S",
                        TypeCode = "S",
                        FullType = "Separator",
                        Value = "S"
                    });
                }

                foreach (var child in Molecules.Values)
                {
                    list.AddRange(child.AllTextualProperties);
                }

                if (list.Count > 0)
                {
                    list = list.Take(list.Count - 1).ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// Count of atoms in all molecules
        /// </summary>
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

        /// <summary>
        /// Lowest atomic number of any atom (if element) in all molecules
        /// </summary>
        public int MinAtomicNumber
        {
            get
            {
                // This number is used because it is higher than the Maximum value in the periodic table;
                int min = 255;

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

        /// <summary>
        /// Highest atomic number of any atom (if element) in all molecules
        /// </summary>
        public int MaxAtomicNumber
        {
            get
            {
                int max = 0;

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

        /// <summary>
        /// Count of bonds in all molecules
        /// </summary>
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

        /// <summary>
        /// Average bond length of all molecules
        /// </summary>
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

        /// <summary>
        /// Bond length used in Xaml
        /// </summary>
        public double XamlBondLength { get; set; }

        /// <summary>
        /// Overall Cml bounding box for all atoms
        /// </summary>
        public Rect BoundingBoxOfCmlPoints
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

        public double MinX => BoundingBoxWithFontSize.Left;
        public double MaxX => BoundingBoxWithFontSize.Right;
        public double MinY => BoundingBoxWithFontSize.Top;
        public double MaxY => BoundingBoxWithFontSize.Bottom;

        /// <summary>
        /// Overall bounding box for all atoms allowing for Font Size
        /// </summary>
        public Rect BoundingBoxWithFontSize
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

        /// <summary>
        /// Font size used for Xaml
        /// </summary>
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

        private readonly Dictionary<string, Molecule> _molecules;

        //wraps up the above Molecules collection
        public ReadOnlyDictionary<string, Molecule> Molecules;

        public string CustomXmlPartGuid { get; set; }

        public List<string> GeneralErrors { get; set; }

        /// <summary>
        /// List of all warnings encountered during the import from external file format
        /// </summary>
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

        /// <summary>
        /// List of all errors encountered during the import from external file format
        /// </summary>
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

        public void CalculateFormula()
        {
            // Phase #1 calculate and set each molecule's formula
            var calculateFormulas = CalculateAllFormulas();

            // Phase #2 - Collate the values
            string result = "";
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (var formula in calculateFormulas)
            {
                if (dictionary.ContainsKey(formula))
                {
                    dictionary[formula]++;
                }
                else
                {
                    dictionary.Add(formula, 1);
                }
            }

            foreach (KeyValuePair<string, int> kvp in dictionary)
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

            ConciseFormula = result;
        }

        private List<string> CalculateAllFormulas()
        {
            List<string> result = new List<string>();
            SetFormulas(Molecules.Values.ToList(), result);
            return result;

            // Local function to support recursion
            void SetFormulas(List<Molecule> molecules, List<string> data)
            {
                foreach (var molecule in molecules)
                {
                    if (molecule.Atoms.Count > 0)
                    {
                        var calculatedFormula = molecule.CalculatedFormula();
                        if (!string.IsNullOrEmpty(calculatedFormula))
                        {
                            molecule.ConciseFormula = calculatedFormula;
                            data.Add(calculatedFormula);
                        }
                    }
                    else
                    {
                        SetFormulas(molecule.Molecules.Values.ToList(), data);
                    }
                }
            }
        }

        /// <summary>
        /// Concise formula for the model
        /// </summary>
        public string ConciseFormula { get; private set; }

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

        public void CenterOn(Point point)
        {
            Rect boundingBox = BoundingBoxWithFontSize;
            Point midPoint = new Point(BoundingBoxWithFontSize.Left + boundingBox.Width / 2, BoundingBoxWithFontSize.Top + BoundingBoxWithFontSize.Height / 2);
            Vector displacement = midPoint - point;
            RepositionAll(displacement.X, displacement.Y);
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

            return res;
        }

        public Molecule AddMolecule(Molecule newMol)
        {
            _molecules[newMol.InternalId] = newMol;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new List<Molecule> { newMol });
            UpdateMoleculeEventHandlers(e);
            OnMoleculesChanged(this, e);
            return newMol;
        }

        public void SetMissingIds()
        {
            foreach (Molecule m in Molecules.Values)
            {
                m.SetMissingIds();
            }
        }

        public void SetProtectedLabels(List<string> protectedLabels)
        {
            foreach (Molecule m in Molecules.Values)
            {
                m.SetProtectedLabels(protectedLabels);
            }
        }

        public void ReLabelGuids()
        {
            int bondCount = 0, atomCount = 0, molCount = 0;

            foreach (var molecule in GetAllMolecules())
            {
                var number = molecule.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    molCount = Math.Max(molCount, n);
                }
            }

            foreach (var atom in GetAllAtoms())
            {
                var number = atom.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    atomCount = Math.Max(atomCount, n);
                }
            }

            foreach (var bond in GetAllBonds())
            {
                var number = bond.Id.Substring(1);
                int n;
                if (int.TryParse(number, out n))
                {
                    bondCount = Math.Max(bondCount, n);
                }
            }

            foreach (Molecule m in Molecules.Values)
            {
                m.ReLabelGuids(ref molCount, ref atomCount, ref bondCount);
            }
        }

        public void Relabel(bool includeNames)
        {
            int bondCount = 0, atomCount = 0, molCount = 0;

            foreach (Molecule m in Molecules.Values)
            {
                m.ReLabel(ref molCount, ref atomCount, ref bondCount, includeNames);
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
            copy.CustomXmlPartGuid = CustomXmlPartGuid;

            return copy;
        }

        private void ClearMolecules()
        {
            _molecules.Clear();
        }

        public HydrogenTargets GetHydrogenTargets(List<Molecule> molecules = null)
        {
            var targets = new HydrogenTargets();

            if (molecules == null)
            {
                var allHydrogens = GetAllAtoms().Where(a => a.Element.Symbol.Equals("H")).ToList();
                ProcessHydrogens(allHydrogens);
            }
            else
            {
                foreach (var mol in molecules)
                {
                    var allHydrogens = mol.Atoms.Values.Where(a => a.Element.Symbol.Equals("H")).ToList();
                    ProcessHydrogens(allHydrogens);
                }
            }

            return targets;

            // Local function
            void ProcessHydrogens(List<Atom> hydrogens)
            {
                if (hydrogens.Any())
                {
                    foreach (var hydrogen in hydrogens)
                    {
                        // Terminal Atom?
                        if (hydrogen.Degree == 1)
                        {
                            // Not Stereo
                            if (hydrogen.Bonds.First().Stereo == Globals.BondStereo.None)
                            {
                                if (!targets.Molecules.ContainsKey(hydrogen.InternalId))
                                {
                                    targets.Molecules.Add(hydrogen.InternalId, hydrogen.Parent);
                                }
                                targets.Atoms.Add(hydrogen);
                                if (!targets.Molecules.ContainsKey(hydrogen.Bonds.First().InternalId))
                                {
                                    targets.Molecules.Add(hydrogen.Bonds.First().InternalId, hydrogen.Parent);
                                }
                                targets.Bonds.Add(hydrogen.Bonds.First());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Ensure that bond length is between 5 and 95, and force to default if required
        /// </summary>
        /// <param name="target">Target bond length (if force true)</param>
        /// <param name="force">true to force setting of bond length to target bond length</param>
        public string EnsureBondLength(double target, bool force)
        {
            string result = string.Empty;

            if (TotalBondsCount > 0 && MeanBondLength > 0)
            {
                if (Math.Abs(MeanBondLength - target) < 0.1)
                {
                    result = string.Empty;
                }
                else
                {
                    if (force)
                    {
                        result = $"Forced BondLength from {Stringify(MeanBondLength)} to {Stringify(target)}";
                        ScaleToAverageBondLength(target);
                    }
                    else
                    {
                        if (MeanBondLength < Constants.MinimumBondLength - Constants.BondLengthTolerance
                            || MeanBondLength > Constants.MaximumBondLength + Constants.BondLengthTolerance)
                        {
                            result = $"Adjusted BondLength from {Stringify(MeanBondLength)} to {Stringify(target)}";
                            ScaleToAverageBondLength(target);
                        }
                        else
                        {
                            result = $"BondLength of {Stringify(MeanBondLength)} is within tolerance";
                        }
                    }
                }
            }

            return result;

            // Local Function
            string Stringify(double value)
            {
                return value.ToString("#,##0.00");
            }
        }

        public void ScaleToAverageBondLength(double newLength, Point centre)
        {
            if (TotalBondsCount > 0 && MeanBondLength > 0)
            {
                ScaleToAverageBondLength(newLength);

                var bb = BoundingBoxWithFontSize;
                var c = new Point(bb.Left + bb.Width / 2, bb.Top + bb.Height / 2);
                RepositionAll(c.X - centre.X, c.Y - centre.Y);
                _boundingBox = Rect.Empty;
            }
        }

        public void ScaleToAverageBondLength(double newLength)
        {
            if (TotalBondsCount > 0 && MeanBondLength > 0)
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

        public TextualProperty GetTextPropertyById(string id)
        {
            TextualProperty tp = null;

            foreach (var molecule in GetAllMolecules())
            {
                if (id.StartsWith(molecule.Id))
                {
                    if (id.EndsWith("f0"))
                    {
                        tp = new TextualProperty
                        {
                            Id = $"{molecule.Id}.f0",
                            Value = molecule.ConciseFormula,
                            FullType = "ConciseFormula"
                        };
                        break;
                    }

                    tp = molecule.Formulas.SingleOrDefault(f => f.Id.Equals(id));
                    if (tp != null)
                    {
                        break;
                    }

                    tp = molecule.Names.SingleOrDefault(n => n.Id.Equals(id));
                    if (tp != null)
                    {
                        break;
                    }
                }
            }

            return tp;
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
                double newLength = Constants.StandardBondLength / Globals.ScaleFactorForXaml;

                if (TotalBondsCount > 0 && MeanBondLength > 0)
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
                double newLength = Constants.StandardBondLength * Globals.ScaleFactorForXaml;

                if (TotalBondsCount > 0 && MeanBondLength > 0)
                {
                    newLength = MeanBondLength * Globals.ScaleFactorForXaml;
                }

                ScaleToAverageBondLength(newLength);
                XamlBondLength = newLength;
                ScaledForXaml = true;

                var middle = BoundingBoxOfCmlPoints;

                if (forDisplay)
                {
                    // Move to (0,0)
                    RepositionAll(middle.Left, middle.Top);
                }

                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(BoundingBoxWithFontSize)));
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
            double desiredLeft = (size.Width - BoundingBoxWithFontSize.Width) / 2.0;
            double desiredTop = (size.Height - BoundingBoxWithFontSize.Height) / 2.0;
            double offsetLeft = BoundingBoxWithFontSize.Left - desiredLeft;
            double offsetTop = BoundingBoxWithFontSize.Top - desiredTop;

            RepositionAll(offsetLeft, offsetTop);
        }
    }

    #endregion Methods
}