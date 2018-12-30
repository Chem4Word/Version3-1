// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model.Enums;
using Chem4Word.Model.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chem4Word.Model
{
    /// <summary>
    /// Represents a single molecule.
    /// *Please* do not persist molecule references
    /// between editing operations.  We cannot promise
    /// that the references will stay current
    /// </summary>
    [Serializable]
    public partial class Molecule : ChemistryContainer
    {
        #region Fields

        private Rect _boundingBox = Rect.Empty;

        public void ResetBoundingBox()
        {
            _boundingBox = Rect.Empty;
        }

        public Rect BoundingBox
        {
            get
            {
                if (_boundingBox == Rect.Empty)
                {
                    CalculateBoundingBox();
                }

                return _boundingBox;
            }
        }

        private void CalculateBoundingBox()
        {
            Model m = this.Model;
            if (m != null & Atoms.Count > 1)
            {
                var xMax = Atoms.Select(a => a.BoundingBox(m.FontSize).Right).Max();
                var xMin = Atoms.Select(a => a.BoundingBox(m.FontSize).Left).Min();

                var yMax = Atoms.Select(a => a.BoundingBox(m.FontSize).Bottom).Max();
                var yMin = Atoms.Select(a => a.BoundingBox(m.FontSize).Top).Min();

                _boundingBox = new Rect(new Point(xMin, yMin), new Point(xMax, yMax));
            }
            else if (m != null & Atoms.Count == 1)
            {
                _boundingBox = Atoms[0].BoundingBox(Model.FontSize);
            }
            else
            {
                _boundingBox = new Rect(new Size(0.0, 0.0));
            }
        }

        public string ConciseFormula { get; set; }

        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }

        #endregion Fields

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public Molecule()
        {
            Atoms = new ObservableCollection<Atom>();
            Bonds = new ObservableCollection<Bond>();
            Rings = new ObservableCollection<Ring>();

            ChemicalNames = new ObservableCollection<ChemicalName>();
            Formulas = new ObservableCollection<Formula>();

            Atoms.CollectionChanged += Atoms_CollectionChanged;
            Bonds.CollectionChanged += Bonds_CollectionChanged;
            Rings.CollectionChanged += Rings_CollectionChanged;

            Warnings = new List<string>();
            Errors = new List<string>();

            this.Id = Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Generates a molecule from a given atom
        /// </summary>
        /// <param name="seed"></param>
        public Molecule(Atom seed) : this()
        {
            Refresh(seed);
        }

        #endregion Constructors

        public override string ToString()
        {
            return $"Molecule: {Id}";
        }

        /// <summary>
        /// Calculated Molecular Formula
        /// </summary>
        /// <returns></returns>
        public string CalculatedFormula()
        {
            string result = "";

            Dictionary<string, int> f = new Dictionary<string, int>();
            SortedDictionary<string, int> r = new SortedDictionary<string, int>();

            f.Add("C", 0);
            f.Add("H", 0);

            foreach (Atom atom in Atoms)
            {
                // ToDo: Do we need to check if this is a functional group here?

                switch (atom.Element.Symbol)
                {
                    case "C":
                        f["C"]++;
                        break;

                    case "H":
                        f["H"]++;
                        break;

                    default:
                        if (!r.ContainsKey(atom.SymbolText))
                        {
                            r.Add(atom.SymbolText, 1);
                        }
                        else
                        {
                            r[atom.SymbolText]++;
                        }
                        break;
                }

                int hCount = atom.ImplicitHydrogenCount;
                if (hCount > 0)
                {
                    f["H"] += hCount;
                }
            }

            foreach (KeyValuePair<string, int> kvp in f)
            {
                if (kvp.Value > 0)
                {
                    result += $"{kvp.Key} {kvp.Value} ";
                }
            }
            foreach (KeyValuePair<string, int> kvp in r)
            {
                result += $"{kvp.Key} {kvp.Value} ";
            }

            return result.Trim();
        }

        private void Refresh(Atom seed, HashSet<Atom> checklist = null)
        {
            //keep a list of the atoms to refer to later when rebuilding

            //set the parent to null but keep a list of all atoms
            if (checklist == null)
            {
                checklist = new HashSet<Atom>();

                foreach (Atom atom in Atoms)
                {
                    atom.Parent = null;
                    checklist.Add(atom);
                }
                checklist.Add(seed);
            }

            var startingMol = this;

            Trash(startingMol);

            Queue<Atom> feed = new Queue<Atom>();
            feed.Enqueue(seed);
            while (feed.Any())
            {
                Atom toDo = feed.Dequeue();
                checklist.Remove(toDo);
                startingMol.Atoms.Add(toDo);
                toDo.Parent = startingMol;

                foreach (Bond bond in toDo.Bonds)
                {
                    if (bond.Parent == null || bond.Parent != startingMol)
                    {
                        startingMol.Bonds.Add(bond);
                    }
                }

                foreach (Atom neighbour in toDo.Neighbours.Where(n => (n.Parent == null || n.Parent != startingMol) & !feed.Contains(n)))
                {
                    feed.Enqueue(neighbour);
                }
            }
            startingMol.RebuildRings();
            Debug.Assert(!startingMol.Atoms.Any(a => a.Parent == null));
            Debug.Assert(!startingMol.Bonds.Any(b => b.Parent == null));

            if (checklist.Any()) //there are still some atoms unaccounted for after the search
                                 //therefore disconnected from the first graph
            {
                seed = checklist.First();
                //checklist.Remove(seed);
                startingMol = new Molecule();
                startingMol.Parent = Parent;
                startingMol.Refresh(seed, checklist);
                Parent.Molecules.Add(startingMol);
            }
        }

        private static void Trash(Molecule startingMol)
        {
            //clear the associated collections
            startingMol.Atoms.RemoveAll();
            startingMol.AllAtoms.RemoveAll();
            foreach (Bond bond in startingMol.Bonds)
            {
                bond.Parent = null;
            }
            startingMol.Bonds.RemoveAll();
            startingMol.AllBonds.RemoveAll();
            startingMol.Rings.RemoveAll();
        }

        /// <summary>
        /// rebuilds the molecule without trashing it
        /// </summary>
        public void Refresh()
        {
            if (Atoms.Any())
            {
                Atom start = this.Atoms[0];
                Refresh(start);
            }
            foreach (Molecule molecule in Molecules.ToList())
            {
                if (molecule.Molecules.Count == 0 && molecule.Atoms.Count == 0)
                {
                    //it's empty, trash it
                    Molecules.Remove(molecule);
                }
                else
                {
                    molecule.Refresh();
                }
            }
        }

        #region Properties

        public ObservableCollection<ChemicalName> ChemicalNames { get; private set; }

        public ObservableCollection<Bond> Bonds { get; private set; }

        public ObservableCollection<Ring> Rings { get; private set; }

        public ObservableCollection<Atom> Atoms { get; private set; }

        //aggregating collections:
        //metadata
        public ObservableCollection<Formula> Formulas { get; private set; }

        /// <summary>
        /// Returns a snapshot of operations performed on each atom
        /// </summary>
        /// <param name="getproperty">Delegate or lambda function which should return a value to store against atom. </param>
        /// <returns></returns>
        public Dictionary<Atom, T> Projection<T>(Func<Atom, T> getproperty)
        {
            return Atoms.ToDictionary(a => a, a => getproperty(a));
        }

        public int FormalCharge { get; set; }
        public MoleculeRole Role { get; set; }

        /// <summary>
        /// Must be set if a molecule is a child of another.
        /// </summary>
        ///
        private int? _count;

        public int? Count
        {
            get { return _count; }

            set
            {
                if (value != null)
                {
                    if (Parent == null)
                    {
                        Debugger.Break();
                        throw new ArgumentOutOfRangeException("Not allowed to set Count on a top-level molecule");
                    }
                }
                _count = value;
            }
        }

        /// <summary>
        /// Use the Tag during editing operations to store state
        /// Not persisted to the model
        /// </summary>
        public object Tag { get; set; }

        #endregion Properties

        #region Methods

        #region Graph Stuff

        /// <summary>
        /// Traverses a molecular graph applying an operation to each and every atom.
        /// Does not require that the atoms be already part of a Molecule.
        /// </summary>
        /// <param name="startAtom">start atom</param>
        /// <param name="operation">delegate pointing to operation to perform</param>
        /// <param name="isntProcessed"> Predicate test to tell us whether or not to process an atom</param>
        private void DepthFirstTraversal(Atom startAtom, Action<Atom> operation, Predicate<Atom> isntProcessed)
        {
            operation(startAtom);

            while (startAtom.UnprocessedDegree(isntProcessed) > 0)
            {
                if (startAtom.UnprocessedDegree(isntProcessed) == 1)
                {
                    startAtom = NextUnprocessedAtom(startAtom, isntProcessed);
                    operation(startAtom);
                }
                else
                {
                    var unassignedAtom = from a in startAtom.Neighbours
                                         where isntProcessed(a)
                                         select a;
                    foreach (Atom atom in unassignedAtom)
                    {
                        DepthFirstTraversal(atom, operation, isntProcessed);
                    }
                }
            }
        }

        /// <summary>
        /// Cleaves off a degree 1 atom from the working set.
        /// Reduces the adjacent atoms' degree by one
        /// </summary>
        /// <param name="toPrune">Atom to prune</param>
        /// <param name="workingSet">Dictionary of atoms</param>
        private static void PruneAtom(Atom toPrune, Dictionary<Atom, int> workingSet)
        {
            foreach (var neighbour in toPrune.Neighbours)
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

                Atom[] atomList = (from kvp in projection
                                   where kvp.Value < 2
                                   select kvp.Key).ToArray();
                hasPruned = atomList.Length > 0;

                foreach (Atom a in atomList)
                {
                    PruneAtom(a, projection);
                }
            }
        }

        public Model Model
        {
            get
            {
                object currentParent = Parent;
                while (currentParent != null && !(currentParent.GetType() == typeof(Model)))
                {
                    currentParent = ((ChemistryContainer)currentParent).Parent;
                }
                return (currentParent as Model);
            }
        }

        #endregion Graph Stuff

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

        public void RebuildRingsFigueras()
        {
#if DEBUG
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
#endif
            WipeMoleculeRings();

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
                    var startAtom = workingSet.Keys.OrderByDescending(a => a.Degree).First(); // go for the highest degree atom
                    Ring nextRing = GetRing(startAtom); //identify a ring
                    if (nextRing != null) //bingo
                    {
                        //and add the ring to the atoms
                        Rings.Add(nextRing); //add the ring to the set
                        foreach (Atom a in nextRing.Atoms.ToList())
                        {
                            if (!a.Rings.Contains(nextRing))
                            {
                                a.Rings.Add(nextRing);
                            }

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
            RefreshRingBonds();
        }

        private void RefreshRingBonds()
        {
            foreach (Ring ring in Rings)
            {
                foreach (Bond ringBond in ring.Bonds)
                {
                    ringBond.NotifyPlacementChanged();
                }
            }
        }

        public void RebuildRings()
        {
            RebuildRingsFigueras();
        }

        /// <summary>
        /// Modified Figueras top-level algorithm:
        /// 1. choose the lowest degree atom
        /// 2. Work out which rings it belongs to
        /// 3. If it belongs to a ring and that ring hasn't been calculated before, then add it to the set
        /// 4. delete the atom from the projection, reduce the degree of neighbouring atoms and prune away the side chains
        /// </summary>
        private void RebuildRingsFiguerasAlt()
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
                    var startAtom = workingSet.OrderBy(kvp => kvp.Value).First().Key; // go for the lowest degree atom (will be of degree >=2)
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
            Debug.WriteLine($"Molecule = {(ChemicalNames.Count > 0 ? this.ChemicalNames?[0].Name : this.ConciseFormula)},  Number of rings = {Rings.Count}");
            sw.Stop();
            Debug.WriteLine($"Elapsed {sw.ElapsedMilliseconds}");
#endif
        }

        /// <summary>
        /// Inplements RP Path
        /// see Lee, C. J., Kang, Y.-M., Cho, K.-H., & No, K. T. (2009).
        ///  A robust method for searching the smallest set of smallest rings
        /// with a path-included distance matrix. Proceedings of the National Academy
        /// of Sciences of the United States of America, 106(41), 17355–17358.
        public void RebuildRingsRPPath()
        {
            // ReSharper disable once InconsistentNaming
            //local function for calculating the PID matrices

            Stopwatch sw = new Stopwatch();
            Stopwatch sw1, sw2, sw3;
            var s = (ChemicalNames.Any() ? ChemicalNames.Last().Name : "[no name]");
            Debug.WriteLine($"Recalculating rings for {s}");
            sw.Start();
            if (HasRings)
            {
                WipeMoleculeRings();

                Dictionary<Atom, int> currentSet = Projection(a => a.Degree);
                //lop off any terminal branches
                sw1 = new Stopwatch();
                sw1.Start();
                PruneSideChains(currentSet);
                var currentSetCount = currentSet.Count;
                List<BitArray>[,] pidMatrix = new List<BitArray>[currentSetCount, currentSetCount];
                List<BitArray>[,] pidMatrixPlus = new List<BitArray>[currentSetCount, currentSetCount];

                int[,] distances = new int[currentSetCount, currentSetCount];

                //int[,,] distancesList = new int[currentSetCount+1,currentSetCount,currentSetCount];

                var candidateSets = new List<(float count, EdgeList shortPath, EdgeList longPath)>();
                //store the atoms in an array for now - makes it easier
                var workingAtoms = currentSet.Keys.ToArray();

                //Phase 0
                //initialise the D0 matrix and the PID matrix

                //first, create an array of bonds which we can use to convert bit arrays to later

                var workingBonds = this.Bonds.ToArray();
                var bondCount = this.Bonds.Count;

                #region local help functions for phase 0

                int lookupBondIndex(Bond val)
                {
                    return Array.IndexOf(workingBonds, val);
                }

                EdgeList[] ToEdgeList(List<BitArray> pm)
                {
                    var listEdgeList = new List<EdgeList>();

                    foreach (var bitArray in pm)
                    {
                        var edgeList = new EdgeList();
                        listEdgeList.Add(edgeList);
                        for (int i = 0; i < bitArray.Length; i++)
                        {
                            if (bitArray[i])
                            {
                                edgeList.Add(workingBonds[i]);
                            }
                        }
                    }
                    return listEdgeList.ToArray();
                }

                #endregion local help functions for phase 0

                //the maximum possible distance between two atoms is the
                //number of bonds in the molecule
                int maxDistance = 2 * Bonds.Count + 1;

                for (int i = 0; i < currentSetCount; i++)
                {
                    for (int j = 0; j < currentSetCount; j++)
                    {
                        Atom a = workingAtoms[i];
                        Atom b = workingAtoms[j];

                        var bondBetween = a.BondBetween(b);

                        if (i != j)
                        {
                            distances[i, j] = bondBetween != null ? 1 : maxDistance;
                        }
                        else
                        {
                            distances[i, i] = 0;
                        }
                        if (bondBetween != null)
                        {
                            BitArray bondFlags = new BitArray(bondCount);
                            bondFlags[lookupBondIndex(bondBetween)] = true;//set the bit corresponding to the bond
                            pidMatrix[i, j] = new List<BitArray> { bondFlags };
                        }
                        else
                        {
                            pidMatrix[i, j] = new List<BitArray>();
                        }

                        pidMatrixPlus[i, j] = new List<BitArray>();
                    }
                }

                //Phase 1
                //now calculate the PID matrices

                for (int k = 0; k < currentSetCount; k++)
                {
                    Parallel.For(0, currentSetCount, i =>
                    {
                        Parallel.For(0, currentSetCount, j =>
                        {
                            BitArray shortPath = pidMatrix[i, k].Count > 0
                                ? pidMatrix[i, k].Last()
                                : new BitArray(bondCount);
                            BitArray longPath = pidMatrix[k, j].Count > 0
                                ? pidMatrix[k, j].Last()
                                : new BitArray(bondCount);
                            if (i != j & j != k & k != i)
                            {
                                int dfull = distances[i, j];
                                int dFirst = distances[i, k];
                                int d3 = distances[k, j];
                                if (dfull > dFirst + d3) //a new shortest path
                                {
                                    if (dfull == dFirst + d3 + 1)
                                    //which is equal to the previous path -1
                                    {
                                        //pidMatrixPlus[i, j] = Array.Empty<EdgeList>(); //change the old path

                                        pidMatrixPlus[i, j] = new List<BitArray>();
                                        foreach (var bitArray in pidMatrix[i, j])
                                        {
                                            pidMatrixPlus[i, j].Add(bitArray);
                                        }
                                    }
                                    else
                                    {
                                        pidMatrixPlus[i, j].Clear();
                                    }
                                    distances[i, j] = dFirst + d3;
                                    BitArray tempPath = (BitArray)longPath.Clone();
                                    tempPath.Or(shortPath);
                                    pidMatrix[i, j] = new List<BitArray> { tempPath };
                                }
                                else if (dfull == dFirst + d3) //another shortest path
                                {
                                    //so append the path to the list
                                    BitArray tempPath = (BitArray)longPath.Clone();
                                    tempPath.Or(shortPath);
                                    pidMatrix[i, j].Add(tempPath);
                                }
                                else if (dfull == dFirst + d3 - 1) //shortest + 1 path
                                {
                                    //append the path
                                    BitArray tempPath = (BitArray)longPath.Clone();
                                    tempPath.Or(shortPath);
                                    pidMatrixPlus[i, j].Add(tempPath);
                                }
                            }
                        }
                        );
                    }
                    );
                }

                sw1.Stop();
                //Phase 2
                //now do the ring candidate search
                sw2 = new Stopwatch();
                sw2.Start();

                int cycleNum = 0;
                //list of candidate ring sets
                HashSet<(int cyclenum, EdgeList[] shortPath, EdgeList[] longPath)> candidates =
                    new HashSet<(int cyclenum, EdgeList[] shortPath, EdgeList[] longPath)>();

                for (int i = 0; i < currentSetCount; i++)
                {
                    for (int j = 0; j < currentSetCount; j++)
                    {
                        if (distances[i, j] != 0 && distances[i, j] != maxDistance
                            && !(pidMatrix[i, j].Any() & !pidMatrixPlus[i, j].Any()))
                        {
                            if (pidMatrixPlus[i, j].Count != 0)
                            {
                                cycleNum = Convert.ToInt32(2 * (distances[i, j] + 0.5));
                            }
                            else
                            {
                                cycleNum = Convert.ToInt32(2 * distances[i, j]);
                            }
                            candidates.Add((cycleNum, ToEdgeList(pidMatrix[i, j]), ToEdgeList(pidMatrixPlus[i, j])));
                        }
                    }
                }
                sw2.Stop();
                Debug.WriteLine("Distance Matrix");

                void PrintDistanceMatrix(float[,] dm)
                {
                    Debug.WriteLine("---------------------------------------------------------");
                    for (int i = 0; i <= dm.GetUpperBound(0); i++)
                    {
                        for (int j = 0; j <= dm.GetUpperBound(1); j++)
                        {
                            Debug.Write($"({dm[i, j]})");
                            Debug.Write("\t");
                        }
                        Debug.WriteLine("");
                    }
                    Debug.WriteLine("---------------------------------------------------------");
                }

                void PrintPIDMatrix(List<BitArray>[,] pm)
                {
                    Debug.WriteLine("---------------------------------------------------------");
                    for (int i = 0; i <= pm.GetUpperBound(0); i++)
                    {
                        for (int j = 0; j <= pm.GetUpperBound(1); j++)
                        {
                            List<BitArray> elist = pm[i, j];
                            EdgeList[] el = ToEdgeList(elist);
                            Debug.Write("(");
                            for (int k = 0; k < elist.Count; k++)
                            {
                                if (k != 0)
                                {
                                    Debug.Write(",");
                                }
                                Debug.Write(el[k].ToString());
                            }
                            Debug.Write(")");
                            Debug.Write("\t");
                        }
                        Debug.WriteLine("");
                    }
                    Debug.WriteLine("---------------------------------------------------------");
                }

                //PrintDistanceMatrix(distances);

                //Debug.WriteLine("PID matrix");
                //PrintPIDMatrix(pidMatrix);
                //Debug.WriteLine("PID+ matrix");
                //PrintPIDMatrix(pidMatrixPlus);

                //Phase 3
                //construct the ring and find the SSSR
                //see Dyott, T. M., & Wipke, W. T. (1975). Use of Ring Assemblies in
                //Ring Perception Algorithm. Journal of Chemical Information and Computer Sciences,
                //15(3), 140–147. https://doi.org/10.1021/ci60003a003

                //first, sort the candidate ringsets by ascending length
                sw3 = new Stopwatch();
                var sortedCandidates = candidates.OrderBy(c => c.cyclenum);
                sw3.Start();
                //make a hashset of edgelists to hold the SSSR

                HashSet<EdgeList> cSSSR = new HashSet<EdgeList>(new EdgeListComparer());
                //HashSet<EdgeList> cSSSR = new HashSet<EdgeList>();

                int nRingIndex = 0;

                EdgeList allBonds = new EdgeList();

                void AddRing(EdgeList tempring)
                {
                    if (!tempring.IsSubsetOf(allBonds))
                    {
                        if (!cSSSR.Contains(tempring))
                        {
                            if (cSSSR.Count == 0)
                            {
                                cSSSR.Add(tempring);
                                Debug.WriteLine(tempring.ToString());
                                allBonds.UnionWith(tempring);
                                nRingIndex += 1;
                            }
                            else
                            {
                                foreach (var ring in cSSSR)
                                {
                                    var newring = ring ^ tempring;
                                    if (!cSSSR.Contains(tempring))
                                    {
                                        cSSSR.Add(tempring);
                                        Debug.WriteLine(tempring.ToString());
                                        allBonds.UnionWith(tempring);
                                        nRingIndex += 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                bool isComplete = false;

                foreach (var candidate in sortedCandidates)
                {
                    var sp = candidate.shortPath.ToArray();
                    var lp = candidate.longPath.ToArray();

                    if (candidate.cyclenum % 2 != 0) //it's odd
                    {
                        EdgeList ringbonds;
                        for (int j = 0; j < candidate.longPath.Length; j++)
                        {
                            var tempring = sp[0] + lp[j];
                            AddRing(tempring);
                            if (nRingIndex == TheoreticalRings)
                            {
                                isComplete = true;
                                break;
                            }
                        }
                    }
                    if (isComplete)
                    {
                        break;
                    }

                    if (nRingIndex != TheoreticalRings)
                    {
                        if (candidate.cyclenum % 2 == 0) //it's even
                        {
                            for (int j = 0; j < candidate.shortPath.Length - 1; j++)
                            {
                                var tempring = sp[j + 1] + sp[j];
                                AddRing(tempring);
                                if (nRingIndex == TheoreticalRings)
                                {
                                    isComplete = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (isComplete)
                    {
                        break;
                    }
                }

                foreach (EdgeList edgeList in cSSSR)
                {
                    Rings.Add(new Ring(edgeList));
                }
                sw3.Stop();

                float totalTime = sw1.ElapsedMilliseconds + sw2.ElapsedMilliseconds + sw3.ElapsedMilliseconds;

                Debug.WriteLine($"{s} Phase 1: {sw1.ElapsedMilliseconds / totalTime}");
                Debug.WriteLine($"{s} Phase 2: {sw2.ElapsedMilliseconds / totalTime}");
                Debug.WriteLine($"{s} Phase 3: {sw3.ElapsedMilliseconds / totalTime}");
            }

            sw.Stop();
            Debug.WriteLine($"Elapsed time for {s}  = {sw.ElapsedMilliseconds} ms");
        }

        private List<Ring> _sortedRings = null;

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
            Rings.RemoveAll();

            //first set all atoms to side chains
            foreach (var a in Atoms)
            {
                a.Rings.RemoveAll();
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
            var prioritisedRings = Rings.Where(x => x.Priority > 0).OrderBy(x => x.Priority).ToList();

            //Define B as an array of size equal to the number of atoms, where each value
            //is equal to the number of times the atom occurs in any of the rings R
            Dictionary<Atom, int> atomFrequency = new Dictionary<Atom, int>();
            foreach (Atom atom in Atoms)
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
            var lowestCumulFreq = prioritisedRings.OrderBy(r => cumulFreqPerRing[r]);

            //Define D as an array of size equal to length of R, where each value is equal to the number of double bonds within the corresponding ring
            Dictionary<Ring, int> doubleBondsperRing = new Dictionary<Ring, int>();
            foreach (Ring ring in lowestCumulFreq)
            {
                doubleBondsperRing[ring] = ring.Bonds.Count(b => b.OrderValue == 2);
            }

            //Perform a stable sort of the list of rings, R, so that those with highest values of D are listed first

            var highestDBperRing = lowestCumulFreq.OrderByDescending(r => doubleBondsperRing[r]);

            return highestDBperRing.ToList();
        }

        //noddy nested class for ring detection
        public class AtomData
        {
            public Atom CurrentAtom { get; set; }
            public Atom Source { get; set; }
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
            foreach (var atom in startAtom.Parent.Atoms)
            {
                path[atom] = new HashSet<Atom>();
            }
            //set up a new queue
            atomsSoFar = new Queue<AtomData>();

            //set up a front node and shove it onto the queue
            //shove the neigbours onto the queue to prime it
            foreach (Atom initialAtom in startAtom.Neighbours)
            {
                var node = new AtomData() { Source = startAtom, CurrentAtom = initialAtom };
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
                            var temp = new HashSet<Atom>();
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
                                var ringAtoms = new HashSet<Atom>();
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

        private static Atom NextUnprocessedAtom(Atom seed, Predicate<Atom> isntProcessed)
        {
            return seed.Neighbours.First(a => isntProcessed(a));
        }

        #endregion Methods

        private void Rings_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Ring ring in e.NewItems)
                    {
                        ring.Parent = this;
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Ring ring in e.OldItems)
                    {
                        ring.Parent = null;
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Ring ring in e.NewItems)
                    {
                        ring.Parent = this;
                    }
                    foreach (Ring ring in e.OldItems)
                    {
                        ring.Parent = null;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void Bonds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Bond bond in e.NewItems)
                    {
                        bond.Parent = this;
                        AllBonds.Add(bond);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Bond bond in e.OldItems)
                    {
                        AllBonds.Remove(bond);
                        bond.Parent = null;
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Bond bond in e.NewItems)
                    {
                        bond.Parent = this;
                        AllBonds.Add(bond);
                    }
                    foreach (Bond bond in e.OldItems)
                    {
                        AllBonds.Remove(bond);
                        bond.Parent = null;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void Atoms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Atom atom in e.NewItems)
                    {
                        atom.Parent = this;
                        AllAtoms.Add(atom);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Atom atom in e.OldItems)
                    {
                        AllAtoms.Remove(atom);
                        atom.Parent = null;
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Atom atom in e.NewItems)
                    {
                        atom.Parent = this;
                        AllAtoms.Add(atom);
                    }
                    foreach (Atom atom in e.OldItems)
                    {
                        AllAtoms.Remove(atom);
                        atom.Parent = null;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        public Point Centroid
        {
            get
            {
                // ReSharper disable once ArrangeAccessorOwnerBody
                return new Point((BoundingBox.Right + BoundingBox.Left) / 2.0, (BoundingBox.Bottom + BoundingBox.Top) / 2.0);
            }
        }

        public List<Atom> ConvexHull
        {
            get
            {
                return Geometry<Atom>.GetHull(AtomsSortedForHull(), a => a.Position);
            }
        }

        public string Id { get; set; }

        /*makes extensive use of the Andrew montone algorithm for determining the convex
       hulls of molecules with or without side chains.
       Assumes all rings have been calculated first*/

        public IEnumerable<Atom> AtomsSortedForHull()
        {
            Debug.Assert(RingsCalculated);
            var atomList = from Atom a in this.Atoms
                           orderby a.Position.X ascending, a.Position.Y descending
                           select a;

            return atomList;
        }

        #region Helpers

        public double MeanBondLength
        {
            get
            {
                double result = Model.XamlBondLength;

                if (Bonds.Any())
                {
                    result = Bonds.Average(b => b.BondVector.Length);
                }

                return result;
            }
        }

        /// <summary>
        /// Moves all atoms of molecule by inverse of x and y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void RepositionAll(double x, double y)
        {
            var offsetVector = new Vector(-x, -y);

            foreach (Atom a in Atoms)
            {
                a.Position += offsetVector;
            }

            foreach (Molecule child in Molecules)
            {
                child.RepositionAll(x, y);
            }
        }

        public void MoveAllAtoms(double x, double y)
        {
            var offsetVector = new Vector(x, y);

            foreach (Atom a in Atoms)
            {
                a.Position += offsetVector;
            }

            foreach (Molecule child in Molecules)
            {
                child.MoveAllAtoms(x, y);
            }

            _boundingBox = Rect.Empty;
        }

        #endregion Helpers

        protected override void ResetCollections()
        {
            base.ResetCollections();
            Atoms = new ObservableCollection<Atom>();
            Atoms.CollectionChanged += Atoms_CollectionChanged;
            Bonds = new ObservableCollection<Bond>();
            Bonds.CollectionChanged += Bonds_CollectionChanged;
            Rings = new ObservableCollection<Ring>();
            Rings.CollectionChanged += Rings_CollectionChanged;
            ChemicalNames = new ObservableCollection<ChemicalName>();
            ChemicalNames.CollectionChanged += ChemicalNames_CollectionChanged;
            Formulas = new ObservableCollection<Formula>();
            Formulas.CollectionChanged += Formulas_CollectionChanged;
        }

        private void Formulas_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private void ChemicalNames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public void ReLabel(bool includeNames, ref int iMolcount, ref int iAtomCount, ref int iBondcount)
        {
            Id = $"m{++iMolcount}";
            foreach (Atom a in Atoms)
            {
                a.Id = $"a{++iAtomCount}";
            }

            foreach (Bond b in Bonds)
            {
                b.Id = $"b{++iBondcount}";
            }

            if (includeNames)
            {
                int formulaCount = 0;
                int nameCount = 0;
                string prefix = $"{Id}.f";

                foreach (Formula f in Formulas)
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

                foreach (Formula f in Formulas)
                {
                    if (string.IsNullOrEmpty(f.Id) || !f.Id.StartsWith(prefix))
                    {
                        f.Id = $"{prefix}{formulaCount++}";
                    }
                }

                prefix = $"{Id}.n";

                foreach (ChemicalName n in ChemicalNames)
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

                foreach (ChemicalName n in ChemicalNames)
                {
                    if (string.IsNullOrEmpty(n.Id) || !n.Id.StartsWith(prefix))
                    {
                        n.Id = $"{prefix}{nameCount++}";
                    }
                }
            }

            if (Molecules.Any())
            {
                foreach (var mol in Molecules)
                {
                    mol.ReLabel(includeNames, ref iMolcount, ref iAtomCount, ref iBondcount);
                }
            }
        }

        public Molecule Clone()
        {
            Molecule clone = new Molecule();
            clone.Id = Id;

            Dictionary<string, Atom> clonedAtoms = new Dictionary<string, Atom>();

            foreach (var atom in Atoms)
            {
                Atom a = new Atom();

                // Add properties which would have been serialized to CML
                a.Id = atom.Id;

                a.Position = atom.Position;
                a.Element = atom.Element;

                a.IsotopeNumber = atom.IsotopeNumber;
                a.FormalCharge = atom.FormalCharge;

                // Save for joining up bonds
                clonedAtoms[atom.Id] = a;

                // Add to clone
                clone.Atoms.Add(a);
            }

            foreach (Bond bond in Bonds)
            {
                Bond b = new Bond();

                // Add properties which would have been serialized to CML
                b.Id = bond.Id;
                b.StartAtom = clonedAtoms[bond.StartAtom.Id];
                b.EndAtom = clonedAtoms[bond.EndAtom.Id];

                b.Order = bond.Order;
                b.Stereo = bond.Stereo;
                b.ExplicitPlacement = bond.ExplicitPlacement;

                // Add to clone
                clone.Bonds.Add(b);
            }

            foreach (ChemicalName cn in ChemicalNames)
            {
                ChemicalName n = new ChemicalName();

                // Add properties which would have been serialized to CML
                n.Id = cn.Id;
                n.DictRef = cn.DictRef;
                n.Name = cn.Name;

                // Add to clone
                clone.ChemicalNames.Add(n);
            }

            foreach (Formula f in Formulas)
            {
                Formula ff = new Formula();

                // Add properties which would have been serialized to CML
                ff.Id = f.Id;
                ff.Convention = f.Convention;
                ff.Inline = f.Inline;

                // Add to clone
                clone.Formulas.Add(f);
            }

            foreach (var molecule in Molecules)
            {
                clone.Molecules.Add(molecule.Clone());
            }

            return clone;
        }

        public void Move(Transform lastOperation)
        {
        }

        public bool Overlaps(List<Point> placements, List<Atom> excludeAtoms = null)
        {
            var area = OverlapArea(placements);

            if (area.GetArea() >= 0.01)
            {
                return true;
            }
            else
            {
                var chainAtoms = Atoms.Where(a => !a.Rings.Any()).ToList();
                if (excludeAtoms != null)
                {
                    foreach (Atom excludeAtom in excludeAtoms)
                    {
                        if (chainAtoms.Contains(excludeAtom))
                        {
                            chainAtoms.Remove(excludeAtom);
                        }
                    }
                }
                var placementsArea = BasicGeometry.BuildPath(placements).Data;
                foreach (var chainAtom in chainAtoms)
                {
                    if (placementsArea.FillContains(chainAtom.Position, 0.01, ToleranceType.Relative))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private PathGeometry OverlapArea(List<Point> placements)
        {
            PathGeometry ringsGeo = null;
            foreach (Ring r in Rings)
            {
                Path ringHull = BasicGeometry.BuildPath(r.Traverse().Select(a => a.Position).ToList());
                if (ringsGeo == null)
                {
                    ringsGeo = ringHull.Data.GetOutlinedPathGeometry();
                }
                else
                {
                    var hull = ringHull.Data;
                    var hullGeo = hull.GetOutlinedPathGeometry();
                    ringsGeo = new CombinedGeometry(GeometryCombineMode.Union, ringsGeo, hullGeo).GetOutlinedPathGeometry();
                }
            }
            Path otherGeo = BasicGeometry.BuildPath(placements);

            var val1 = ringsGeo;
            if (val1 != null)
            {
                val1.FillRule = FillRule.EvenOdd;
            }
            var val2 = otherGeo.Data.GetOutlinedPathGeometry();
            if (val2 != null)
            {
                val2.FillRule = FillRule.EvenOdd;
            }

            var overlap = new CombinedGeometry(GeometryCombineMode.Intersect, val1, val2).GetOutlinedPathGeometry();
            //return (id == IntersectionDetail.FullyContains | id == IntersectionDetail.FullyInside |
            //        id == IntersectionDetail.Intersects);

            return overlap;
        }

        /// <summary>
        /// Joins another molecule into this one
        /// </summary>
        /// <param name="mol">Molecule to merge into this one</param>
        public void Merge(Molecule mol)
        {
            Debug.Assert(mol != this);
            Debug.Assert(mol != null);
            Parent?.Molecules.Remove(mol);
            foreach (Atom newAtom in
               mol.Atoms.ToArray())
            {
                mol.Atoms.Remove(newAtom);
                if (!Atoms.Contains(newAtom))
                {
                    Atoms.Add(newAtom);
                }
            }
            foreach (Bond newBond in mol.Bonds.ToArray())
            {
                mol.Bonds.Remove(newBond);
                if (!Bonds.Contains(newBond))
                {
                    Bonds.Add(newBond);
                }
            }
        }

        /// <summary>
        /// split a molecule into two
        /// assuming that the bond between a and b has already
        /// been deleted
        /// </summary>
        /// <param name="a">Atom from first molecule</param>
        /// <param name="b">Atom from second molecule</param>
        public void Split(Atom a, Atom b)
        {
            Debug.Assert(a.BondBetween(b) == null);

            b.Parent = null;
            Refresh();

            if (b.Parent == null)//if it's non-null after refresh, then it was part of a ring system
            {
                Molecule newmol = new Molecule();

                Parent.Molecules.Add(newmol);
                newmol.Refresh(b);

                foreach (Atom oldAtom in newmol.Atoms)
                {
                    Atoms.Remove(oldAtom);
                }
            }
        }
    }
}