// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Chem4Word.Model2.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Model2
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FunctionalGroup : ElementBase
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private double _atomicWeight = 0d;

        public static bool TryParse(string desc, out ElementBase element)
        {
            if (TryParse(desc, out FunctionalGroup fg))
            {
                element = fg;
                return true;
            }
            else
            {
                if (Globals.PeriodicTable.HasElement(desc))
                {
                    element = (ElementBase)Globals.PeriodicTable[desc];
                    return true;
                }
            }

            element = null;
            return false;
        }

        public static bool TryParse(string desc, out FunctionalGroup fg)
        {
            try
            {
                if (Globals.FunctionalGroupsDictionary.ContainsKey(desc))
                {
                    fg = Globals.FunctionalGroupsDictionary[desc];
                    return true;
                }
                else
                {
                    fg = null;
                    return false;
                }
            }
            catch (Exception)
            {
                fg = null;
                return false;
            }
        }

        public override string Colour => Globals.PeriodicTable.C.Colour;

        public override double AtomicWeight
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
                            atwt += component.AtomicWeight * component.Count;
                        }
                    }
                    return atwt;
                }

                return _atomicWeight;
            }
            set { _atomicWeight = value; }
        }

        public Dictionary<string, int> FormulaParts
        {
            get
            {
                Dictionary<string, int> parts = new Dictionary<string, int>();

                foreach (var component in Components)
                {
                    var pp = component.FormulaParts;
                    foreach (var p in pp)
                    {
                        if (parts.ContainsKey(p.Key))
                        {
                            parts[p.Key] += p.Value * component.Count;
                        }
                        else
                        {
                            parts.Add(p.Key, p.Value * component.Count);
                        }
                    }
                }

                return parts;
            }
        }

        /// <summary>
        /// Determines whether the functional group can be flipped about the pivot
        /// </summary>
        [JsonProperty]
        public bool Flippable { get; set; }

        /// <summary>
        /// Symbol refers to the 'Ph', 'Bz' etc
        /// It is a unique key for the functional group
        /// Symbol can also be of the form CH3, CF3, C2H5 etc
        /// </summary>
        [JsonProperty]
        public override string Symbol { get; set; }

        [JsonProperty]
        public bool ShowAsSymbol { get; set; }

        /// <summary>
        /// Defines the constituents of the superatom
        /// The 'pivot' atom that bonds to the fragment appears FIRST in the list
        /// so CH3 can appear as H3C
        ///
        /// Ths property can be null, which means that the symbol gets rendered
        /// </summary>
        [JsonProperty]
        public List<Group> Components { get; set; }

        public string Expand(bool reverse = false)
        {
            string result = "";

            // Step 1; Collect a forward list of terms
            List<string> expanded = new List<string>();

            if (ShowAsSymbol)
            {
                expanded.Add(Symbol);
            }
            else
            {
                foreach (var component in Components)
                {
                    expanded.Add(ExpandLocal(component));
                }
            }

            // Step 2; If reverse, reverse the array and set anchor term as last term
            int anchorTerm = 0;
            if (Flippable && reverse)
            {
                expanded.Reverse();
                anchorTerm = expanded.Count - 1;
            }

            // Step 3; Combine strings together to form final output, surrounding anchor "term" with []
            for (int i = 0; i < expanded.Count; i++)
            {
                if (i == anchorTerm)
                {
                    result += $"[{expanded[i]}]";
                }
                else
                {
                    result += expanded[i];
                }
            }

            return result;

            // Local Function for recursion
            string ExpandLocal(Group localComponent)
            {
                string localResult = "";

                ElementBase elementBase;
                var ok = AtomHelpers.TryParse(localComponent.Component, out elementBase);
                if (ok)
                {
                    if (elementBase is Element element)
                    {
                        localResult = element.Symbol;

                        if (localComponent.Count != 1)
                        {
                            localResult = $"{localResult}{localComponent.Count}";
                        }
                    }

                    if (elementBase is FunctionalGroup fg)
                    {
                        if (fg.ShowAsSymbol)
                        {
                            localResult = fg.Symbol;
                        }
                        else
                        {
                            if (reverse)
                            {
                                for (int i = fg.Components.Count - 1; i >= 0; i--)
                                {
                                    localResult += ExpandLocal(fg.Components[i]);
                                }
                            }
                            else
                            {
                                foreach (var fgc in fg.Components)
                                {
                                    localResult += ExpandLocal(fgc);
                                }
                            }
                        }

                        if (localComponent.Count != 1)
                        {
                            localResult = $"({localResult}){localComponent.Count}";
                        }
                    }
                }

                return localResult;
            }
        }
    }
}