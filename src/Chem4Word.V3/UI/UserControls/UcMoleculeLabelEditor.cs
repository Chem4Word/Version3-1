// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.UI.UserControls
{
    public partial class UcMoleculeLabelEditor : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private int _maxFormulaId;
        private int _maxNameId;
        private string _cml;

        public Molecule Molecule { get; set; }
        public List<string> Used1D { get; set; }

        public UcMoleculeLabelEditor()
        {
            InitializeComponent();
        }

        private int MaxId(string id, int current)
        {
            int result = current;

            string[] parts = id.Split('.');

            if (parts.Length > 1)
            {
                string s = parts[1].Replace("n", "").Replace("f", "");
                int n;
                if (int.TryParse(s, out n))
                {
                    result = Math.Max(current, n);
                }
            }

            return result;
        }

        private void RefreshFormulaePanel()
        {
            panelFormulae.AutoScroll = false;
            panelFormulae.Controls.Clear();

            int i = 0;
            foreach (var f in Molecule.Formulas)
            {
                UcEditFormula elc = new UcEditFormula(this);
                elc.IsLoading = true;
                elc.Id = f.Id;
                _maxFormulaId = MaxId(f.Id, _maxFormulaId);
                elc.Parent = panelFormulae;
                elc.Location = new Point(5, 5 + i * elc.ClientRectangle.Height);
                elc.Width = panelFormulae.Width - 10;
                elc.FormulaText = f.Value;
                elc.CanDelete = !Used1D.Any(s => s.StartsWith(f.Id));
                elc.CanEdit = f.Type.Equals(Constants.Chem4WordUserFormula);
                elc.Convention = f.Type;
                elc.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                elc.IsLoading = false;
                i++;
            }

            panelFormulae.AutoScroll = true;
        }

        private void RefreshNamesPanel()
        {
            panelNames.AutoScroll = false;
            panelNames.Controls.Clear();

            int i = 0;
            foreach (var n in Molecule.Names)
            {
                UcEditName elc = new UcEditName(this);
                elc.Id = n.Id;
                _maxNameId = MaxId(n.Id, _maxNameId);
                elc.Parent = panelNames;
                elc.Location = new Point(5, 5 + i * elc.ClientRectangle.Height);
                elc.Width = panelNames.Width - 10;
                elc.DictRef = n.Type;
                elc.NameText = n.Value;
                elc.CanDelete = !Used1D.Any(s => s.StartsWith(n.Id));
                elc.CanEdit = n.Type.Equals(Constants.Chem4WordUserSynonym);
                elc.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                i++;
            }

            panelNames.AutoScroll = true;
        }

        public void RefreshPanels()
        {
            RefreshFormulaePanel();
            RefreshNamesPanel();
            if (string.IsNullOrEmpty(_cml))
            {
                Model model = new Model();
                model.AddMolecule(Molecule);
                Molecule.Parent = model;
                CMLConverter cmlConverter = new CMLConverter();
                _cml = cmlConverter.Export(model);
                display1.Chemistry = _cml;
            }
        }

        public void ChangeFormulaValueAt(string id, string value)
        {
            foreach (var f in Molecule.Formulas)
            {
                if (f.Id.Equals(id))
                {
                    f.Value = value;
                    break;
                }
            }
        }

        public void DeleteFourmulaAt(string id)
        {
            foreach (var f in Molecule.Formulas.ToList())
            {
                if (f.Id.Equals(id))
                {
                    Molecule.Formulas.Remove(f);
                    RefreshFormulaePanel();
                    break;
                }
            }
        }

        public void ChangeNameValueAt(string id, string name)
        {
            foreach (var n in Molecule.Names)
            {
                if (n.Id.Equals(id))
                {
                    n.Value = name;
                    break;
                }
            }
        }

        public void DeleteChemicalNameAt(string id)
        {
            foreach (var n in Molecule.Names.ToList())
            {
                if (n.Id.Equals(id))
                {
                    Molecule.Names.Remove(n);
                    RefreshNamesPanel();
                    break;
                }
            }
        }

        private void UcMoleculeLabelEditor_Load(object sender, EventArgs e)
        {
            // Do Nothing
            display1.Background = System.Windows.Media.Brushes.White;
        }

        private void OnAddNameClick(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                var n = new TextualProperty();
                n.Type = Constants.Chem4WordUserSynonym;
                n.Value = "";
                n.Id = $"{Molecule.Id}.n{++_maxNameId}";
                Molecule.Names.Add(n);
                RefreshNamesPanel();
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex).ShowDialog();
            }
        }

        private void OnAddFormulaClick(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                var f = new TextualProperty();
                f.Type = Constants.Chem4WordUserFormula;
                f.Value = "";
                f.Id = $"{Molecule.Id}.f{++_maxFormulaId}";
                Molecule.Formulas.Add(f);
                RefreshFormulaePanel();
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex).ShowDialog();
            }
        }
    }
}