﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Reflection;
using Chem4Word.ACME;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    internal class TaskPaneHelper
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public static void InsertChemistry(bool isCopy, Application app, Display display, bool fromLibrary)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Document doc = app.ActiveDocument;
            Selection sel = app.Selection;
            ContentControl cc = null;

            if (Globals.Chem4WordV3.SystemOptions == null)
            {
                Globals.Chem4WordV3.LoadOptions();
            }

            bool allowed = true;
            string reason = "";

            if (Globals.Chem4WordV3.ChemistryAllowed)
            {
                if (sel.ContentControls.Count > 0)
                {
                    cc = sel.ContentControls[1];
                    if (cc.Title != null && cc.Title.Equals(Constants.ContentControlTitle))
                    {
                        reason = "a chemistry object is selected";
                        allowed = false;
                    }
                }
            }
            else
            {
                reason = Globals.Chem4WordV3.ChemistryProhibitedReason;
                allowed = false;
            }

            if (allowed)
            {
                try
                {
                    CMLConverter cmlConverter = new CMLConverter();
                    var model = cmlConverter.Import(display.Chemistry.ToString());

                    if (fromLibrary)
                    {
                        if (Globals.Chem4WordV3.SystemOptions.RemoveExplicitHydrogensOnImportFromLibrary)
                        {
                            model.RemoveExplicitHydrogens();
                        }

                        var outcome = model.EnsureBondLength(Globals.Chem4WordV3.SystemOptions.BondLength,
                                               Globals.Chem4WordV3.SystemOptions.SetBondLengthOnImportFromLibrary);
                        if (!string.IsNullOrEmpty(outcome))
                        {
                            Globals.Chem4WordV3.Telemetry.Write(module, "Information", outcome);
                        }
                    }

                    cc = ChemistryHelper.Insert2DChemistry(doc, cmlConverter.Export(model), isCopy);
                }
                catch (Exception ex)
                {
                    using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                    {
                        form.ShowDialog();
                    }
                }
                finally
                {
                    if (cc != null)
                    {
                        // Move selection point into the Content Control which was just edited or added
                        app.Selection.SetRange(cc.Range.Start, cc.Range.End);
                    }
                }
            }
            else
            {
                UserInteractions.WarnUser($"You can't insert a chemistry object because {reason}");
            }
        }
    }
}