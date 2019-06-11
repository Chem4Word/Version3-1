// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Chem4Word.Core.UI.Forms;
using IChem4Word.Contracts;
using Newtonsoft.Json;

namespace Chem4Word.ACME.Utils
{
    public static class FileUtils
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public static Options LoadAcmeSettings(string settingsFile, IChem4WordTelemetry telemetry, Point topLeft)
        {
            Options result = new Options();

            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!string.IsNullOrEmpty(settingsFile))
                {
                    if (File.Exists(settingsFile))
                    {
                        string json = File.ReadAllText(settingsFile);
                        result = JsonConvert.DeserializeObject<Options>(json);
                        string temp = JsonConvert.SerializeObject(result, Formatting.Indented);
                        if (!json.Equals(temp))
                        {
                            File.WriteAllText(settingsFile, temp);
                        }
                    }
                    else
                    {
                        result.RestoreDefaults();
                        string json = JsonConvert.SerializeObject(result, Formatting.Indented);
                        File.WriteAllText(settingsFile, json);
                    }

                    // Clear the Dirty flag
                    result.Dirty = false;
                }
            }
            catch (Exception ex)
            {
                new ReportError(telemetry, topLeft, module, ex).ShowDialog();
            }

            return result;
        }

        public static void SaveAcmeSettings(Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!string.IsNullOrEmpty(options.SettingsFile))
                {
                    string temp = JsonConvert.SerializeObject(options, Formatting.Indented);
                    File.WriteAllText(options.SettingsFile, temp);

                    // Clear the Dirty flag
                    options.Dirty = false;
                }
            }
            catch (Exception ex)
            {
                new ReportError(telemetry, topLeft, module, ex).ShowDialog();
            }
        }
    }
}