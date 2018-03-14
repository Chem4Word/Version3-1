// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Chem4Word.Core.Helpers;

namespace Chem4Word.Model
{
    public static class FunctionalGroups
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType.Name;

        private static Dictionary<string, FunctionalGroup> _shortcutList;

        /// <summary>
        /// ShortcutList represent text as a user might type in a superatom,
        /// actual values control how they are rendered
        /// </summary>
        public static Dictionary<string, FunctionalGroup> ShortcutList {
            get {
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

            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(),"FunctionalGroupsV2.json");
            if (!string.IsNullOrEmpty(json))
            {
                ShortcutList = JsonConvert.DeserializeObject<Dictionary<string, FunctionalGroup>>(json);
            }
        }

        //public static void LoadDefaults()
        //{
        //    ShortcutList = new Dictionary<string, FunctionalGroup>
        //    {
        //        //all the R residues are set to arbitrary multiplicity just so they appear subscripted
        //        //their atomic weight is zero anyhow
        //        //multiple dictionary keys may refer to the same functional group
        //        //simply to allow synonyms
        //        //when displayed, numbers in the names are automatically subscripted

        //        //note that ACME will automatically render a group as inverted if appropriate
        //        //so that CH3 -> H3C
        //        ["R1"] =
        //        new FunctionalGroup("R1",
        //            components: new List<Group> { new Group("R", 1) }, atwt: 0.0d),
        //        ["R2"] =
        //        new FunctionalGroup("R2",
        //            components: new List<Group> { new Group("R", 2) }, atwt: 0.0d),
        //        ["R3"] =
        //        new FunctionalGroup("R3",
        //            components: new List<Group> { new Group("R", 3) }, atwt: 0.0d),
        //        ["R4"] =
        //        new FunctionalGroup("R4",
        //            components: new List<Group> { new Group("R", 4) }, atwt: 0.0d),

        //        //generic halogen
        //        ["X"] = new FunctionalGroup("X", atwt: 0.0d),
        //        //typical shortcuts
        //        ["CH2"] =
        //        new FunctionalGroup("CH2",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 1),
        //                new Group("H", 2)
        //            }
        //        ),
        //        ["OH"] = new FunctionalGroup("OH",
        //            components: new List<Group>()
        //            {
        //                new Group("O", 1),
        //                new Group("H", 1)
        //            }
        //        ),
        //        ["CH3"] =
        //        new FunctionalGroup("CH3", flippable: true,
        //            components: new List<Group>
        //            {
        //                new Group("C", 1),
        //                new Group("H", 3)
        //            }),
        //        ["C2H5"] =
        //        new FunctionalGroup("C2H5",
        //            components: new List<Group>
        //            {
        //                new Group("C", 2),
        //                new Group("H", 5)
        //            }),
        //        ["Me"] = new FunctionalGroup("Me",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 1),
        //                new Group("H", 3)
        //            }, showAsSymbol: true),
        //        ["Et"] = new FunctionalGroup("Et",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 2),
        //                new Group("H", 5)
        //            }, showAsSymbol: true),
        //        ["Pr"] = new FunctionalGroup("Pr",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 3),
        //                new Group("H", 7)
        //            }, showAsSymbol: true),
        //        ["i-Pr"] = new FunctionalGroup("i-Pr",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 3),
        //                new Group("H", 7)
        //            }, showAsSymbol: true),
        //        ["iPr"] = new FunctionalGroup("i-Pr",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 3),
        //                new Group("H", 7)
        //            }, showAsSymbol: true),
        //        ["n-Bu"] = new FunctionalGroup("n-Bu",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 4),
        //                new Group("H", 9)
        //            }, showAsSymbol: true),
        //        ["nBu"] = new FunctionalGroup("n-Bu",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 4),
        //                new Group("H", 9)
        //            }, showAsSymbol: true),
        //        ["t-Bu"] = new FunctionalGroup("t-Bu",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 4),
        //                new Group("H", 9)
        //            }, showAsSymbol: true),
        //        ["tBu"] = new FunctionalGroup("t-Bu",
        //            components: new List<Group>()
        //            {
        //                new Group("C", 4),
        //                new Group("H", 9)
        //            }, showAsSymbol: true),
        //        ["Ph"] =
        //        new FunctionalGroup("Ph", components: new List<Group>()
        //        {
        //            new Group("C", 6),
        //            new Group("H", 5)
        //        }, showAsSymbol: true),
        //        ["CF3"] =
        //        new FunctionalGroup("CF3", flippable: true, components: new List<Group>()
        //        {
        //            new Group("C", 1),
        //            new Group("F", 3)
        //        }),
        //        ["CCl3"] =
        //        new FunctionalGroup("CCl3", flippable: true, components: new List<Group>()
        //        {
        //            new Group("C", 1),
        //            new Group("Cl", 3)
        //        }),
        //        ["C2F5"] =
        //        new FunctionalGroup("C2F5", components: new List<Group>()
        //        {
        //            new Group("C", 2),
        //            new Group("F", 5)
        //        }),
        //        ["TMS"] =
        //        new FunctionalGroup("TMS", components: new List<Group>()
        //        {
        //            new Group("C", 3),
        //            new Group("Si", 1),
        //            new Group("H", 9)
        //        }, showAsSymbol: true),
        //        ["COOH"] =
        //        new FunctionalGroup("CO2H", flippable: true, components: new List<Group>()
        //        {
        //            new Group("C", 1),
        //            new Group("O", 1),
        //            new Group("O", 1),
        //            new Group("H", 1)
        //        }),
        //        ["CO2H"] =
        //        new FunctionalGroup("COOH", components: new List<Group>()
        //        {
        //            new Group("C", 1),
        //            new Group("O", 2),
        //            new Group("H", 1)
        //        }),
        //        ["NO2"] =
        //        new FunctionalGroup("NO2", flippable: true, components: new List<Group>()
        //        {
        //            new Group("N", 1),
        //            new Group("O", 2),
        //        }),
        //        ["NH2"] =
        //        new FunctionalGroup("NH2", flippable: true, components: new List<Group>()
        //            {
        //                new Group("N", 1),
        //                new Group("H", 2),
        //            }
        //        )
        //    };
        //    //now do the more complex components : we need to add these in sequentially
        //    ShortcutList["CH2OH"] =
        //        new FunctionalGroup("CH2OH", flippable: true, components: new List<Group>()
        //        {
        //            new Group("CH2", 1),
        //            new Group("OH", 1)
        //        });
        //    ShortcutList["CH2CH2OH"] =
        //        new FunctionalGroup("CH2CH2OH", flippable: true, components: new List<Group>()
        //        {
        //            new Group("CH2", 1),
        //            new Group("CH2", 1),
        //            new Group("OH", 1)
        //        });
        //    ShortcutList["Bz"] =
        //        new FunctionalGroup("Bz", flippable: true, showAsSymbol: true, components: new List<Group>()
        //        {
        //            new Group("Ph", 1),
        //            new Group("CH2", 1)
        //        });
        //}

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
    }
}