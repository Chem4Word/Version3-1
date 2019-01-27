// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters;
using Chem4Word.Model2.Helpers;
using Chem4Word.Telemetry;

namespace WinForms.TestHarness.Model2
{
    public partial class MainTestForm : Form
    {
        private TelemetryWriter _telemetry = new TelemetryWriter(true);

        public MainTestForm()
        {
            InitializeComponent();
        }

        private void Load_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("CML molecule files (*.cml)|*.cml");
            //sb.Append("|MDL molecule files (*.mol, *.sdf)|*.mol;*.sdf");
            //sb.Append("|All molecule files (*.mol, *.sdf, *.cml)|*.mol;*.sdf;*.cml");

            openFileDialog1.Title = "Open Structure";
            openFileDialog1.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            openFileDialog1.Filter = sb.ToString();
            openFileDialog1.FileName = "";
            openFileDialog1.ShowHelp = false;
            openFileDialog1.FilterIndex = 0;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = Path.GetFileName(openFileDialog1.FileName);
                _telemetry.Write("FlexForm.LoadStructure()", "Information", $"File: {filename}");

                LoadModel(openFileDialog1.FileName);
            }
        }

        private void LoadModel(string fileName)
        {
            try
            {
                var converter = new CMLConverter();
                using (StreamReader sr = new StreamReader(fileName))
                {
                    Stopwatch sw = new Stopwatch();
                    var model = converter.Import(sr.ReadToEnd());
                    sw.Stop();
                    //MessageBox.Show($"Converting took {sw.ElapsedMilliseconds}");

                    foreach (var modelMolecule in model.Molecules.Values)
                    {
                        sw.Reset();

                        //MessageBox.Show($"Rebuilding rings took {sw.ElapsedMilliseconds}");
                        //MessageBox.Show($"Ring count= {modelMolecule.Rings.Count}");
                        LoadTreeNode(modelMolecule);
                    }
                    model.AtomsChanged += Model_AtomsChanged;
                    model.BondsChanged += Model_BondsChanged;
                    model.MoleculesChanged += Model_MoleculesChanged;
                    model.PropertyChanged += Model_PropertyChanged;

                    int atoms = model.TotalAtomsCount;
                    int bonds = model.TotalBondsCount;
                    textBox1.AppendText($"Total Atoms Count is {atoms}, Total Bonds Count is {bonds}\n");
                    var list = new List<string>();
                    list.AddRange(model.GeneralErrors);
                    list.AddRange(model.AllErrors);
                    list.AddRange(model.AllWarnings);
                    textBox1.AppendText(string.Join(Environment.NewLine, list) + "\n");
                }
            }
            catch (Exception exception)
            {
                _telemetry.Write("FlexForm.LoadStructure()", "Exception", $"Exception: {exception.Message}");
                _telemetry.Write("FlexForm.LoadStructure()", "Exception(Data)", $"Exception: {exception}");
                MessageBox.Show(exception.StackTrace, exception.Message);
            }

            // Local function to allow recursive calling
            void LoadTreeNode(Molecule modelMolecule, TreeNode root = null)
            {
                TreeNode parentNode;
                if (root == null)
                {
                    //FileInfo fi = new FileInfo(fileName);
                    //parentNode = treeView1.Nodes.Add(modelMolecule.Path, fi.Name + ": " + modelMolecule.ToString());
                    parentNode = treeView1.Nodes.Add(modelMolecule.Path, modelMolecule.ToString());
                    textBox1.AppendText($"Molecule {modelMolecule.Path} added. \n");
                }
                else
                {
                    parentNode = root.Nodes.Add(modelMolecule.Path, modelMolecule.ToString());
                    textBox1.AppendText($"Molecule {modelMolecule.Path} added. \n");
                }
                parentNode.Tag = modelMolecule;

                foreach (Atom atom in modelMolecule.Atoms.Values)
                {
                    var res = parentNode.Nodes.Add(atom.Path, atom.ToString());
                    res.Tag = atom;
                    textBox1.AppendText($"Atom {atom.Path} added. \n");
                }

                foreach (Bond bond in modelMolecule.Bonds)
                {
                    var res = parentNode.Nodes.Add(bond.Path, bond.ToString());
                    res.Tag = bond;
                    textBox1.AppendText($"Bond {bond.Path} added. \n");
                }

                foreach (Ring r in modelMolecule.Rings)
                {
                    var ringnode = parentNode.Nodes.Add(r.GetHashCode().ToString(), $"Ring count {r.Atoms.Count}");
                    ringnode.Tag = r;
                    foreach (Atom a in r.Atoms)
                    {
                        ringnode.Nodes.Add(r.GetHashCode() + a.Id, a.Id);
                    }
                }
                foreach (var childMol in modelMolecule.Molecules.Values)
                {
                    LoadTreeNode(childMol, parentNode);
                }
            }

        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Bond)

            {
                textBox1.AppendText($"Bond {(sender as Bond).Path} property {e.PropertyName} changed.\n");
            }
        }

        private void Model_MoleculesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
        }

        private void Model_BondsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (sender is Molecule m)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        string key = ((Bond)item).Path;
                        var foundNode = treeView1.Nodes.Find(key, true);

                        treeView1.Nodes.Remove(foundNode[0]);
                        textBox1.AppendText($"Bond {foundNode[0].Tag} removed. \n");
                    }
                }
            }
        }

        private void Model_AtomsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (sender is Molecule m)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        string key = ((Atom)item).Path;
                        var foundNode = treeView1.Nodes.Find(key, true);

                        treeView1.Nodes.Remove(foundNode[0]);
                        textBox1.AppendText($"Atom {foundNode[0].Tag} removed. \n");
                    }
                }
            }
        }

        private void MainTestForm_Load(object sender, EventArgs e)
        {
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            switch (e.Node.Tag)
            {
                case Bond b:
                    b.Parent.RemoveBond(b);
                    break;

                case Atom a:
                    var bondList = a.Bonds.ToArray();
                    foreach (Bond aBond in bondList)
                    {
                        a.Parent.RemoveBond(aBond);
                    }

                    a.Parent.RemoveAtom(a);
                    break;

                default:
                    break;
            }
        }
    }
}
