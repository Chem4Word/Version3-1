// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace Chem4Word.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Group
    {
        [JsonProperty]
        public string Component { get; set; }

        [JsonProperty]
        public int Count { get; set; }

        public Group(string e, int c)
        {
            Component = e;
            Count = c;
        }

        /// <summary>
        /// Calculate combined AtomicWeight
        /// </summary>
        public double AtomicWeight
        {
            get
            {
                var pt = new PeriodicTable();
                if (pt.HasElement(Component))
                {
                    return ((Element)pt[Component]).AtomicWeight * Count;
                }
                else
                {
                    FunctionalGroup fg = FunctionalGroups.ShortcutList[Component];
                    if (fg != null)
                    {
                        return FunctionalGroups.ShortcutList[Component].AtomicWeight * Count;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }
    }


    [JsonObject(MemberSerialization.OptIn)]
    public class FunctionalGroup : ElementBase
    {
        private string _symbol = "";
        private double _atomicWeight = 0d;

        public FunctionalGroup()
        {
        }

        /// <summary>
        /// Generates a new functional group to use for a superatom
        /// </summary>
        /// <param name="symbol">Actual symbol used for the atom (mandatory)</param>
        /// <param name="components"> Key-Value list of components and how many</param>
        /// <param name="atwt">atomic weight of the atom</param>
        /// <param name="showAsSymbol">whether the element is rendered as its symbol or constructed from its components</param>
        public FunctionalGroup(string symbol,
            List<Group> components = null,
            double atwt = 0d, bool showAsSymbol = false, bool flippable = false)
        {
            Symbol = symbol;
            AtomicWeight = atwt;
            Components = components;
            this.ShowAsSymbol = showAsSymbol;
            Flippable = flippable;
        }

        [JsonProperty]
        public bool ShowAsSymbol { get; set; }

        public sealed override double AtomicWeight
        {
            get
            {
                if (_atomicWeight == 0d)
                {
                    double atwt = 0.0d;
                    if (Components != null)
                    {
                        //add up the atoms' atomic weights times their multiplicity
                        foreach (Group component in Components)
                        {
                            atwt += component.AtomicWeight;
                        }
                    }
                    return atwt;
                }

                return _atomicWeight;
            }
            set { _atomicWeight = value; }
        }

        /// <summary>
        /// Symbol refers to the 'Ph', 'Bz' etc
        /// It is a unique key for the functional group
        /// Symbol can also be of the form CH3, CF3, C2H5 etc
        /// </summary>
        [JsonProperty]
        public sealed override string Symbol
        {
            get
            {
                return _symbol;
            }

            set { _symbol = value; }
        }

        /// <summary>
        /// Defines the constituents of the superatom
        /// The 'pivot' atom that bonds to the fragment appears FIRST in the list
        /// so CH3 can appear as H3C
        ///
        /// Ths property can be null, which means that the symbol gets rendered
        /// </summary>
        [JsonProperty]
        public List<Group> Components { get; set; }

        /// <summary>
        /// Determines whether the functional group can be flipped about the pivot
        /// </summary>
        [JsonProperty]
        public bool Flippable { get; set; }
    }
}