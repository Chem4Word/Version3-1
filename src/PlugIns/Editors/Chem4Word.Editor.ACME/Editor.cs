// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.ACME;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;

namespace Chem4Word.Editor.ACME
{
    public class Editor : IChem4WordEditor
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "ACME Structure Editor";

        public string Description => "This is the brand new Chem4Word editor. ACME stands for A Chemical Molecule Editor.";

        public bool HasSettings => true;

        public bool CanEditNestedMolecules => true;
        public bool CanEditFunctionalGroups => true;
        public bool RequiresSeedAtom => false;
        public List<string> Used1DProperties { get; set; }

        public Point TopLeft { get; set; }

        public IChem4WordTelemetry Telemetry { get; set; }

        public string ProductAppDataPath { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public string Cml { get; set; }

        private Options _editorOptions = new Options();

        public bool ChangeSettings(Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                if (HasSettings)
                {
                    LoadSettings();
                }

                Settings settings = new Settings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                Options tempOptions = _editorOptions.Clone();
                settings.SettingsPath = ProductAppDataPath;
                settings.EditorOptions = tempOptions;

                DialogResult dr = settings.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    _editorOptions = tempOptions.Clone();
                }
                settings.Close();
                settings = null;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }
            return true;
        }

        public void LoadSettings()
        {
            if (!string.IsNullOrEmpty(ProductAppDataPath))
            {
                string fileName = $"{_product}.json";
                string optionsFile = Path.Combine(ProductAppDataPath, fileName);
                _editorOptions = FileUtils.LoadAcmeSettings(optionsFile, Telemetry, TopLeft);
                _editorOptions.Dirty = false;
                _editorOptions.SettingsFile = optionsFile;
            }
        }

        public DialogResult Edit()
        {
            DialogResult dialogResult = DialogResult.Cancel;

            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                if (HasSettings)
                {
                    LoadSettings();
                }

                using (EditorHost host = new EditorHost(Cml, Used1DProperties, _editorOptions))
                {
                    host.TopLeft = TopLeft;

                    DialogResult showDialog = host.ShowDialog();
                    if (showDialog == DialogResult.OK)
                    {
                        dialogResult = showDialog;
                        Cml = host.OutputValue;
                    }
                }
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return dialogResult;
        }
    }
}