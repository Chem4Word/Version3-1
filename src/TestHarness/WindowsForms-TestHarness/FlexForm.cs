// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Converters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;

namespace WinFormsTestHarness
{
    public partial class FlexForm : Form
    {
        private Model model = null;

        public FlexForm()
        {
            InitializeComponent();
        }

        private void LoadStructure_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("All molecule files (*.mol, *.sdf, *.cml)|*.mol;*.sdf;*.cml");
            sb.Append("|CML molecule files (*.cml)|*.cml");
            sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");

            openFileDialog1.FileName = "*.*";
            openFileDialog1.Filter = sb.ToString();

            DialogResult dr = openFileDialog1.ShowDialog();

            if (dr == DialogResult.OK)
            {
                string fileType = Path.GetExtension(openFileDialog1.FileName).ToLower();
                string filename = Path.GetFileName(openFileDialog1.FileName);
                string mol = File.ReadAllText(openFileDialog1.FileName);
                string cml = "";

                CMLConverter cmlConvertor = new CMLConverter();
                SdFileConverter sdFileConverter = new SdFileConverter();

                switch (fileType)
                {
                    case ".mol":
                    case ".sdf":
                        model = sdFileConverter.Import(mol);
                        model.RefreshMolecules();
                        model.Relabel();
                        cml = cmlConvertor.Export(model);
                        //model.DumpModel("After Import");

                        break;

                    case ".cml":
                    case ".xml":
                        model = cmlConvertor.Import(mol);
                        model.RefreshMolecules();
                        model.Relabel();
                        cml = cmlConvertor.Export(model);
                        break;
                }

                ShowChemistry(filename, model);
            }
        }

        private void ShowChemistry(string filename, Model mod)
        {
            if (mod != null)
            {
                if (mod.AllErrors.Any() || mod.AllWarnings.Any())
                {
                    List<string> lines = new List<string>();
                    if (mod.AllErrors.Any())
                    {
                        lines.Add("Error(s)");
                        lines.AddRange(mod.AllErrors);
                    }

                    if (mod.AllWarnings.Any())
                    {
                        lines.Add("Warnings(s)");
                        lines.AddRange(mod.AllWarnings);
                    }

                    MessageBox.Show(string.Join(Environment.NewLine, lines));
                }
                else
                {
                    model = mod;
                    this.Text = filename;
                    this.display1.Chemistry = model;
                    display1.ShowCarbonLabels = true;
                    display1.Background = new SolidColorBrush(System.Windows.Media.Colors.Gray);
                    display1.Mask = new SolidColorBrush(System.Windows.Media.Color.FromArgb(elementHost1.BackColor.A, elementHost1.BackColor.R, elementHost1.BackColor.G, elementHost1.BackColor.B));
                    EditStructure.Enabled = true;
                }
            }
        }

        private void EditStructure_Click(object sender, EventArgs e)
        {
            if (model != null)
            {
                CMLConverter cc = new CMLConverter();
                EditorHost editorHost = new EditorHost(cc.Export(model));
                editorHost.ShowDialog();
                if (editorHost.Result == DialogResult.OK)
                {
                    Model m = cc.Import(editorHost.OutputValue);
                    ShowChemistry("Edited", m);
                }
            }
        }

        private void FlexForm_Load(object sender, EventArgs e)
        {

        }
    }
}