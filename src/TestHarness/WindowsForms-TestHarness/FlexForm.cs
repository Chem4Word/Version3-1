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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;

namespace WinFormsTestHarness
{
    public partial class FlexForm : Form
    {
        private Model _model = null;

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

            openFileDialog1.Filter = sb.ToString();
            openFileDialog1.FileName = "";

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
                        _model = sdFileConverter.Import(mol);
                        _model.RefreshMolecules();
                        _model.Relabel();
                        cml = cmlConvertor.Export(_model);
                        //model.DumpModel("After Import");

                        break;

                    case ".cml":
                    case ".xml":
                        _model = cmlConvertor.Import(mol);
                        _model.RefreshMolecules();
                        _model.Relabel();
                        cml = cmlConvertor.Export(_model);
                        break;
                }

                ShowChemistry(filename, _model);
            }
        }

        private void ChangeBackground_Click(object sender, EventArgs e)
        {
            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                elementHost1.BackColor = colorDialog1.Color;
                display1.BackgroundColor = ColorToBrush(elementHost1.BackColor);
            }
        }

        private void EditStructure_Click(object sender, EventArgs e)
        {
            Model model = display1.Chemistry as Model;
            if (model != null)
            {
                CMLConverter cc = new CMLConverter();
                EditorHost editorHost = new EditorHost(cc.Export(model), EditorType.Text);
                editorHost.ShowDialog();
                if (editorHost.Result == DialogResult.OK)
                {
                    Model m = cc.Import(editorHost.OutputValue);
                    ShowChemistry("Edited", m);
                }
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
                    _model = mod;
                    if (!string.IsNullOrEmpty(filename))
                    {
                        Text = filename;
                    }
                    display1.BackgroundColor = ColorToBrush(elementHost1.BackColor);
                    display1.Chemistry = _model;
                    ShowCarbons.Checked = false;
                    EditStructure.Enabled = true;
                    ShowCarbons.Enabled = true;
                    RemoveAtom.Enabled = true;
                    RandomElement.Enabled = true;
                    EditorType.Enabled = true;
                }
            }
        }

        private void SetCarbons(Model model, bool state)
        {
            foreach (var atom in model.AllAtoms)
            {
                if (atom.Element.Symbol.Equals("C"))
                {
                    atom.ShowSymbol = state;
                }
            }
        }

        private Brush ColorToBrush(System.Drawing.Color colour)
        {
            string hex = $"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";
            var converter = new BrushConverter();
            return (Brush)converter.ConvertFromString(hex);
        }

        private void ShowCarbons_CheckedChanged(object sender, EventArgs e)
        {
            Model model = display1.Chemistry as Model;
            if (model != null)
            {
                Model newModel = model.Clone();
                newModel.FontSize = display1.FontSize;
                SetCarbons(newModel, ShowCarbons.Checked);
                Debug.WriteLine($"Old Model: ({model.MinX}, {model.MinY}):({model.MaxX}, {model.MaxY})");
                Debug.WriteLine($"New Model: ({newModel.MinX}, {newModel.MinY}):({newModel.MaxX}, {newModel.MaxY})");
                //newModel.RebuildMolecules();
                display1.Chemistry = newModel;
            }
        }

        private void RemoveAtom_Click(object sender, EventArgs e)
        {
            Model model = display1.Chemistry as Model;
            if (model != null)
            {
                if (model.AllAtoms.Any())
                {
                    Molecule modelMolecule = model.Molecules.Where(m => m.Atoms.Any()).FirstOrDefault();
                    var atom = modelMolecule.Atoms[0];
                    foreach (var neighbouringBond in atom.Bonds)
                    {
                        neighbouringBond.OtherAtom(atom).Bonds.Remove(neighbouringBond);
                        modelMolecule.Bonds.Remove(neighbouringBond);
                    }

                    modelMolecule.Atoms.Remove(atom);
                }

                model.RefreshMolecules();
            }
        }

        private void RandomElement_Click(object sender, EventArgs e)
        {
            Model model = display1.Chemistry as Model;
            if (model != null)
            {
                if (model.AllAtoms.Any())
                {
                    var rnd = new Random(DateTime.Now.Millisecond);

                    var maxAtoms = model.AllAtoms.Count;
                    int targetAtom = rnd.Next(0, maxAtoms);

                    var elements = Globals.PeriodicTable.Elements;
                    int newElement = rnd.Next(0, elements.Values.Max(v => v.AtomicNumber));
                    var x = elements.Values.Where(v => v.AtomicNumber == newElement).FirstOrDefault();

                    if (x == null)
                    {
                        Debugger.Break();
                    }
                    model.AllAtoms[targetAtom].Element = x as ElementBase;
                    if (x.Symbol.Equals("C"))
                    {
                        model.AllAtoms[targetAtom].ShowSymbol = ShowCarbons.Checked;
                    }
                    model.RefreshMolecules();
                }
            }
        }

        private void FlexForm_Load(object sender, EventArgs e)
        {
            EditorType.Items.Clear();
            EditorType.Items.Add("ACME");
            EditorType.Items.Add("CML");
            EditorType.SelectedIndex = 0;
        }

        private void Serialize_Click(object sender, EventArgs e)
        {
            Model model = display1.Chemistry as Model;
            if (model != null)
            {
                string filename = $"{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.bin";
                string targetFile = Path.Combine(@"C:\Temp", filename);

                Debug.WriteLine("Serialising model as binary.");
                Stopwatch sw = new Stopwatch();
                sw.Start();

                MemoryStream ms1 = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms1, model);

                ms1.Position = 0;
                using (FileStream file = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
                {
                    byte[] bytes = new byte[ms1.Length];
                    ms1.Read(bytes, 0, (int)ms1.Length);
                    file.Write(bytes, 0, bytes.Length);
                    ms1.Close();
                }
                sw.Stop();

                Debug.WriteLine($" Binary serialisation took {sw.ElapsedMilliseconds} milliseconds.");

                Debug.WriteLine("Serialising model as CML to file.");
                sw.Reset();
                sw.Start();
                CMLConverter cc = new CMLConverter();
                File.WriteAllText(targetFile.Replace(".bin", ".cml"), cc.Export(model));
                sw.Stop();
                Debug.WriteLine($" Writing CML file took {sw.ElapsedMilliseconds} milliseconds.");

                MemoryStream ms2 = new MemoryStream();
                using (FileStream file = new FileStream(targetFile, FileMode.Open, FileAccess.Read))
                {
                    byte[] bytes = new byte[file.Length];
                    file.Read(bytes, 0, (int)file.Length);
                    ms2.Write(bytes, 0, (int)file.Length);
                }

                ms2.Position = 0;
                Model x = new BinaryFormatter().Deserialize(ms2) as Model;
                x.RebuildMolecules();
                display1.Chemistry = x;
            }
        }

        private void Examine_Click(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(@"C:\Temp", "*.bin");
            MemoryStream memoryStream = new MemoryStream();
            using (FileStream file = new FileStream(files.Last(), FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                memoryStream.Write(bytes, 0, (int)file.Length);
            }

            memoryStream.Position = 0;
            BinarySerializationStreamAnalyzer analyzer = new BinarySerializationStreamAnalyzer();
            analyzer.Read(memoryStream);

            Dumper dumper = new Dumper(analyzer.Analyze());
            dumper.ShowDialog();
        }
    }
}