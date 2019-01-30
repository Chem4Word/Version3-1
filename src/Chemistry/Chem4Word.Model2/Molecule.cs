// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;

namespace Chem4Word.Model2
{
    public class Molecule : ChemistryBase, IChemistryContainer, INotifyPropertyChanged
    {
        #region Fields

        #region Collections

        public readonly List<Ring> Rings;
        public readonly ReadOnlyDictionary<string, Atom> Atoms; //keyed by ID
        private Dictionary<string, Atom> _atoms;
        public readonly ReadOnlyCollection<Bond> Bonds; //this is the edge list
        private List<Bond> _bonds;
        private Dictionary<string, Molecule> _molecules;
        public readonly ReadOnlyDictionary<string, Molecule> Molecules;
        public readonly ObservableCollection<Formula> Formulas;
        public readonly ObservableCollection<ChemicalName> Names;
        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }

        private List<Ring> _sortedRings = null;

        #endregion Collections

        #endregion Fields

        #region Properties

        private string _id;
        private string _internalId;

        public int AtomCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.AtomCount;
                }

                count += Atoms.Count;

                return count;
            }
        }

        public int BondCount
        {
            get
            {
                int count = 0;

                foreach (var molecule in Molecules.Values)
                {
                    count += molecule.BondCount;
                }

                count += Bonds.Count;

                return count;
            }
        }

        public List<double> BondLengths
        {
            get
            {
                List<double> lengths = new List<double>();

                foreach (var mol in Molecules.Values)
                {
                    lengths.AddRange(mol.BondLengths);
                }

                foreach (var bond in Bonds)
                {
                    lengths.Add(bond.BondVector.Length);
                }

                return lengths;
            }
        }

        public Rect BoundingBox
        {
            get
            {
                Rect boundingBox = Rect.Empty;

                if (Atoms != null && Atoms.Any())
                {
                    var xMax = Atoms.Values.Select(a => a.Position.X).Max();
                    var xMin = Atoms.Values.Select(a => a.Position.X).Min();

                    var yMax = Atoms.Values.Select(a => a.Position.Y).Max();
                    var yMin = Atoms.Values.Select(a => a.Position.Y).Min();

                    boundingBox = new Rect(new Point(xMin, yMin), new Point(xMax, yMax));
                }

                foreach (var mol in Molecules.Values)
                {
                    if (boundingBox.IsEmpty)
                    {
                        boundingBox = mol.BoundingBox;
                    }
                    else
                    {
                        boundingBox.Union(mol.BoundingBox);
                    }
                }

                return boundingBox;
            }
        }

        #region Structural Properties

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string InternalId => _internalId;

        public IChemistryContainer Parent { get; set; }

        /// <summary>
        /// Returns the root IChemistryContainer  in the tree
        ///
        /// </summary>
        public IChemistryContainer Root
        {
            get
            {
                if (Parent != null)
                {
                    if (Parent.Root != null)
                    {
                        return Parent.Root;
                    }
                    return this;
                }

                return this;
            }
        }

        /// <summary>
        /// Returns a unique path to the molecule
        /// if the molecule is part of a model
        /// this starts with "/"
        /// </summary>
        public string Path
        {
            get
            {
                if (Parent == null)
                {
                    return Id;
                }

                if (Parent is Model)
                {
                    return ((Model)Parent).Path + Id;
                }

                return ((Molecule)Parent).Path + "/" + Id;
            }
        }

        public Model Model => Root as Model;

        #endregion Structural Properties

        #region Chemical properties

        private int? _spinMultiplicity;

        public int? SpinMultiplicity
        {
            get
            {
                return _spinMultiplicity;
            }
            set
            {
                _spinMultiplicity = value;
                OnPropertyChanged();
            }
        }

        #endregion Chemical properties

        private int? _formalCharge;

        public int? FormalCharge
        {
            get
            {
                return _formalCharge;
            }
            set
            {
                _formalCharge = value;
                //Attributed call knows who we are, no need to pass "FormalCharge" as an argument
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Constructors

        public Molecule()
        {
            Id = Guid.NewGuid().ToString("D");
            _internalId = Id;
            Errors = new List<string>();
            Warnings = new List<string>();
            Formulas = new ObservableCollection<Formula>();
            Names = new ObservableCollection<ChemicalName>();
            _atoms = new Dictionary<string, Atom>();
            Atoms = new ReadOnlyDictionary<string, Atom>(_atoms);
            _bonds = new List<Bond>();
            Bonds = new ReadOnlyCollection<Bond>(_bonds);
            _molecules = new Dictionary<string, Molecule>();
            Molecules = new ReadOnlyDictionary<string, Molecule>(_molecules);
            Rings = new List<Ring>();
        }

        public Molecule(XElement cmlElement) : this()
        {
            string idValue = cmlElement.Attribute("id")?.Value;
            if (idValue != null)
            {
                Id = idValue;
            }

            Errors = new List<string>();
            Warnings = new List<string>();

            List<XElement> childMolecules = CML.GetMolecules(cmlElement);

            List<XElement> atomElements = CML.GetAtoms(cmlElement);
            List<XElement> bondElements = CML.GetBonds(cmlElement);
            List<XElement> nameElements = CML.GetNames(cmlElement);
            List<XElement> formulaElements = CML.GetFormulas(cmlElement);

            foreach (XElement childElement in childMolecules)
            {
                Molecule newMol = new Molecule(childElement);
                AddMolecule(newMol);
                newMol.Parent = this;
            }

            Dictionary<string, string> reverseAtomLookup = new Dictionary<string, string>();
            foreach (XElement atomElement in atomElements)
            {
                Atom newAtom = new Atom(atomElement);
                if (newAtom.Messages.Count > 0)
                {
                    Errors.AddRange(newAtom.Messages);
                }

                AddAtom(newAtom);
                reverseAtomLookup[newAtom.Id] = newAtom.InternalId;
                newAtom.Parent = this;
            }

            foreach (XElement bondElement in bondElements)
            {
                Bond newBond = new Bond(bondElement, reverseAtomLookup);

                if (newBond.Messages.Count > 0)
                {
                    Errors.AddRange(newBond.Messages);
                }

                AddBond(newBond);
                newBond.Parent = this;
            }

            foreach (XElement formulaElement in formulaElements)
            {
                // Only import Concise Once
                if (string.IsNullOrEmpty(ConciseFormula))
                {
                    ConciseFormula = formulaElement.Attribute("concise")?.Value;
                }

                Formula formula = new Formula(formulaElement);
                if (formula.IsValid)
                {
                    Formulas.Add(formula);
                }
            }

            foreach (XElement nameElement in nameElements)
            {
                Names.Add(new ChemicalName(nameElement));
            }

            RebuildRings();
        }

        public void AddBond(Bond newBond)
        {
            _bonds.Add(newBond);
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Bond> { newBond });
            OnBondsChanged(this, e);

            UpdateBondsPropertyHandlers(e);
        }

        public Bond AddBond(string newID)
        {
            var newBond = new Bond();
            newBond.Id = newID;
            AddBond(newBond);
            return newBond;
        }

        public bool RemoveBond(Bond toRemove)
        {
            bool result = _bonds.Remove(toRemove);
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<Bond> { toRemove });
            OnBondsChanged(this, e);
            UpdateBondsPropertyHandlers(e);
            return result;
        }

        public Atom AddAtom(Atom newAtom)
        {
            _atoms[newAtom.InternalId] = newAtom;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Atom> { newAtom });
            OnAtomsChanged(this, e);
            UpdateAtomPropertyHandlers(e);
            return newAtom;
        }

        public bool RemoveAtom(Atom toRemove)
        {
            bool bondsExist =
                Bonds.Any(b => b.StartAtomInternalId.Equals(toRemove.InternalId) | b.EndAtomInternalId.Equals(toRemove.InternalId));
            if (bondsExist)
            {
                throw new InvalidOperationException("Cannot remove an Atom without first removing the attached Bonds.");
            }

            bool result = _atoms.Remove(toRemove.InternalId);
            if (result)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<Atom> { toRemove });
                OnAtomsChanged(this, e);
                UpdateAtomPropertyHandlers(e);
            }

            return result;
        }

        private void UpdateAtomPropertyHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object oldItem in e.OldItems)
                {
                    ((Atom)oldItem).PropertyChanged -= ChemObject_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (object newItem in e.NewItems)
                {
                    ((Atom)newItem).PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        public bool RemoveMolecule(Molecule mol)
        {
            var res = _molecules.Remove(mol.InternalId);
            if (res)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<Molecule> { mol });
                OnMoleculesChanged(this, e);
                UpdateMoleculeHandlers(e);
            }

            return res;
        }

        public Molecule AddMolecule(Molecule newMol)
        {
            _molecules[newMol.InternalId] = newMol;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<Molecule> { newMol });
            OnMoleculesChanged(this, e);
            UpdateMoleculeHandlers(e);
            return newMol;
        }

        public string ConciseFormula { get; set; }

        #endregion Constructors

        #region Events

        public event NotifyCollectionChangedEventHandler AtomsChanged;

        public event NotifyCollectionChangedEventHandler BondsChanged;

        public event NotifyCollectionChangedEventHandler MoleculesChanged;

        #endregion Events

        #region Event handlers

        private void OnMoleculesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = MoleculesChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void UpdateMoleculeHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object oldItem in e.OldItems)
                {
                    Molecule mol = (Molecule)oldItem;
                    mol.AtomsChanged -= Atoms_CollectionChanged;
                    mol.BondsChanged -= Bonds_CollectionChanged;
                    mol.PropertyChanged -= ChemObject_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (object newItem in e.NewItems)
                {
                    Molecule mol = (Molecule)newItem;
                    mol.AtomsChanged += Atoms_CollectionChanged;
                    mol.BondsChanged += Bonds_CollectionChanged;
                    mol.PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void Bonds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnBondsChanged(sender, e);
        }

        private void OnBondsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = BondsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void UpdateBondsPropertyHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object oldItem in e.OldItems)
                {
                    ((Bond)oldItem).PropertyChanged -= ChemObject_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (object newItem in e.NewItems)
                {
                    ((Bond)newItem).PropertyChanged += ChemObject_PropertyChanged;
                }
            }
        }

        private void Atoms_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAtomsChanged(sender, e);
        }

        private void OnAtomsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = AtomsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        private void ChemObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        #endregion Event handlers

        #region Methods

        //public void UpdateBondRefs(string oldID, string newID)
        //{
        //    var bstart = from b in this?.Bonds
        //        where b.StartAtomInternalId == oldID
        //        select b;
        //    foreach (Bond b in bstart)
        //    {
        //        b.StartAtomInternalId = newID;
        //    }

        //    var bend = from b in this?.Bonds
        //        where b.EndAtomInternalId == oldID
        //        select b;
        //    foreach (Bond b in bend)
        //    {
        //        b.EndAtomInternalId = newID;
        //    }
        //}

        public List<Atom> GetAtomNeighbours(Atom atom)
        {
            List<Atom> temps = new List<Atom>();
            foreach (Bond bond in Bonds)
            {
                if (bond.StartAtomInternalId.Equals(atom.InternalId))
                {
                    temps.Add(Atoms[bond.EndAtomInternalId]);
                }
                if (bond.EndAtomInternalId.Equals(atom.InternalId))
                {
                    temps.Add(Atoms[bond.StartAtomInternalId]);
                }
            }
            return temps.ToList();
        }

        public void ReLabel(bool includeNames, ref int iMolcount, ref int iAtomCount, ref int iBondcount)
        {
            //do the atoms first THEN the bonds
            //this sets the bond IDs
            Id = $"m{++iMolcount}";
            Dictionary<string, string> atomIDLookup = new Dictionary<string, string>();
            foreach (Atom a in Atoms.Values)
            {
                a.Id = $"a{++iAtomCount}";
            }

            foreach (Bond b in Bonds)
            {
                b.Id = $"b{++iBondcount}";
            }

            int count = 1;
            foreach (var formula in Formulas)
            {
                formula.Id = $"{Id}.f{count++}";
            }

            count = 1;
            foreach (var name in Names)
            {
                name.Id = $"{Id}.n{count++}";
            }

            if (Molecules.Any())
            {
                foreach (Molecule mol in Molecules.Values)
                {
                    mol.ReLabel(includeNames, ref iMolcount, ref iAtomCount, ref iBondcount);
                }
            }
        }

        //checks to make sure that all atom references within bonds are valid
        public bool CheckAtomRefs()
        {
            foreach (Bond bond in Bonds)
            {
                if (!Atoms.Keys.Contains(bond.StartAtomInternalId) || !Atoms.Keys.Contains(bond.EndAtomInternalId))
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerable<Bond> GetBonds(string atomID)
        {
            return (from startBond in Bonds
                    where startBond.StartAtomInternalId.Equals(atomID)
                    select startBond)
                .Union(from endBond in Bonds
                       where endBond.EndAtomInternalId.Equals(atomID)
                       select endBond);
        }

        public void Refresh()
        {
            foreach (var child in Molecules.Values)
            {
                child.Refresh();
            }

            RebuildRings();
        }

        public Molecule Clone()
        {
            Molecule clone = new Molecule().CloneExcept(this, new string[] { });
            foreach (KeyValuePair<string, Atom> keyValuePair in Atoms)
            {
                Atom atom = keyValuePair.Value;
                clone.AddAtom(atom.Clone());
            }

            foreach (Bond bond in Bonds)
            {
                clone.AddBond(bond.Clone());
            }

            foreach (KeyValuePair<string, Molecule> keyValuePair in Molecules)
            {
                Molecule child = keyValuePair.Value;
                Molecule tempMol = child.Clone();
                clone.AddMolecule(tempMol);
            }
            clone.RebuildRings();
            return clone;
        }

        #endregion Methods

        #region Overrides

        public override string ToString()
        {
            return $"Molecule {Id} - {InternalId}; Atoms {Atoms.Count} Bonds {Bonds.Count} Molecules {Molecules.Count}";
        }

        #endregion Overrides

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region Ring stuff

        /// <summary>
        /// How many rings the molecule contains.  Non cyclic molecules ALWAYS have atoms = bonds +1
        /// </summary>
        public int TheoreticalRings => Bonds.Count - Atoms.Count + 1;

        /// <summary>
        /// If the molecule has more atoms than bonds, it doesn't have a ring. Test this before running ring perception.
        /// </summary>
        public bool HasRings => TheoreticalRings > 0;

        /// <summary>
        /// Quick check to see if the rings need recalculating
        /// </summary>
        public bool RingsCalculated
        {
            get
            {
                if (!HasRings)
                {
                    return true;  //don't bother recaculating the rings for a linear molecule
                }
                else
                {
                    return Rings.Count > 0; //we have rings present, so why haven't we calculated them?
                }
            }
        }//have we calculated the rings yet?

        //private void RefreshRingBonds()
        //{
        //    foreach (Ring ring in Rings)
        //    {
        //        foreach (Bond ringBond in ring.Bonds)
        //        {
        //            ringBond.NotifyPlacementChanged();
        //        }
        //    }
        //}
        /// <summary>
        /// Cleaves off a degree 1 atom from the working set.
        /// Reduces the adjacent atoms' degree by one
        /// </summary>
        /// <param name="toPrune">Atom to prune</param>
        /// <param name="workingSet">Dictionary of atoms</param>
        private static void PruneAtom(Atom toPrune, Dictionary<Atom, int> workingSet)
        {
            foreach (Atom neighbour in toPrune.Neighbours)
            {
                if (workingSet.ContainsKey(neighbour))
                {
                    workingSet[neighbour] -= 1;
                }
            }
            workingSet.Remove(toPrune);
        }

        /// <summary>
        /// Removes side chain atoms from the working set
        /// DOES NOT MODIFY the original molecule!
        /// Assumes we don't have any degree zero atoms
        /// (i.e this isn't a single atom Molecule)
        /// </summary>
        /// <param name="projection">Molecule to prune</param>
        public static void PruneSideChains(Dictionary<Atom, int> projection)
        {
            bool hasPruned = true;

            while (hasPruned)
            {
                //clone the working set atoms first because otherwise LINQ will object

                List<Atom> atomList = new List<Atom>();
                foreach (KeyValuePair<Atom, int> kvp in projection)
                {
                    if (kvp.Value < 2)
                    {
                        atomList.Add(kvp.Key);
                    }
                }
                hasPruned = atomList.Count > 0;

                foreach (Atom a in atomList)
                {
                    PruneAtom(a, projection);
                }
            }
        }

        public Dictionary<Atom, T> Projection<T>(Func<Atom, T> getProperty)
        {
            return Atoms.Values.ToDictionary(a => a, a => getProperty(a));
        }

        public void RebuildRings()
        {
            RebuildRingsFigueras();

            // -------------- //
            // Local Function //
            // -------------- //
            void RebuildRingsFigueras()
            {
#if DEBUG
                //Stopwatch sw = new Stopwatch();
                //sw.Start();
#endif
                Rings.Clear();

                if (HasRings)
                {
                    //working set of atoms
                    //it's a dictionary, because we initially store the degree of each atom against it
                    //this will change as the pruning operation kicks in
                    Dictionary<Atom, int> workingSet = Projection(a => a.Degree);
                    //lop off any terminal branches
                    PruneSideChains(workingSet);

                    while (workingSet.Any()) //do we have any atoms left in the set
                    {
                        Atom startAtom = workingSet.Keys.OrderByDescending(a => a.Degree).First(); // go for the highest degree atom
                        Ring nextRing = GetRing(startAtom); //identify a ring
                        if (nextRing != null) //bingo
                        {
                            //and add the ring to the atoms
                            Rings.Add(nextRing); //add the ring to the set
                            foreach (Atom a in nextRing.Atoms.ToList())
                            {
                                //if (!a.Rings.Contains(nextRing))
                                //{
                                //    a.Rings.Add(nextRing);
                                //}

                                if (workingSet.ContainsKey(a))
                                {
                                    workingSet.Remove(a);
                                }
                                //remove the atoms in the ring from the working set BUT NOT the graph!
                            }
                        }
                        else
                        {
                            workingSet.Remove(startAtom);
                        } //the atom doesn't belong in a ring, remove it from the set.
                    }
                }
#if DEBUG
                //Debug.WriteLine($"Molecule = {(ChemicalNames.Count > 0 ? this.ChemicalNames?[0].Name : this.ConciseFormula)},  Number of rings = {Rings.Count}");
                //sw.Stop();
                //Debug.WriteLine($"Elapsed {sw.ElapsedMilliseconds}");
#endif
                //RefreshRingBonds();
            }

            // -------------- //
            // Local Function //
            // -------------- //

            // Modified Figueras top-level algorithm:
            // 1. choose the lowest degree atom
            // 2. Work out which rings it belongs to
            // 3. If it belongs to a ring and that ring hasn't been calculated before, then add it to the set
            // 4. delete the atom from the projection, reduce the degree of neighbouring atoms and prune away the side chains
            void RebuildRingsFiguerasAlt()
            {
#if DEBUG
                Stopwatch sw = new Stopwatch();
                sw.Start();
#endif
                HashSet<string> RingIDs = new HashSet<string>(); //list of rings processed so far
                if (HasRings)
                {
                    WipeMoleculeRings();

                    //working set of atoms
                    //it's a dictionary, because we initially store the degree of each atom against it
                    //this will change as the pruning operation kicks in
                    Dictionary<Atom, int> workingSet = Projection(a => a.Degree);
                    //lop off any terminal branches - removes all atoms of degree <=1
                    PruneSideChains(workingSet);

                    while (workingSet.Any()) //do we have any atoms left in the set
                    {
                        Atom startAtom = workingSet.OrderBy(kvp => kvp.Value).First().Key; // go for the lowest degree atom (will be of degree >=2)
                        Ring nextRing = GetRing(startAtom); //identify a ring

                        if (nextRing != null && !RingIDs.Contains(nextRing.UniqueID)) //bingo
                        {
                            //and add the ring to the atoms
                            Rings.Add(nextRing); //add the ring to the set
                            RingIDs.Add(nextRing.UniqueID);

                            foreach (Atom a in nextRing.Atoms.ToList())
                            {
                                a.Rings.Add(nextRing);
                            }

                            //
                            if (workingSet.ContainsKey(startAtom))
                            {
                                foreach (Atom atom in startAtom.Neighbours.Where(a => workingSet.ContainsKey(a)))
                                {
                                    //reduce the degree of its neighbours by one
                                    workingSet[atom] -= 1;
                                }
                                //break the ring

                                workingSet.Remove(startAtom);
                                //and chop down the dangling chains
                                PruneSideChains(workingSet);
                            }
                            //remove the atoms in the ring from the working set BUT NOT the graph!
                        }
                        else
                        {
                            Debug.Assert(workingSet.ContainsKey(startAtom));
                            workingSet.Remove(startAtom);
                        } //the atom doesn't belong in a ring, remove it from the set.
                    }
                }
#if DEBUG
                //Debug.WriteLine($"Molecule = {(ChemicalNames.Count > 0 ? this.ChemicalNames?[0].Name : this.ConciseFormula)},  Number of rings = {Rings.Count}");
                sw.Stop();
                Debug.WriteLine($"Elapsed {sw.ElapsedMilliseconds}");
#endif
            }
        }

        /// <summary>
        /// Sorts rings for double bond placement
        /// using Alex Clark's rules
        /// </summary>
        public List<Ring> SortedRings
        {
            get
            {
                if (_sortedRings == null)
                {
                    _sortedRings = SortRingsForDBPlacement();
                }
                return _sortedRings;
            }
        }

        private void WipeMoleculeRings()
        {
            Rings.Clear();

            //first set all atoms to side chains
            foreach (Atom a in Atoms.Values)
            {
                a.Rings.Clear();
                _sortedRings = null;
            }
        }

        /// <summary>
        /// Sorts a series of small rings ready for determining double bond placement
        /// see DOI: 10.1002/minf.201200171
        /// Rendering molecular sketches for publication quality output
        /// Alex M Clark
        /// </summary>
        /// <returns>List of rings</returns>
        // ReSharper disable once InconsistentNaming
        public List<Ring> SortRingsForDBPlacement()
        {
            //
            //Debug.Assert(HasRings); //no bloody point in running this unless it has rings
            //Debug.Assert(RingsCalculated); //make sure that if the molecule contains rings that we have calculated them
            //1) All rings of sizes 6, 5, 7, 4 and 3 are discovered, in that order, and added to a list R.
            List<Ring> prioritisedRings = Rings.Where(x => x.Priority > 0).OrderBy(x => x.Priority).ToList();

            //Define B as an array of size equal to the number of atoms, where each value
            //is equal to the number of times the atom occurs in any of the rings R
            Dictionary<Atom, int> atomFrequency = new Dictionary<Atom, int>();
            foreach (Atom atom in Atoms.Values)
            {
                atomFrequency[atom] = atom.Rings.Count;
            }

            //Define Q as an array of size equal to length of R, where each value is equal
            //to sum of B[r], where r iterates over each of the atoms within the ring.
            Dictionary<Ring, int> cumulFreqPerRing = new Dictionary<Ring, int>();
            foreach (Ring ring in prioritisedRings)
            {
                int sumBr = 0;
                foreach (Atom atom in ring.Atoms)
                {
                    sumBr += atomFrequency[atom];
                }

                cumulFreqPerRing[ring] = sumBr;
            }

            //Perform a stable sort of the list of rings, R, so that those with the lowest values of Q are listed first.
            IOrderedEnumerable<Ring> lowestCumulFreq = prioritisedRings.OrderBy(r => cumulFreqPerRing[r]);

            //Define D as an array of size equal to length of R, where each value is equal to the number of double bonds within the corresponding ring
            Dictionary<Ring, int> doubleBondsperRing = new Dictionary<Ring, int>();
            foreach (Ring ring in lowestCumulFreq)
            {
                doubleBondsperRing[ring] = ring.Bonds.Count(b => b.OrderValue == 2);
            }

            //Perform a stable sort of the list of rings, R, so that those with highest values of D are listed first

            IOrderedEnumerable<Ring> highestDBperRing = lowestCumulFreq.OrderByDescending(r => doubleBondsperRing[r]);

            return highestDBperRing.ToList();
        }

        /// <summary>
        /// Start with an atom and detect which ring it's part of
        /// </summary>
        /// <param name="startAtom">Atom of degree >= 2</param>
        ///
        private static Ring GetRing(Atom startAtom)
        {
            // Only returns the first ring.
            //
            // Uses the Figueras algorithm
            // Figueras, J, J. Chem. Inf. Comput. Sci., 1996,36, 96, 986-991
            // The algorithm goes as follows:
            //1. Remove node frontNode and its Source from the front of the queue.
            //2. For each node m attached to frontNode, and not equal to Source:
            //If path[m] is null, compute path[m] ) path[frontNode] +[m]
            //and put node m(with its Source, frontNode) on the back of the queue.
            //If path[m] is not null then
            //      1) Compute the intersection path[frontNode]*path[m].
            //      2) If the intersection is a singleton, compute the ring set  path[m]+path[frontNode] and exit.
            //3. Return to step 1.
            //set up the data structures
            Queue<AtomData> atomsSoFar; //needed for BFS
            Dictionary<Atom, HashSet<Atom>> path = new Dictionary<Atom, HashSet<Atom>>();

            //initialise all the paths to empty
            foreach (Atom atom in startAtom.Parent.Atoms.Values)
            {
                path[atom] = new HashSet<Atom>();
            }
            //set up a new queue
            atomsSoFar = new Queue<AtomData>();

            //set up a front node and shove it onto the queue
            //shove the neigbours onto the queue to prime it
            foreach (Atom initialAtom in startAtom.Neighbours)
            {
                AtomData node = new AtomData() { Source = startAtom, CurrentAtom = initialAtom };
                path[initialAtom] = new HashSet<Atom>() { startAtom, initialAtom };
                atomsSoFar.Enqueue(node);
            }
            //now scan the Molecule and detect all rings
            while (atomsSoFar.Any())
            {
                AtomData frontNode = atomsSoFar.Dequeue();
                foreach (Atom m in frontNode.CurrentAtom.Neighbours)
                {
                    if (m != frontNode.Source) //ignore an atom that we've visited
                    {
                        if ((!path.ContainsKey(m)) || (path[m].Count == 0)) //null path
                        {
                            HashSet<Atom> temp = new HashSet<Atom>();
                            temp.Add(m);
                            temp.UnionWith(path[frontNode.CurrentAtom]);
                            path[m] = temp; //add on the path built up so far
                            AtomData newItem = new AtomData { Source = frontNode.CurrentAtom, CurrentAtom = m };
                            atomsSoFar.Enqueue(newItem);
                        }
                        else //we've got a collision - is it a ring closure
                        {
                            HashSet<Atom> overlap = new HashSet<Atom>();
                            overlap.UnionWith(path[frontNode.CurrentAtom]); //clone this set
                            overlap.IntersectWith(path[m]);
                            if (overlap.Count == 1) //we've had a singleton overlap :  ring closure
                            {
                                HashSet<Atom> ringAtoms = new HashSet<Atom>();
                                ringAtoms.UnionWith(path[m]);
                                ringAtoms.UnionWith(path[frontNode.CurrentAtom]);

                                return new Ring(ringAtoms);
                            }
                        }
                    }
                }
            }
            //no collisions therefore no rings detected
            return null;
        }

        #endregion Ring stuff
    }
}