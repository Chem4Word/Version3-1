// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using Chem4Word.Model;
using Chem4Word.Model.Converters.CML;
using Chem4Word.Model.Converters.MDL;
using Chem4Word.Telemetry;
using Chem4Word.ViewModel;
using WinFormsTestHarness;

namespace WinForms.TestHarness
{
    public partial class FlexForm : Form
    {
        private Stack<Model> _undoStack = new Stack<Model>();
        private Stack<Model> _redoStack = new Stack<Model>();

        private TelemetryWriter _telemetry = new TelemetryWriter(true);

        public FlexForm()
        {
            InitializeComponent();
        }

        private void LoadStructure_Click(object sender, EventArgs e)
        {
            try
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

                        if (model.MeanBondLength < 2.5 || model.MeanBondLength > 97.5)
                        {
                            model.ScaleToAverageBondLength(20);
                        }
                        _telemetry.Write("FlexForm.LoadStructure()", "Information", $"File: {filename}");
                        ShowChemistry(filename, model);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write("FlexForm.LoadStructure()", "Exception", $"Exception: {exception.Message}");
                _telemetry.Write("FlexForm.LoadStructure()", "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
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

        private void EditWithAcme_Click(object sender, EventArgs e)
        {
            try
            {
                Model model = Display.Chemistry as Model;
                if (model != null)
                {
                    Model clone = model.Clone();
                    clone.RescaleForCml();

                    CMLConverter cc = new CMLConverter();
                    EditorHost editorHost = new EditorHost(cc.Export(clone), "ACME");
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
            catch (Exception exception)
            {
                _telemetry.Write("FlexForm.EditWithAcme_Click()", "Exception", $"Exception: {exception.Message}");
                _telemetry.Write("FlexForm.EditWithAcme_Click()", "Exception(Data)", $"Exception: {exception}");
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
            EditWithAcme.Enabled = true;
            ShowCarbons.Enabled = true;
            RemoveAtom.Enabled = true;
            RandomElement.Enabled = true;
            EditCml.Enabled = true;
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
                newModel.RefreshMolecules();
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

                foreach (var mol in model.Molecules)
                {
                    mol.ConciseFormula = "";
                }

                model.RefreshMolecules();
                Information.Text = $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength}";
            }
        }

        private void RandomElement_Click(object sender, EventArgs e)
        {
            Model model = Display.Chemistry as Model;
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

                    foreach (var mol in model.Molecules)
                    {
                        mol.ConciseFormula = "";
                    }
                    model.RefreshMolecules();
                    Information.Text = $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength}";
                }
            }
        }

        private void FlexForm_Load(object sender, EventArgs e)
        {
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

        private void EditCml_Click(object sender, EventArgs e)
        {
            try
            {
                Model model = Display.Chemistry as Model;
                if (model != null)
                {
                    Model clone = model.Clone();
                    clone.RescaleForCml();

                    CMLConverter cc = new CMLConverter();
                    EditorHost editorHost = new EditorHost(cc.Export(clone), "CML");
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
            catch (Exception exception)
            {
                _telemetry.Write("FlexForm.EditCml_Click()", "Exception", $"Exception: {exception.Message}");
                _telemetry.Write("FlexForm.EditCml_Click()", "Exception(Data)", $"Exception: {exception}");
            }
        }
    }
}