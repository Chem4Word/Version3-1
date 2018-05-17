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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Chem4Word.ViewModel;

namespace WinFormsTestHarness
{
    public partial class FlexForm : Form
    {
        private Stack<Model> _undoStack = new Stack<Model>();
        private Stack<Model> _redoStack = new Stack<Model>();

        public FlexForm()
        {
            InitializeComponent();
        }

        private void LoadStructure_Click(object sender, EventArgs e)
        {
            Model model = null;

            StringBuilder sb = new StringBuilder();
            sb.Append("All molecule files (*.mol, *.sdf, *.cml)|*.mol;*.sdf;*.cml");
            sb.Append("|CML molecule files (*.cml)|*.cml");
            sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");

            openFileDialog1.Title = "Open Structure";
            openFileDialog1.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            openFileDialog1.Filter = sb.ToString();
            openFileDialog1.FileName = "";
            openFileDialog1.ShowHelp = false;

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
                        break;

                    case ".cml":
                    case ".xml":
                        model = cmlConvertor.Import(mol);
                        model.RefreshMolecules();
                        model.Relabel();
                        cml = cmlConvertor.Export(model);
                        break;
                }

                if (model != null)
                {
                    Model existing = Display.Chemistry as Model;
                    if (existing != null)
                    {
                        Model clone = existing.Clone();
                        clone.RescaleForCml();

                        Debug.WriteLine($"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength} onto Stack");
                        _undoStack.Push(clone);
                    }

                    if (model.MeanBondLength < 5 || model.MeanBondLength > 95)
                    {
                        model.ScaleToAverageBondLength(20);
                    }
                    ShowChemistry(filename, model);
                }
            }
        }

        private void ChangeBackground_Click(object sender, EventArgs e)
        {
            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                DisplayHost.BackColor = colorDialog1.Color;
                //Display.BackgroundColor = ColorToBrush(DisplayHost.BackColor);
            }
        }

        private void EditStructure_Click(object sender, EventArgs e)
        {
            Model model = Display.Chemistry as Model;
            if (model != null)
            {
                Model clone = model.Clone();
                clone.RescaleForCml();

                CMLConverter cc = new CMLConverter();
                EditorHost editorHost = new EditorHost(cc.Export(clone), EditorType.Text);
                editorHost.ShowDialog();
                if (editorHost.Result == DialogResult.OK)
                {
                    Debug.WriteLine($"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength} onto Stack");
                    _undoStack.Push(clone);
                    Model m = cc.Import(editorHost.OutputValue);
                    ShowChemistry($"Edited {m.ConciseFormula}", m);
                }
            }
        }

        private void ShowChemistry(string filename, Model model)
        {
            if (model != null)
            {
                if (model.AllErrors.Any() || model.AllWarnings.Any())
                {
                    List<string> lines = new List<string>();
                    if (model.AllErrors.Any())
                    {
                        lines.Add("Error(s)");
                        lines.AddRange(model.AllErrors);
                    }

                    if (model.AllWarnings.Any())
                    {
                        lines.Add("Warnings(s)");
                        lines.AddRange(model.AllWarnings);
                    }

                    MessageBox.Show(string.Join(Environment.NewLine, lines));
                }
                else
                {
                    if (!string.IsNullOrEmpty(filename))
                    {
                        Text = filename;
                    }
                    Information.Text = $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength}";

                    //Display.BackgroundColor = ColorToBrush(DisplayHost.BackColor);
                    model.RefreshMolecules();

                    Display.Chemistry = model;
                    Debug.WriteLine($"FlexForm is displaying {model.ConciseFormula}");

                    EnableNormalButtons();
                    EnableUndoRedoButtons();
                }
            }
        }

        private void EnableNormalButtons()
        {
            ShowCarbons.Checked = false;
            EditStructure.Enabled = true;
            ShowCarbons.Enabled = true;
            RemoveAtom.Enabled = true;
            RandomElement.Enabled = true;
            EditorType.Enabled = true;
        }

        private List<DisplayViewModel> StackToList(Stack<Model> stack)
        {
            List<DisplayViewModel> list = new List<DisplayViewModel>();
            CMLConverter cc = new CMLConverter();
            foreach (var item in stack)
            {
                list.Add(new DisplayViewModel
                {
                    Model = cc.Import(cc.Export(item))
                });
            }
            return list;
        }

        private void EnableUndoRedoButtons()
        {
            Redo.Enabled = _redoStack.Count > 0;
            Undo.Enabled = _undoStack.Count > 0;

            UndoStack.StackList.ItemsSource = StackToList(_undoStack);
            RedoStack.StackList.ItemsSource = StackToList(_redoStack);

            //ListStacks();

            //// Select last item
            //UndoStack.StackList.SelectedIndex = UndoStack.StackList.Items.Count - 1;
            //UndoStack.StackList.ScrollIntoView(UndoStack.StackList.SelectedItem);

            //// Select last item
            //RedoStack.StackList.SelectedIndex = RedoStack.StackList.Items.Count - 1;
            //RedoStack.StackList.ScrollIntoView(RedoStack.StackList.SelectedItem);
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
            Model model = Display.Chemistry as Model;
            if (model != null)
            {
                Model newModel = model.Clone();
                SetCarbons(newModel, ShowCarbons.Checked);
                Debug.WriteLine($"Old Model: ({model.MinX}, {model.MinY}):({model.MaxX}, {model.MaxY})");
                Debug.WriteLine($"New Model: ({newModel.MinX}, {newModel.MinY}):({newModel.MaxX}, {newModel.MaxY})");
                Display.Chemistry = newModel;
            }
        }

        private void RemoveAtom_Click(object sender, EventArgs e)
        {
            Model model = Display.Chemistry as Model;
            if (model != null)
            {
                Model clone = model.Clone();
                clone.RescaleForCml();
                _undoStack.Push(clone);
                EnableUndoRedoButtons();

                //Model clone = null;
                //if (_cloneViaCml)
                //{
                //    // This works ...
                //    CMLConverter cc = new CMLConverter();
                //    clone = cc.Import(cc.Export(model));
                //}
                //else
                //{
                //    // This is not right ...
                //    int beforeAtoms = model.AllAtoms.Count;
                //    int beforeBonds = model.AllBonds.Count;
                //    Debug.WriteLine($"Before Clone {model.AllAtoms.Count} {model.AllBonds.Count}");

                //    clone = model.Clone();
                //    int afterAtoms = model.AllAtoms.Count;
                //    int afterBonds = model.AllBonds.Count;

                //    if (beforeAtoms != afterAtoms
                //        || beforeBonds != afterBonds
                //        || clone.AllAtoms.Count != model.AllAtoms.Count
                //        || clone.AllBonds.Count != model.AllBonds.Count)
                //    {
                //        Debug.WriteLine($"After Clone {model.AllAtoms.Count} {model.AllBonds.Count}");
                //        Debug.WriteLine($"Clone {clone.AllAtoms.Count} {clone.AllBonds.Count}");
                //        int cloneAtoms = clone.AllAtoms.Count;
                //        int cloneBonds = clone.AllBonds.Count;
                //        Debugger.Break();
                //    }
                //}

                Model clone2 = clone.Clone();
                if (clone2.AllAtoms.Any())
                {
                    Molecule modelMolecule = clone2.Molecules.Where(m => m.Atoms.Any()).FirstOrDefault();
                    var atom = modelMolecule.Atoms[0];
                    foreach (var neighbouringBond in atom.Bonds)
                    {
                        neighbouringBond.OtherAtom(atom).Bonds.Remove(neighbouringBond);
                        modelMolecule.Bonds.Remove(neighbouringBond);
                    }

                    modelMolecule.Atoms.Remove(atom);
                }

                foreach (var mol in clone2.Molecules)
                {
                    mol.ConciseFormula = "";
                }

                clone2.RefreshMolecules();
                //display1.Chemistry = model;
                ShowChemistry("Remove Atom", clone2);
            }
        }

        private void RandomElement_Click(object sender, EventArgs e)
        {
            Model model = Display.Chemistry as Model;
            if (model != null)
            {
                Model clone = model.Clone();
                clone.RescaleForCml();
                _undoStack.Push(clone);
                EnableUndoRedoButtons();

                //if (_cloneViaCml)
                //{
                //    // This works ...
                //    CMLConverter cc = new CMLConverter();
                //    clone = cc.Import(cc.Export(model));
                //}
                //else
                //{
                //    // This is not right ...
                //    int beforeAtoms = model.AllAtoms.Count;
                //    int beforeBonds = model.AllBonds.Count;
                //    Debug.WriteLine($"Before Clone {model.AllAtoms.Count} {model.AllBonds.Count}");

                //    clone = model.Clone();
                //    int afterAtoms = model.AllAtoms.Count;
                //    int afterBonds = model.AllBonds.Count;

                //    if (beforeAtoms != afterAtoms
                //        || beforeBonds != afterBonds
                //        || clone.AllAtoms.Count != model.AllAtoms.Count
                //        || clone.AllBonds.Count != model.AllBonds.Count)
                //    {
                //        Debug.WriteLine($"After Clone {model.AllAtoms.Count} {model.AllBonds.Count}");
                //        Debug.WriteLine($"Clone {clone.AllAtoms.Count} {clone.AllBonds.Count}");
                //        int cloneAtoms = clone.AllAtoms.Count;
                //        int cloneBonds = clone.AllBonds.Count;
                //        Debugger.Break();
                //    }
                //}

                Model clone2 = clone.Clone();
                if (clone2.AllAtoms.Any())
                {
                    var rnd = new Random(DateTime.Now.Millisecond);

                    var maxAtoms = clone2.AllAtoms.Count;
                    int targetAtom = rnd.Next(0, maxAtoms);

                    var elements = Globals.PeriodicTable.Elements;
                    int newElement = rnd.Next(0, elements.Values.Max(v => v.AtomicNumber));
                    var x = elements.Values.Where(v => v.AtomicNumber == newElement).FirstOrDefault();

                    if (x == null)
                    {
                        Debugger.Break();
                    }
                    clone2.AllAtoms[targetAtom].Element = x as ElementBase;
                    if (x.Symbol.Equals("C"))
                    {
                        clone2.AllAtoms[targetAtom].ShowSymbol = ShowCarbons.Checked;
                    }

                    foreach (var mol in clone2.Molecules)
                    {
                        mol.ConciseFormula = "";
                    }
                    clone2.RefreshMolecules();
                }

                //display1.Chemistry = model;
                ShowChemistry("Random Element", clone2);
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
            Model model = Display.Chemistry as Model;
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
                cc.Compressed = true;
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
                Display.Chemistry = x;
            }
        }

        private void Examine_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        private void Hex_Click(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(@"C:\Temp", "*.bin");
            HexViewer hexViewer = new HexViewer(files.Last());
            hexViewer.ShowDialog();
        }

        private void Timing_Click(object sender, EventArgs e)
        {
            Model x = Display.Chemistry as Model;
            if (x != null)
            {
                int max = 1000;
                Stopwatch sw = new Stopwatch();
                CMLConverter cc = new CMLConverter();
                string type = "model";

                switch (type)
                {
                    case "model":
                        Stack<Model> models = new Stack<Model>();

                        sw.Start();

                        for (int i = 0; i < max; i++)
                        {
                            Model model = Display.Chemistry as Model;
                            models.Push(model.Clone());
                        }
                        for (int i = 0; i < max; i++)
                        {
                            Model model = models.Pop();
                            Display.Chemistry = model;
                        }

                        sw.Stop();
                        break;

                    case "cml":
                        Stack<string> cmlModels = new Stack<string>();

                        sw.Start();

                        for (int i = 0; i < max; i++)
                        {
                            Model model = Display.Chemistry as Model;
                            cmlModels.Push(cc.Export(model));
                        }
                        for (int i = 0; i < max; i++)
                        {
                            Model model = cc.Import(cmlModels.Pop());
                            Display.Chemistry = model;
                        }

                        sw.Stop();
                        break;
                }
                Debug.WriteLine($"Push/Pop {max} operations took {sw.ElapsedMilliseconds} milliseconds.");
            }
        }

        private void Undo_Click(object sender, EventArgs e)
        {
            Model m = _undoStack.Pop();
            Debug.WriteLine($"Popped F: {m.ConciseFormula} BL: {m.MeanBondLength} from Undo Stack");

            Model c = Display.Chemistry as Model;
            Model clone = c.Clone();
            clone.RescaleForCml();

            Debug.WriteLine($"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength} onto Redo Stack");
            _redoStack.Push(clone);

            ShowChemistry($"Undo -> {m.ConciseFormula}", m);
        }

        private void Redo_Click(object sender, EventArgs e)
        {
            Model m = _redoStack.Pop();
            Debug.WriteLine($"Popped F: {m.ConciseFormula} BL: {m.MeanBondLength} from Redo Stack");

            Model c = Display.Chemistry as Model;
            Model clone = c.Clone();
            clone.RescaleForCml();

            Debug.WriteLine($"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength} onto Undo Stack");
            _undoStack.Push(clone);

            ShowChemistry($"Redo -> {m.ConciseFormula}", m);
        }

        private void ListStacks()
        {
            if (_undoStack.Any())
            {
                Debug.WriteLine("Undo Stack");
                foreach (var model in _undoStack)
                {
                    Debug.WriteLine($"{model.ConciseFormula} [{model.GetHashCode()}]");
                }
            }
            if (_redoStack.Any())
            {
                Debug.WriteLine("Redo Stack");
                foreach (var model in _redoStack)
                {
                    Debug.WriteLine($"{model.ConciseFormula} [{model.GetHashCode()}]");
                }
            }
        }
    }
}