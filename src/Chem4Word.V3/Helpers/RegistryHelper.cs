﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Chem4Word.Core.Helpers;
using Microsoft.Win32;

namespace Chem4Word.Helpers
{
    public static class RegistryHelper
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private static int _counter = 1;

        public static void SendSetupActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordSetupRegistryKey, true);
            if (rk != null)
            {
                string[] names = rk.GetValueNames();
                List<string> values = new List<string>();
                foreach (var name in names)
                {
                    string message = rk.GetValue(name).ToString();

                    string timestamp = name;
                    int bracket = timestamp.IndexOf("[", StringComparison.InvariantCulture);
                    if (bracket > 0)
                    {
                        timestamp = timestamp.Substring(0, bracket).Trim();
                    }

                    values.Add($"{timestamp} {message}");
                    rk.DeleteValue(name);
                }

                if (values.Any())
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Setup", string.Join(Environment.NewLine, values));
                }
            }
        }

        public static void SendUpdateActions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordUpdateRegistryKey, true);
            if (rk != null)
            {
                string[] names = rk.GetValueNames();
                List<string> values = new List<string>();
                foreach (var name in names)
                {
                    string message = rk.GetValue(name).ToString();

                    string timestamp = name;
                    int bracket = timestamp.IndexOf("[", StringComparison.InvariantCulture);
                    if (bracket > 0)
                    {
                        timestamp = timestamp.Substring(0, bracket).Trim();
                    }

                    values.Add($"{timestamp} {message}");
                    rk.DeleteValue(name);
                }
                if (values.Any())
                {
                    Globals.Chem4WordV3.Telemetry.Write(module, "Update", string.Join(Environment.NewLine, values));
                }
            }
        }

        public static void SendExceptions()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(Constants.Chem4WordExceptionsRegistryKey, true);
            int messageSize = 0;
            if (rk != null)
            {
                string[] names = rk.GetValueNames();
                List<string> values = new List<string>();
                foreach (var name in names)
                {
                    string message = rk.GetValue(name).ToString();

                    string timestamp = name;
                    int bracket = timestamp.IndexOf("[", StringComparison.InvariantCulture);
                    if (bracket > 0)
                    {
                        timestamp = timestamp.Substring(0, bracket).Trim();
                    }

                    values.Add($"{timestamp} {message}");
                    messageSize += timestamp.Length + message.Length;
                    if (messageSize > 30000)
                    {
                        SendData(module, values);
                        values = new List<string>();
                        messageSize = 0;
                    }
                    rk.DeleteValue(name);
                }

                SendData(module, values);
            }
        }

        private static void SendData(string module, List<string> values)
        {
            if (values.Any())
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Exception", string.Join(Environment.NewLine, values));
            }
        }

        public static void StoreMessage(string module, string message)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(Constants.Chem4WordExceptionsRegistryKey);
            if (key != null)
            {
                int procId = 0;
                try
                {
                    procId = Process.GetCurrentProcess().Id;
                }
                catch
                {
                    //
                }

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                key.SetValue($"{timestamp} [{procId}.{_counter++:000}]", $"[{procId}] {module} {message}");
            }
        }

        public static void StoreException(string module, Exception exception)
        {
            StoreMessage(module, exception.ToString());
        }
    }
}