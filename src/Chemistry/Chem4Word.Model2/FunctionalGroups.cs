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
    public class FunctionalGroups
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private static Dictionary<string, FunctionalGroup> _shortcutList;

        /// <summary>
        /// Gets the Dictionary key for a given FunctionalGroup
        /// </summary>
        /// <param name="functionalGroup"></param>
        /// <returns></returns>
        public static string GetKey(FunctionalGroup functionalGroup)
        {
            string key = "";

            foreach (var kvp in Globals.FunctionalGroupsDictionary)
            {
                if (kvp.Value.Equals(functionalGroup))
                {
                    key = kvp.Key;
                    break;
                }
            }

            return key;
        }

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
            //private set { _shortcutList = value; }
        }

        private static void LoadFromResource()
        {
            _shortcutList = new Dictionary<string, FunctionalGroup>();

            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "FunctionalGroups.json");
            if (!string.IsNullOrEmpty(json))
            {
                _shortcutList = JsonConvert.DeserializeObject<Dictionary<string, FunctionalGroup>>(json);
            }
        }

        //list of valid shortcuts for testing input
        public static string ValidShortCuts => "(" + _shortcutList.Select(e => e.Key).Aggregate((start, next) => start + "|" + next) + ")";

        //list of valid elements (followed by subscripts) for testing input
        public static Regex NameParser => new Regex($"^(?<element>{Globals.PeriodicTable.ValidElements}+[0-9]*)+\\s*$");

        //checks to see whether a typed in expression matches a given shortcut
        public static bool IsValid(string expr)
        {
            return NameParser.IsMatch(expr) || IsFunctionalGroup(expr);
        }

        public static bool IsFunctionalGroup(string expr)
        {
            return ValidShortCuts.Contains($"|{expr}|");
        }
    }
}