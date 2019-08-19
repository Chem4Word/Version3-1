// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.ACME
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Options
    {
        private int _bondLength;

        [JsonProperty]
        public int BondLength
        {
            get => _bondLength;
            set
            {
                _bondLength = value;
                Dirty = true;
            }
        }

        [JsonProperty]
        public bool ShowMoleculeGroups { get; set; }

        public string SettingsFile { get; set; }
        public bool Dirty { get; set; }

        public Options()
        {
            RestoreDefaults();
        }

        public Options Clone()
        {
            Options clone = new Options();

            clone.BondLength = BondLength;
            clone.ShowMoleculeGroups = ShowMoleculeGroups;

            clone.SettingsFile = SettingsFile;

            return clone;
        }

        public void RestoreDefaults()
        {
            BondLength = (int)Constants.StandardBondLength;
            ShowMoleculeGroups = true;
            Dirty = false;
        }
    }
}