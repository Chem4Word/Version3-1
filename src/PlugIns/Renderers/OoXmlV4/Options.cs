// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word.Renderer.OoXmlV4
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Options
    {
        [JsonProperty]
        public bool ShowHydrogens { get; set; }

        [JsonProperty]
        public bool ColouredAtoms { get; set; }

        [JsonProperty]
        public bool ShowMoleculeGroups { get; set; }

        [JsonProperty]
        public bool ShowMoleculeLabels { get; set; }

        // Not editable here, kept in sync by file system watcher
        [JsonProperty]
        public int BondLength { get; set; }

        // Debugging
        [JsonProperty]
        public bool ClipLines { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowCharacterBoundingBoxes { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowMoleculeBoundingBoxes { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowAtomPositions { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowHulls { get; set; }

        // Debugging
        [JsonProperty]
        public bool ShowRingCentres { get; set; }

        public Options()
        {
            RestoreDefaults();
        }

        public Options Clone()
        {
            Options clone = new Options();

            // Main User Options
            clone.ColouredAtoms = ColouredAtoms;
            clone.ShowHydrogens = ShowHydrogens;
            clone.ShowMoleculeGroups = ShowMoleculeGroups;
            clone.ShowMoleculeLabels = ShowMoleculeLabels;

            // Hidden
            clone.BondLength = BondLength;

            // Debugging Options
            clone.ClipLines = ClipLines;
            clone.ShowCharacterBoundingBoxes = ShowCharacterBoundingBoxes;
            clone.ShowMoleculeBoundingBoxes = ShowMoleculeBoundingBoxes;
            clone.ShowRingCentres = ShowRingCentres;
            clone.ShowAtomPositions = ShowAtomPositions;
            clone.ShowHulls = ShowHulls;

            return clone;
        }

        public void RestoreDefaults()
        {
            ShowHydrogens = true;
            ColouredAtoms = true;
            ShowMoleculeGroups = true;
            ShowMoleculeLabels = false;

            BondLength = (int)Constants.StandardBondLength;

            // Debugging Options
            ClipLines = true;
            ShowCharacterBoundingBoxes = false;
            ShowMoleculeBoundingBoxes = false;
            ShowRingCentres = false;
            ShowAtomPositions = false;
            ShowHulls = false;
        }
    }
}