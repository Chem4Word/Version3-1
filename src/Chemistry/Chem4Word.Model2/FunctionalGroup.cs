// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Chem4Word.Model2
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FunctionalGroup : ElementBase
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private static Dictionary<string, FunctionalGroup> _shortcutList;

        /// <summary>
        /// ShortcutList represent text as a user might type in a superatom,
        /// actual values control how they are rendered
        /// </summary>
        public static Dictionary<string, FunctionalGroup> ShortcutList
        {
            get
            {
                if (_shortcutList == null)
                {
                    LoadFromResource();
                }
                return _shortcutList;
            }
            private set { _shortcutList = value; }
        }

        public static bool TryParse(string desc, out FunctionalGroup fg)
        {
            try
            {
                if (ShortcutList.ContainsKey(desc))
                {
                    fg = ShortcutList[desc];
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

        private static void LoadFromResource()
        {
            ShortcutList = new Dictionary<string, FunctionalGroup>();

            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "FunctionalGroups.json");
            if (!string.IsNullOrEmpty(json))
            {
                ShortcutList = JsonConvert.DeserializeObject<Dictionary<string, FunctionalGroup>>(json);
            }
        }

        public override string Colour => "#000000";

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
        public string Symbol { get; set; }
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

            if (reverse)
            {
                for (int i = Components.Count - 1; i >= 0; i--)
                {
                    Append(Components[i]);
                }
            }
            else
            {
                foreach (var component in Components)
                {
                    Append(component);
                }
            }

            return result;

            // Local Function
            void Append(Group component)
            {
                // Expand FG
                FunctionalGroup fg;
                FunctionalGroup.TryParse(component.Component, out fg);
                if (fg != null && !fg.ShowAsSymbol)
                {
                    // This is a nested FG
                    result += fg.Expand(reverse);
                }
                else
                {
                    // This is an Element or Simple FG
                    result += $"{component.Component}";
                    if (component.Count > 1)
                    {
                        result += $"{component.Count}";
                    }
                }
            }
        }
    }
}