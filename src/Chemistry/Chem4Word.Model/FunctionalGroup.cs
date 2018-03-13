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
    /// <summary>
    /// Identifies components of atoms in FGs
    /// </summary>
    ///
    ///

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
                    FunctionalGroup fg = FunctionalGroups.GetByName[Component];
                    if (fg != null)
                    {
                        return FunctionalGroups.GetByName[Component].AtomicWeight * Count;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Functional groups are serialised as JSON
    /// {
    /// "symbol":"CH2",
    /// "flippable":"false"
    /// "components":
    /// [
    /// {"element":"C"},
    /// {"element":"H", "count":2},
    /// ]
    /// }
    /// {
    /// "symbol":"CH2CH2OH",
    /// "flippable":"true"
    /// "components":
    /// [
    /// {"group":"CH2"},
    /// {"group":"CH2"},
    /// {"element":"O"},
    /// {"element":"H"},
    /// ]
    /// }
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class FunctionalGroup : ElementBase
    {
        public const string TagSymbol = "symbol";
        public const string TagComponents = "components";
        public const string TagFlippable = "flippable";
        public const string TagElement = "element";
        public const string TagCount = "count";
        public const string TagGroup = "group";
        public const string TagShowAsSymbol = "showassymbol";

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

        public FunctionalGroup(JObject groupAsJson)
        {
            var pt = new PeriodicTable();
            Symbol = groupAsJson[TagSymbol].ToString();
            Flippable = groupAsJson[TagFlippable].Value<bool?>() ?? false;
            ShowAsSymbol = groupAsJson[TagShowAsSymbol].Value<bool?>() ?? false;
            Components = new List<Group>();
            var complist = groupAsJson[TagComponents];

            foreach (JToken c in complist)
            {
                Group g;
                if (c.Value<string>(TagElement) != null)
                {
                    g = new Group(c.Value<string>(TagElement), c.Value<int?>("count") ?? 1);
                }
                else if (c.Value<string>(TagGroup) != null)
                {
                    g = new Group(c.Value<string>(TagGroup).ToString(), c.Value<int?>("count") ?? 1);
                }
                else
                {
                    throw new InvalidDataException("Element/group tag missing");
                }
                Components.Add(g);
            }
        }

        [JsonProperty]
        public bool ShowAsSymbol { get; set; }

        //[JsonProperty]
        public sealed override double AtomicWeight
        {
            get
            {
                if (_atomicWeight == 0d)
                {
                    double atwt = 0.0d;
                    if (Components != null)
                    {
                        //add up the atoms' atomicv weights times their multiplicity
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

        [JsonProperty]
        public override string Name { get; set; }

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