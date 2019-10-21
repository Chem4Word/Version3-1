// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Core.Helpers
{
    public static class Constants
    {
        public static string Chem4WordVersion = "3.1";
        public static string Chem4WordVersionFiles = "files3-1";
        public static string ContentControlTitle = "Chemistry";
        public static string LegacyContentControlTitle = "chemistry";
        public static string NavigatorTaskPaneTitle = "Navigator";
        public static string LibraryTaskPaneTitle = "Library";

        public static double TopLeftOffset = 24;
        public static string OoXmlBookmarkPrefix = "C4W_";
        public const string LibraryFileName = "Library.db";

        public static string Chem4WordWebServiceUri = "https://chemicalservices.azurewebsites.net/api/Resolve";

        public static string DefaultEditorPlugIn = "ACME Structure Editor";
        public static string DefaultEditorPlugIn800 = "ChemDoodle Web Structure Editor V8.0.0";
        public static string DefaultEditorPlugIn702 = "ChemDoodle Web Structure Editor V7.0.2";
        public static int ChemDoodleWeb800MinimumBrowserVersion = 11;

        public static string DefaultRendererPlugIn = "Open Office Xml Renderer V4";

        // Registry Locations
        public const string Chem4WordRegistryKey = @"SOFTWARE\Chem4Word V3";

        public const string RegistryValueNameLastCheck = "Last Update Check";
        public const string RegistryValueNameVersionsBehind = "Versions Behind";
        public const string Chem4WordSetupRegistryKey = @"SOFTWARE\Chem4Word V3\Setup";
        public const string Chem4WordUpdateRegistryKey = @"SOFTWARE\Chem4Word V3\Update";

        // Update Checks
        public const int MaximunVersionsBehind = 7;

        // Bond length limits etc
        public const double MinimumBondLength = 5;

        public const double StandardBondLength = 20;
        public const double MaximumBondLength = 95;
        public const double BondLengthTolerance = 1;

    }
}