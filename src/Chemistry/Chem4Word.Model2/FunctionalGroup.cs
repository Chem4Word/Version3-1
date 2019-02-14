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
    public class SubGroup
    {
        public string Component { get; set; }
        public int Count { get; set; }

        public ElementBase Resolve()
        {
            
            if(FunctionalGroup.TryParse(Component, out FunctionalGroup fg))
            {
                return fg;
            }
            else if (Globals.PeriodicTable.HasElement(Component))
            {
                return Globals.PeriodicTable.Elements[Component];
            }

            return null;
        }
    }
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
                fg = ShortcutList[desc];
                return true;
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

        //list of valid shortcuts for testing input
        public static string ValidShortCuts => "^(" +
                                               ShortcutList.Select(e => e.Key).Aggregate((start, next) => start + "|" + next) + ")$";

        //and the regex to use it
        public static Regex ShortcutParser => new Regex(ValidShortCuts);

        //list of valid elements (followed by subscripts) for testing input
        public static Regex NameParser => new Regex($"^(?<element>{Globals.PeriodicTable.ValidElements}+[0-9]*)+\\s*$");

        //checks to see whether a typed in expression matches a given shortcut
        public static bool IsValid(string expr)
        {
            return NameParser.IsMatch(expr) || ShortcutParser.IsMatch(expr);
        }

        public override string Colour => "#000000";

        [JsonProperty]
        public bool Flippable { get; set; }
        [JsonProperty]
        public string Symbol { get; set; }
        [JsonProperty]
        public bool ShowAsSymbol { get; set; }
        [JsonProperty]
        public List<SubGroup> Components { get; set; }
    }
}