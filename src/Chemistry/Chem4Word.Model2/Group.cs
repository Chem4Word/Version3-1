// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using Chem4Word.Model2.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Model2
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
        /// Calculated combined AtomicWeight
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
}