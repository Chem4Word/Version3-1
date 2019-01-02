// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using IChem4Word.Contracts;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model;
using Chem4Word.Model.Converters;
using Chem4Word.Model.Converters.CML;

namespace Chem4Word.Editor.ACME
{
    public class Editor : IChem4WordEditor
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "ACME Structure Editor";

        public string Description => "This is the brand new editor";

        public bool HasSettings => false;

        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public string ProductAppDataPath { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public string Cml { get; set; }

        public bool ChangeSettings(Point topLeft)
        {
            // This PlugIn has no settings
            return false;
        }

        public void LoadSettings()
        {
            // This PlugIn has no settings
        }

        public DialogResult Edit()
        {
            DialogResult result = DialogResult.Cancel;

            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                if (HasSettings)
                {
                    LoadSettings();
                }

                // Strip off Formulae and ChemicalNames as we don't edit them here
                CMLConverter cmlConverter = new CMLConverter();
                Model.Model model = cmlConverter.Import(Cml);
                foreach (Molecule molecule in model.Molecules)
                {
                    molecule.ChemicalNames.Clear();
                    molecule.Formulas.Clear();
                    molecule.ConciseFormula = "";
                }

                EditorHost host = new EditorHost(cmlConverter.Export(model));
                host.TopLeft = TopLeft;
                host.ShowDialog();
                result = host.Result;
                Cml = host.OutputValue;
                host.Close();
                host.Dispose();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return result;
        }
    }
}
