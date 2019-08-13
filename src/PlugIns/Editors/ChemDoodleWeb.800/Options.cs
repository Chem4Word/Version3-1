// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Editor.ChemDoodleWeb800
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Options
    {
        [JsonProperty]
        public bool ShowHydrogens { get; set; }

        [JsonProperty]
        public bool ColouredAtoms { get; set; }

        [JsonProperty]
        public bool ShowCarbons { get; set; }

        [JsonProperty]
        public int BondLength { get; set; }

        public Options()
        {
            RestoreDefaults();
        }

        public Options Clone()
        {
            Options clone = new Options();

            clone.ColouredAtoms = ColouredAtoms;
            clone.ShowHydrogens = ShowHydrogens;
            clone.ShowCarbons = ShowCarbons;

            clone.BondLength = BondLength;

            return clone;
        }

        public void RestoreDefaults()
        {
            ShowHydrogens = true;
            ColouredAtoms = true;
            ShowCarbons = false;

            BondLength = (int)Constants.StandardBondLength;
        }
    }
}