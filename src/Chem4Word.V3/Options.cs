// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using Chem4Word.Core.Helpers;
using Newtonsoft.Json;

namespace Chem4Word
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Options
    {
        #region Telemetry

        [JsonProperty]
        public bool TelemetryEnabled { get; set; }

        #endregion Telemetry

        #region Automatic Updates

        //[JsonProperty]
        public bool AutoUpdateEnabled { get; set; }

        //[JsonProperty]
        public int AutoUpdateFrequency { get; set; }

        #endregion Automatic Updates

        #region Selected Plug Ins

        [JsonProperty]
        public string SelectedEditorPlugIn { get; set; }

        [JsonProperty]
        public string SelectedRendererPlugIn { get; set; }

        #endregion Selected Plug Ins

        #region General

        [JsonProperty]
        public int BondLength { get; set; }

        [JsonProperty]
        public bool RemoveExplicitHydrogensOnImportFromFile { get; set; }

        [JsonProperty]
        public bool RemoveExplicitHydrogensOnImportFromSearch { get; set; }

        [JsonProperty]
        public bool RemoveExplicitHydrogensOnImportFromLibrary { get; set; }

        [JsonProperty]
        public bool SetBondLengthOnImportFromFile { get; set; }

        [JsonProperty]
        public bool SetBondLengthOnImportFromSearch { get; set; }

        [JsonProperty]
        public bool SetBondLengthOnImportFromLibrary { get; set; }

        #endregion General

        // Not Saved
        public Point WordTopLeft { get; set; }

        public Options()
        {
            RestoreDefaults();
        }

        public Options Clone()
        {
            Options clone = new Options();

            clone.TelemetryEnabled = TelemetryEnabled;

            clone.SelectedEditorPlugIn = SelectedEditorPlugIn;
            clone.SelectedRendererPlugIn = SelectedRendererPlugIn;

            clone.AutoUpdateEnabled = AutoUpdateEnabled;
            clone.AutoUpdateFrequency = AutoUpdateFrequency;

            clone.BondLength = BondLength;

            clone.SetBondLengthOnImportFromFile = SetBondLengthOnImportFromFile;
            clone.SetBondLengthOnImportFromSearch = SetBondLengthOnImportFromSearch;
            clone.SetBondLengthOnImportFromLibrary = SetBondLengthOnImportFromLibrary;

            clone.RemoveExplicitHydrogensOnImportFromFile = RemoveExplicitHydrogensOnImportFromFile;
            clone.RemoveExplicitHydrogensOnImportFromSearch = RemoveExplicitHydrogensOnImportFromSearch;
            clone.RemoveExplicitHydrogensOnImportFromLibrary = RemoveExplicitHydrogensOnImportFromLibrary;

            return clone;
        }

        public void RestoreDefaults()
        {
            // User Options
            TelemetryEnabled = true;

            SelectedEditorPlugIn = Constants.DefaultEditorPlugIn;
            SelectedRendererPlugIn = Constants.DefaultRendererPlugIn;

            AutoUpdateEnabled = true;
            AutoUpdateFrequency = 7;

            BondLength = (int)Constants.StandardBondLength;

            SetBondLengthOnImportFromFile = true;
            SetBondLengthOnImportFromSearch = true;
            SetBondLengthOnImportFromLibrary = true;

            RemoveExplicitHydrogensOnImportFromFile = false;
            RemoveExplicitHydrogensOnImportFromSearch = false;
            RemoveExplicitHydrogensOnImportFromLibrary = false;
        }
    }
}