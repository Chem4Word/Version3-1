﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;
using static Chem4Word.Model2.Helpers.CML;

namespace Chem4Word.Model2
{
    public class Atom : ChemistryBase, INotifyPropertyChanged
    {
        #region Fields

        private bool _explicitC;
        public readonly List<string> Messages = new List<string>();

        #endregion Fields

        #region Properties

        private ElementBase _element;

        public ElementBase Element
        {
            get { return _element; }
            set
            {
                _element = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SymbolText));
                OnPropertyChanged(nameof(ImplicitHydrogenCount));
            }
        }

        public IEnumerable<Atom> Neighbours
        {
            get { return Parent.GetAtomNeighbours(this); }
        }

        public HashSet<Atom> NeighbourSet => new HashSet<Atom>(Neighbours);

        /// <summary>
        /// The rings this atom belongs to
        /// </summary>
        public List<Ring> Rings { get; private set; }

        public Molecule Parent { get; set; }

        public IEnumerable<Bond> Bonds
        {
            get
            {
                if (Parent != null)
                {
                    return Parent.GetBonds(InternalId);
                }
                else
                    return null;
            }
        }

        public int Degree => Bonds.Count();

        //public List<Atom> UnprocessedNeighbours(Predicate<Atom> unprocessedTest)
        //{
        //    return Neighbours.Where(a => unprocessedTest(a)).ToList();
        //}

        private string _id;

        public string Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    var oldID = _id;
                    _id = value;
                    //Parent?.UpdateBondRefs(oldID, value); //could be updating while orphaned
                }
            }
        }

        public string Path
        {
            get
            {
                if (Parent == null)
                {
                    return Id;
                }
                else
                {
                    return Parent.Path + "/" + Id;
                }
            }
        }

        public Point Position
        {
            get { return _position; }
            set
            {
                _position = value;
                OnPropertyChanged();
            }
        }

        public bool ShowSymbol
        {
            get
            {
                switch (Element)
                {
                    case Element e when (e == Globals.PeriodicTable.C & IsotopeNumber != null | (FormalCharge ?? 0) != 0):
                        return true;

                    case Element e when (e == Globals.PeriodicTable.C):
                        return _explicitC;

                    case FunctionalGroup fg:
                        return true;

                    default:
                        return false;
                }
            }
            set
            {
                switch (Element)
                {
                    case Element e when (e == Globals.PeriodicTable.C):
                        _explicitC = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(SymbolText));
                        break;

                    default:
                        Debugger.Break();
                        throw new ArgumentOutOfRangeException("ShowSymbol", "Cannot set explicit display on a non-carbon atom.");
                }
            }
        }

        public string SymbolText
        {
            get
            {
                if (Element != null)
                {
                    if (Element is FunctionalGroup)
                    {
                        return ((FunctionalGroup)Element).Symbol;
                    }
                    else
                    {
                        if (Element.Symbol.Equals("C"))
                        {
                            if (ShowSymbol)
                            {
                                return "C";
                            }

                            if (Degree <= 1)
                            {
                                return "C";
                            }

                            if (Degree == 2)
                            {
                                var bonds = Bonds.ToArray();
                                // This code is triggered when adding the first Atom to a bond
                                //  at this point one of the atoms is undefined
                                Atom a1 = bonds[0].OtherAtom(this);
                                Atom a2 = bonds[1].OtherAtom(this);

                                if (a1 != null && a2 != null)
                                {
                                    double angle1 = Vector.AngleBetween(-(this.Position - a1.Position), this.Position - a2.Position);

                                    if (Math.Abs(angle1) < 8)
                                    {
                                        return "C";
                                    }
                                }
                                else
                                {
                                    //Debugger.Break();
                                }
                            }

                            return "";
                        }
                        else
                        {
                            return Element.Symbol;
                        }
                    }
                }
                else
                {
                    return "";
                }
            }
        }

        public object Tag { get; set; }

        private int? _isotopeNumber;

        public int? IsotopeNumber
        {
            get
            {
                return _isotopeNumber;
            }
            set
            {
                _isotopeNumber = value;
                OnPropertyChanged();
            }
        }

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

        private int? _formalCharge;

        public int? FormalCharge
        {
            get { return _formalCharge; }
            set
            {
                _formalCharge = value;
                //Attributed call knows who we are, no need to pass "FormalCharge" as an argument
                OnPropertyChanged();
            }
        }

        private bool _doubletRadical;
        private Point _position;

        public int ImplicitHydrogenCount
        {
            get
            {
                // Return -1 if we don't need to do anything
                int iHydrogenCount = -1;

                // Applies to "B,C,N,O,F,Si,P,S,Cl,As,Se,Br,Te,I,At";
                string appliesTo = Globals.PeriodicTable.ImplicitHydrogenTargets;

                if (appliesTo.Contains(Element.Symbol))
                {
                    int iBondCount = (int)Math.Floor(this.BondOrders);
                    int iCharge = 0;
                    iCharge = FormalCharge ?? 0;
                    int iValence = PeriodicTable.GetValence((Element as Element), iBondCount);
                    int iDiff = iValence - iBondCount;
                    if (iCharge > 0)
                    {
                        int iVdiff = 4 - iValence;
                        if (iCharge <= iVdiff)
                        {
                            iDiff += iCharge;
                        }
                        else
                        {
                            iDiff = 4 - iBondCount - iCharge + iVdiff;
                        }
                    }
                    else
                    {
                        iDiff += iCharge;
                    }
                    // Ensure iHydrogenCount returned is never -ve
                    if (iDiff >= 0)
                    {
                        iHydrogenCount = iDiff;
                    }
                    else
                    {
                        iHydrogenCount = 0;
                    }
                }
                return iHydrogenCount;
            }
        }

        public double BondOrders
        {
            get
            {
                double order = 0d;
                if (Parent != null)
                {
                    foreach (Bond bond in Bonds)
                    {
                        order += bond.OrderValue ?? 0d;
                    }
                }

                return order;
            }
        }

        public bool DoubletRadical
        {
            get { return _doubletRadical; }
            set
            {
                _doubletRadical = value;
                //Attributed call knows who we are, no need to pass "DoubletRadical" as an argument
                OnPropertyChanged();
            }
        }

        public bool IsUnsaturated => BondOrders > Degree;

        //drawing related properties
        public Vector BalancingVector
        {
            get
            {
                Vector vsumVector = BasicGeometry.ScreenNorth;

                if (Bonds.Any())
                {
                    double sumOfLengths = 0;
                    foreach (var bond in Bonds)
                    {
                        Vector v = bond.OtherAtom(this).Position - this.Position;
                        sumOfLengths += v.Length;
                        vsumVector += v;
                    }

                    // Set tiny amount as 10% of average bond length
                    double tinyAmount = sumOfLengths / Bonds.Count() * 0.1;
                    double xy = vsumVector.Length;

                    // Is resultant vector is big enough for us to use?
                    if (xy >= tinyAmount)
                    {
                        // Get vector in opposite direction
                        vsumVector = -vsumVector;
                        vsumVector.Normalize();
                    }
                    else
                    {
                        // Get vector of first bond
                        Vector vector = Bonds.First().OtherAtom(this).Position - Position;
                        if (Bonds.Count() == 2)
                        {
                            // Get vector at right angles
                            vsumVector = vector.Perpendicular();
                        }
                        else
                        {
                            // Get vector in opposite direction
                            vsumVector = -vector;
                        }
                        vsumVector.Normalize();
                    }
                }

                //Debug.WriteLine($"Atom {Id} Resultant Balancing Vector Angle is {Vector.AngleBetween(BasicGeometry.ScreenNorth, vsumVector)}");
                return vsumVector;
            }
        }

        #endregion Properties

        #region Constructors

        public Atom()
        {
            Id = Guid.NewGuid().ToString("D");
            _internalId = Id;
        }

        /// <summary>
        /// The internal ID is what is used to tie atoms and bonds together
        /// </summary>
        private readonly string _internalId;

        internal string InternalId => _internalId;

        public Atom(XElement atomElement) : this()
        {
            Messages = new List<string>();
            string message = "";
            string atomLabel = atomElement.Attribute("id")?.Value;

            Point p = GetPosn(atomElement, out message);
            if (!string.IsNullOrEmpty(message))
            {
                Messages.Add(message);
            }

            Id = atomLabel;
            Position = p;

            ElementBase e = GetChemicalElement(atomElement, out message);
            if (!string.IsNullOrEmpty(message))
            {
                Messages.Add(message);
            }

            if (e != null)
            {
                Element = e;
                FormalCharge = GetFormalCharge(atomElement);
                IsotopeNumber = GetIsotopeNumber(atomElement);
            }
        }

        #endregion Constructors

        #region Methods

        public List<Atom> NeighboursExcept(Atom toIgnore)
        {
            return Neighbours.Where(a => a != toIgnore).ToList();
        }

        public List<Atom> NeighboursExcept(params Atom[] toIgnore)
        {
            return Neighbours.Where(a => !toIgnore.Contains(a)).ToList();
        }

        public Bond BondBetween(Atom atom)
        {
            foreach (var parentBond in Parent.Bonds)
            {
                if (parentBond.StartAtomID.Equals(InternalId) && parentBond.EndAtomID.Equals(atom.InternalId))
                {
                    return parentBond;
                }
                if (parentBond.EndAtomID.Equals(InternalId) && parentBond.StartAtomID.Equals(atom.InternalId))
                {
                    return parentBond;
                }
            }
            return null;
        }

        public Atom Clone()
        {
            return new Atom().CloneExcept(this, new[] { "Id" });
        }

        public CompassPoints GetDefaultHOrientation()
        {
            if (ImplicitHydrogenCount >= 1)
            {
                if (Bonds.Count() == 0)
                {
                    return CompassPoints.East;
                }
                else if (Bonds.Count() == 1)
                {
                    var angle = Vector.AngleBetween(BasicGeometry.ScreenNorth,
                        Bonds.First().OtherAtom(this).Position - Position);
                    int clockDirection = BasicGeometry.SnapToClock(angle);

                    if (clockDirection == 0 | clockDirection == 6)
                    {
                        return CompassPoints.East;
                    }
                    else if (clockDirection >= 6 & clockDirection <= 11)
                    {
                        return CompassPoints.East;
                    }
                    else
                    {
                        return CompassPoints.West;
                    }
                }
                else
                {
                    double baFromNorth = Vector.AngleBetween(BasicGeometry.ScreenNorth,
                        BalancingVector);

                    return BasicGeometry.SnapTo4NESW(baFromNorth);
                }
            }
            return CompassPoints.East;
        }

        //notification methods
        public void NotifyBondingChanged()
        {
            OnPropertyChanged(nameof(Degree));
            OnPropertyChanged(nameof(ImplicitHydrogenCount));
            OnPropertyChanged(nameof(BalancingVector));
            OnPropertyChanged(nameof(ShowSymbol));
            OnPropertyChanged(nameof(SymbolText));
        }

        #endregion Methods

        #region Overrides

        public override string ToString()
        {
            var symbol = Element != null ? Element.Symbol : "???";
            return $"Atom {Id} - {InternalId}: {symbol} @ {Position.X.ToString("0.000")}, {Position.Y.ToString("0.000")}";
        }

        #endregion Overrides

        #region Events

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #endregion Events
    }
}