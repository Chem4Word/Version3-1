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
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using Chem4Word.ACME;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Helpers;
using Chem4Word.Telemetry;

namespace WinForms.TestHarness
{
    public partial class FlexForm : Form
    {
        private Stack<Model> _undoStack = new Stack<Model>();
        private Stack<Model> _redoStack = new Stack<Model>();

        private TelemetryWriter _telemetry = new TelemetryWriter(true);

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private string _lastCml = null;

        public FlexForm()
        {
            InitializeComponent();
        }

        private void LoadStructure_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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

                    CMLConverter cmlConvertor = new CMLConverter();
                    SdFileConverter sdFileConverter = new SdFileConverter();

                    switch (fileType)
                    {
                        case ".mol":
                        case ".sdf":
                            model = sdFileConverter.Import(mol);
                            break;

                        case ".cml":
                        case ".xml":
                            model = cmlConvertor.Import(mol);
                            break;
                    }

                    if (model != null)
                    {
                        model.EnsureBondLength(20, false);

                        if (!string.IsNullOrEmpty(_lastCml))
                        {
                            var clone = cmlConvertor.Import(_lastCml);
                            Debug.WriteLine(
                                $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Stack");
                            _undoStack.Push(clone);
                        }

                        _lastCml = cmlConvertor.Export(model);

                        _telemetry.Write(module, "Information", $"File: {filename}");
                        ShowChemistry(filename, model);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        #region Disconnected Code - Please Keep for reference

        private void ChangeBackground_Click(object sender, EventArgs e)
        {
            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                DisplayHost.BackColor = colorDialog1.Color;
            }
        }

        private void SetCarbons(Model model, bool state)
        {
            foreach (var atom in model.GetAllAtoms())
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model model = Display.Chemistry as Model;
                if (model != null)
                {
                    Model copy = model.Copy();
                    //SetCarbons(copy, ShowCarbons.Checked);
                    copy.Refresh();
                    Debug.WriteLine($"Old Model: ({model.MinX}, {model.MinY}):({model.MaxX}, {model.MaxY})");
                    Debug.WriteLine($"New Model: ({copy.MinX}, {copy.MinY}):({copy.MaxX}, {copy.MaxY})");
                    Display.Chemistry = copy;
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void RemoveAtom_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model model = Display.Chemistry as Model;
                if (model != null)
                {
                    var allAtoms = model.GetAllAtoms();
                    if (model.GetAllAtoms().Any())
                    {
                        Molecule modelMolecule =
                            model.GetAllMolecules().FirstOrDefault(m => allAtoms.Any() && m.Atoms.Count > 0);
                        var atom = modelMolecule.Atoms.Values.First();
                        var bondList = atom.Bonds.ToList();
                        foreach (var neighbouringBond in bondList)
                        {
                            modelMolecule.RemoveBond(neighbouringBond);
                            neighbouringBond.OtherAtom(atom).UpdateVisual();
                            foreach (Bond bond in neighbouringBond.OtherAtom(atom).Bonds)
                            {
                                bond.UpdateVisual();
                            }
                        }

                        modelMolecule.RemoveAtom(atom);
                    }

                    model.Refresh();
                    Information.Text =
                        $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength.ToString("#,##0.00")}";
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void RandomElement_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model model = Display.Chemistry as Model;
                if (model != null)
                {
                    var allAtoms = model.GetAllAtoms();
                    if (allAtoms.Any())
                    {
                        var rnd = new Random(DateTime.Now.Millisecond);

                        var maxAtoms = allAtoms.Count;
                        int targetAtom = rnd.Next(0, maxAtoms);

                        var elements = Globals.PeriodicTable.Elements;
                        int newElement = rnd.Next(0, elements.Values.Max(v => v.AtomicNumber));
                        var x = elements.Values.FirstOrDefault(v => v.AtomicNumber == newElement);

                        if (x == null)
                        {
                            Debugger.Break();
                        }

                        allAtoms[targetAtom].Element = x as ElementBase;
                        if (x.Symbol.Equals("C"))
                        {
                            //allAtoms[targetAtom].ShowSymbol = ShowCarbons.Checked;
                        }

                        allAtoms[targetAtom].UpdateVisual();

                        foreach (Bond b in allAtoms[targetAtom].Bonds)
                        {
                            b.UpdateVisual();
                        }

                        model.Refresh();
                        Information.Text =
                            $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength.ToString("#,##0.00")}";
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        #endregion Disconnected Code - Please Keep for reference

        private void EditLabels_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
#if !DEBUG
            try
#endif
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    EditorHost editorHost = new EditorHost(_lastCml, "LABELS");
                    editorHost.ShowDialog(this);
                    if (editorHost.Result == DialogResult.OK)
                    {
                        CMLConverter cc = new CMLConverter();
                        var clone = cc.Import(_lastCml);
                        Debug.WriteLine(
                            $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Stack");
                        _undoStack.Push(clone);

                        Model m = cc.Import(editorHost.OutputValue);
                        m.Relabel(true);
                        _lastCml = cc.Export(m);

                        ShowChemistry($"Edited {m.ConciseFormula}", m);
                    }
                }
            }
#if !DEBUG
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
#endif
        }

        private void EditWithAcme_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
#if !DEBUG
            try
#endif
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    EditorHost editorHost = new EditorHost(_lastCml, "ACME");
                    editorHost.ShowDialog(this);
                    if (editorHost.Result == DialogResult.OK)
                    {
                        CMLConverter cc = new CMLConverter();
                        var clone = cc.Import(_lastCml);
                        Debug.WriteLine(
                            $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Stack");
                        _undoStack.Push(clone);

                        Model m = cc.Import(editorHost.OutputValue);
                        m.Relabel(true);
                        _lastCml = cc.Export(m);

                        ShowChemistry($"Edited {m.ConciseFormula}", m);
                    }
                }
            }
#if !DEBUG
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
#endif
        }

        private void ShowChemistry(string filename, Model model)
        {
            Display.Clear();

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

                    Information.Text =
                        $"Formula: {model.ConciseFormula} BondLength: {model.MeanBondLength.ToString("#,##0.00")}";

                    model.Refresh();

                    Display.Chemistry = model;
                    Debug.WriteLine($"FlexForm is displaying {model.ConciseFormula}");

                    EnableNormalButtons();
                    EnableUndoRedoButtons();
                }
            }
        }

        private void EnableNormalButtons()
        {
            EditWithAcme.Enabled = true;
            EditLabels.Enabled = true;
            EditCml.Enabled = true;

            ShowCml.Enabled = true;
            ClearChemistry.Enabled = true;
            SaveStructure.Enabled = true;

            ListStacks();
        }

        private List<ViewModel> StackToList(Stack<Model> stack)
        {
            List<ViewModel> list = new List<ViewModel>();
            foreach (var item in stack)
            {
                var model = item.Copy();
                model.Refresh();
                list.Add(new ViewModel(model));
            }

            return list;
        }

        private void EnableUndoRedoButtons()
        {
            Redo.Enabled = _redoStack.Count > 0;
            Undo.Enabled = _undoStack.Count > 0;
            UndoStack.ListOfDisplays.ItemsSource = StackToList(_undoStack);
            RedoStack.ListOfDisplays.ItemsSource = StackToList(_redoStack);
        }

        private void Undo_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model m = _undoStack.Pop();
                m.CheckIntegrity();
                Debug.WriteLine(
                    $"Popped F: {m.ConciseFormula} BL: {m.MeanBondLength.ToString("#,##0.00")} from Undo Stack");

                if (!string.IsNullOrEmpty(_lastCml))
                {
                    CMLConverter cc = new CMLConverter();
                    var copy = cc.Import(_lastCml);
                    _lastCml = cc.Export(m);

                    Debug.WriteLine(
                        $"Pushing F: {copy.ConciseFormula} BL: {copy.MeanBondLength.ToString("#,##0.00")} onto Redo Stack");
                    _redoStack.Push(copy);
                }

                ShowChemistry($"Undo -> {m.ConciseFormula}", m);
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void Redo_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Model m = _redoStack.Pop();
                Debug.WriteLine(
                    $"Popped F: {m.ConciseFormula} BL: {m.MeanBondLength.ToString("#,##0.00")} from Redo Stack");

                if (!string.IsNullOrEmpty(_lastCml))
                {
                    CMLConverter cc = new CMLConverter();
                    var clone = cc.Import(_lastCml);
                    _lastCml = cc.Export(m);

                    Debug.WriteLine(
                        $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Undo Stack");
                    _undoStack.Push(clone);
                }

                ShowChemistry($"Redo -> {m.ConciseFormula}", m);
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void ListStacks()
        {
            if (_undoStack.Any())
            {
                Debug.WriteLine("Undo Stack");
                foreach (var model in _undoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }

            if (_redoStack.Any())
            {
                Debug.WriteLine("Redo Stack");
                foreach (var model in _redoStack)
                {
                    Debug.WriteLine(
                        $"{model.ConciseFormula} [{model.GetHashCode()}] {model.MeanBondLength.ToString("#,##0.00")}");
                }
            }
        }

        private void EditCml_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    EditorHost editorHost = new EditorHost(_lastCml, "CML");
                    editorHost.ShowDialog(this);
                    if (editorHost.Result == DialogResult.OK)
                    {
                        CMLConverter cc = new CMLConverter();
                        var clone = cc.Import(_lastCml);
                        Debug.WriteLine(
                            $"Pushing F: {clone.ConciseFormula} BL: {clone.MeanBondLength.ToString("#,##0.00")} onto Stack");
                        _undoStack.Push(clone);

                        Model m = cc.Import(editorHost.OutputValue);
                        m.Relabel(true);
                        _lastCml = cc.Export(m);

                        ShowChemistry($"Edited {m.ConciseFormula}", m);
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void ShowCml_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                if (!string.IsNullOrEmpty(_lastCml))
                {
                    var f = new ShowCml();
                    f.Cml = _lastCml;
                    f.ShowDialog(this);
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void SaveStructure_Click(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                CMLConverter cmlConverter = new CMLConverter();
                Model m = cmlConverter.Import(_lastCml);
                m.CustomXmlPartGuid = "";

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CML molecule files (*.cml)|*.cml|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf";
                DialogResult dr = sfd.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    FileInfo fi = new FileInfo(sfd.FileName);
                    _telemetry.Write(module, "Information", $"Exporting to '{fi.Name}'");
                    string fileType = Path.GetExtension(sfd.FileName).ToLower();
                    switch (fileType)
                    {
                        case ".cml":
                            string temp = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                + Environment.NewLine
                                + cmlConverter.Export(m);
                            File.WriteAllText(sfd.FileName, temp);
                            break;

                        case ".mol":
                        case ".sdf":
                            // https://www.chemaxon.com/marvin-archive/6.0.2/marvin/help/formats/mol-csmol-doc.html
                            double before = m.MeanBondLength;
                            // Set bond length to 1.54 angstroms (Å)
                            m.ScaleToAverageBondLength(1.54);
                            double after = m.MeanBondLength;
                            _telemetry.Write(module, "Information", $"Structure rescaled from {before.ToString("#0.00")} to {after.ToString("#0.00")}");
                            SdFileConverter converter = new SdFileConverter();
                            File.WriteAllText(sfd.FileName, converter.Export(m));
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }

        private void ClearChemistry_Click(object sender, EventArgs e)
        {
            var cc = new CMLConverter();
            _undoStack.Push(cc.Import(_lastCml));
            _lastCml = "<cml></cml>";

            Display.Clear();
            EnableUndoRedoButtons();
        }

        private void FlexForm_Load(object sender, EventArgs e)
        {
            ShowGroupsBox.Checked = Display.ShowGroups;
        }

        private void ShowGroupsBox_CheckedChanged(object sender, EventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Display.ShowGroups = ShowGroupsBox.Checked;
            }
            catch (Exception exception)
            {
                _telemetry.Write(module, "Exception", $"Exception: {exception.Message}");
                _telemetry.Write(module, "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }
        }
    }
}