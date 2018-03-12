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

        /// <summary>
        /// ShortcutList represent text as a user might type in a superatom,
        /// actual values control how they are rendered
        /// </summary>
        public static Dictionary<string, FunctionalGroup> ShortcutList { get; private set; }

        public static bool TryParse(string desc, out FunctionalGroup fg)
        {
            try
            {
                fg = GetByName[desc];
                return true;
            }
            catch (Exception)
            {
                fg = null;
                return false;
            }
        }

        public static SQLiteConnection DatabaseConnection
        {
            get
            {
                // ToDo: Figure out How to get the Folder and File below :-
                string datbasepath = Path.Combine(@"C:\ProgramData\Chem4Word.V3", Constants.FunctionalGroupDatbaseFileName);

                if (!File.Exists(datbasepath))
                {
                    ResourceHelper.WriteResource(Assembly.GetExecutingAssembly(), Constants.FunctionalGroupDatbaseFileName, datbasepath);
                }

                // Source https://www.connectionstrings.com/sqlite/
                var conn = new SQLiteConnection($"Data Source={datbasepath};Synchronous=Full");
                return conn.OpenAndReturn();
            }
        }

        public static void LoadFromJsonV2()
        {
            ShortcutList = new Dictionary<string, FunctionalGroup>();

            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(),"FunctionalGroupsV2.json");
            if (!string.IsNullOrEmpty(json))
            {
                ShortcutList = JsonConvert.DeserializeObject<Dictionary<string, FunctionalGroup>>(json);
            }
        }

        public static void LoadFromDatabsae()
        {
            ShortcutList = new Dictionary<string, FunctionalGroup>();

            SQLiteDataReader names = GetAllGroups();
            while (names.Read())
            {
                string fgName = names["Name"] as string;
                FunctionalGroup fg = new FunctionalGroup(fgName);
                bool flippable = (bool)names["Flippable"];
                bool showAsSymbol = (bool)names["ShowAsSymbol"];
                fg.Flippable = flippable;
                fg.ShowAsSymbol = showAsSymbol;
                fg.Components = new List<Group>();

                SQLiteDataReader components = GetGroupDetails(fgName);
                while (components.Read())
                {
                    string compName = components["Name"].ToString();
                    int count = int.Parse(components["Count"].ToString());
                    fg.Components.Add(new Group(compName, count));
                }
                components.Close();
                components.Dispose();

                ShortcutList.Add(fgName, fg);
                Debug.WriteLine(fgName);
            }

            names.Close();
            names.Dispose();
        }

        private static SQLiteDataReader GetGroupDetails(string name)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Select c.Type, c.Name, gc.Count");
                sb.AppendLine("From Groups g");
                sb.AppendLine("Inner Join GroupComponents gc ON gc.GroupId = g.Id");
                sb.AppendLine("Inner Join Components c ON gc.ComponentId = c.Id");
                // Convert to Parameterised Query later on
                sb.AppendLine($"Where g.Name = '{name}'");
                sb.AppendLine("Order By gc.Ordered");

                SQLiteConnection conn = DatabaseConnection;
                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
                //new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex).ShowDialog();
                return null;
            }
        }

        private static SQLiteDataReader GetAllGroups()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Select g.Name, Max(c.Type) AS MaxType, g.Flippable, g.ShowAsSymbol");
                sb.AppendLine("From Groups g");
                sb.AppendLine("Inner Join GroupComponents gc ON gc.GroupId = g.Id");
                sb.AppendLine("Inner Join Components c ON gc.ComponentId = c.Id");
                sb.AppendLine("Group By g.Id, g.Name");
                sb.AppendLine("Order By Max(c.Type),g.Id");

                SQLiteConnection conn = DatabaseConnection;
                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
                //new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex).ShowDialog();
                return null;
            }
        }

        public static void LoadFromFile(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            StreamReader tr = new StreamReader(fs);
            var fgJSON = tr.ReadToEnd();

            Load(fgJSON);
        }

        public static void Load(string fgJson)
        {
            ShortcutList = new Dictionary<string, FunctionalGroup>();
            var gd = JObject.Parse(fgJson);
            //var groups = JsonConvert.DeserializeObject<List<JObject>>(fgJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var groups = gd.SelectToken("Groups");
            ShortcutList.Clear();
            foreach (JObject jObject in groups)
            {
                var fg = new FunctionalGroup(jObject);
                ShortcutList[fg.Symbol] = fg;
            }
        }

        public static Dictionary<string, FunctionalGroup> GetByName
        {
            get
            {
                return ShortcutList;
            }
        }

        public static string GetJSON()
        {
            var result = JsonConvert.SerializeObject(ShortcutList.Values.ToList());
            return result;
        }

        public static void LoadDefaults()
        {
            ShortcutList = new Dictionary<string, FunctionalGroup>
            {
                //all the R residues are set to arbitrary multiplicity just so they appear subscripted
                //their atomic weight is zero anyhow
                //multiple dictionary keys may refer to the same functional group
                //simply to allow synonyms
                //when displayed, numbers in the names are automatically subscripted

                //note that ACME will automatically render a group as inverted if appropriate
                //so that CH3 -> H3C
                ["R1"] =
                new FunctionalGroup("R1",
                    components: new List<Group> { new Group("R", 1) }, atwt: 0.0d),
                ["R2"] =
                new FunctionalGroup("R2",
                    components: new List<Group> { new Group("R", 2) }, atwt: 0.0d),
                ["R3"] =
                new FunctionalGroup("R3",
                    components: new List<Group> { new Group("R", 3) }, atwt: 0.0d),
                ["R4"] =
                new FunctionalGroup("R4",
                    components: new List<Group> { new Group("R", 4) }, atwt: 0.0d),

                //generic halogen
                ["X"] = new FunctionalGroup("X", atwt: 0.0d),
                //typical shortcuts
                ["CH2"] =
                new FunctionalGroup("CH2",
                    components: new List<Group>()
                    {
                        new Group("C", 1),
                        new Group("H", 2)
                    }
                ),
                ["OH"] = new FunctionalGroup("OH",
                    components: new List<Group>()
                    {
                        new Group("O", 1),
                        new Group("H", 1)
                    }
                ),
                ["CH3"] =
                new FunctionalGroup("CH3", flippable: true,
                    components: new List<Group>
                    {
                        new Group("C", 1),
                        new Group("H", 3)
                    }),
                ["C2H5"] =
                new FunctionalGroup("C2H5",
                    components: new List<Group>
                    {
                        new Group("C", 2),
                        new Group("H", 5)
                    }),
                ["Me"] = new FunctionalGroup("Me",
                    components: new List<Group>()
                    {
                        new Group("C", 1),
                        new Group("H", 3)
                    }, showAsSymbol: true),
                ["Et"] = new FunctionalGroup("Et",
                    components: new List<Group>()
                    {
                        new Group("C", 2),
                        new Group("H", 5)
                    }, showAsSymbol: true),
                ["Pr"] = new FunctionalGroup("Pr",
                    components: new List<Group>()
                    {
                        new Group("C", 3),
                        new Group("H", 7)
                    }, showAsSymbol: true),
                ["i-Pr"] = new FunctionalGroup("i-Pr",
                    components: new List<Group>()
                    {
                        new Group("C", 3),
                        new Group("H", 7)
                    }, showAsSymbol: true),
                ["iPr"] = new FunctionalGroup("i-Pr",
                    components: new List<Group>()
                    {
                        new Group("C", 3),
                        new Group("H", 7)
                    }, showAsSymbol: true),
                ["n-Bu"] = new FunctionalGroup("n-Bu",
                    components: new List<Group>()
                    {
                        new Group("C", 4),
                        new Group("H", 9)
                    }, showAsSymbol: true),
                ["nBu"] = new FunctionalGroup("n-Bu",
                    components: new List<Group>()
                    {
                        new Group("C", 4),
                        new Group("H", 9)
                    }, showAsSymbol: true),
                ["t-Bu"] = new FunctionalGroup("t-Bu",
                    components: new List<Group>()
                    {
                        new Group("C", 4),
                        new Group("H", 9)
                    }, showAsSymbol: true),
                ["tBu"] = new FunctionalGroup("t-Bu",
                    components: new List<Group>()
                    {
                        new Group("C", 4),
                        new Group("H", 9)
                    }, showAsSymbol: true),
                ["Ph"] =
                new FunctionalGroup("Ph", components: new List<Group>()
                {
                    new Group("C", 6),
                    new Group("H", 5)
                }, showAsSymbol: true),
                ["CF3"] =
                new FunctionalGroup("CF3", flippable: true, components: new List<Group>()
                {
                    new Group("C", 1),
                    new Group("F", 3)
                }),
                ["CCl3"] =
                new FunctionalGroup("CCl3", flippable: true, components: new List<Group>()
                {
                    new Group("C", 1),
                    new Group("Cl", 3)
                }),
                ["C2F5"] =
                new FunctionalGroup("C2F5", components: new List<Group>()
                {
                    new Group("C", 2),
                    new Group("F", 5)
                }),
                ["TMS"] =
                new FunctionalGroup("TMS", components: new List<Group>()
                {
                    new Group("C", 3),
                    new Group("Si", 1),
                    new Group("H", 9)
                }, showAsSymbol: true),
                ["COOH"] =
                new FunctionalGroup("CO2H", flippable: true, components: new List<Group>()
                {
                    new Group("C", 1),
                    new Group("O", 1),
                    new Group("O", 1),
                    new Group("H", 1)
                }),
                ["CO2H"] =
                new FunctionalGroup("COOH", components: new List<Group>()
                {
                    new Group("C", 1),
                    new Group("O", 2),
                    new Group("H", 1)
                }),
                ["NO2"] =
                new FunctionalGroup("NO2", flippable: true, components: new List<Group>()
                {
                    new Group("N", 1),
                    new Group("O", 2),
                }),
                ["NH2"] =
                new FunctionalGroup("NH2", flippable: true, components: new List<Group>()
                    {
                        new Group("N", 1),
                        new Group("H", 2),
                    }
                )
            };
            //now do the more complex components : we need to add these in sequentially
            ShortcutList["CH2OH"] =
                new FunctionalGroup("CH2OH", flippable: true, components: new List<Group>()
                {
                    new Group("CH2", 1),
                    new Group("OH", 1)
                });
            ShortcutList["CH2CH2OH"] =
                new FunctionalGroup("CH2CH2OH", flippable: true, components: new List<Group>()
                {
                    new Group("CH2", 1),
                    new Group("CH2", 1),
                    new Group("OH", 1)
                });
            ShortcutList["Bz"] =
                new FunctionalGroup("Bz", flippable: true, showAsSymbol: true, components: new List<Group>()
                {
                    new Group("Ph", 1),
                    new Group("CH2", 1)
                });
        }

        //list of valid shortcuts for testing input
        public static string ValidShortCuts => "^(" +
            GetByName.Select(e => e.Key).Aggregate((start, next) => start + "|" + next) + ")$";

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