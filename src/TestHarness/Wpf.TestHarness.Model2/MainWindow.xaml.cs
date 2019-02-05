// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using Microsoft.Win32;

namespace Wpf.TestHarness.Model2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Model _myModel;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadModel(string fileName)
        {
            void LoadTreeNode(Molecule modelMolecule, ChemItem root = null)
            {
                ChemItem parentNode;
                parentNode = new ChemItem();
                parentNode.Header = modelMolecule.ToString();
                parentNode.Tag = "Molecule";
                parentNode.Chemistry = modelMolecule;
                if (root == null)
                {
                    //FileInfo fi = new FileInfo(fileName);
                    //parentNode = treeView1.Nodes.Add(modelMolecule.Path, fi.Name + ": " + modelMolecule.ToString());
                    
                    MoleculeTreeView.Items.Add(parentNode);
                }
                else
                {
                    root.Items.Add(parentNode);
                }
                foreach (Atom atom in modelMolecule.Atoms.Values)
                {
                    var atomItem = new ChemItem();
                    atomItem.Header = atom.ToString();
                    atomItem.Chemistry = atom;
                    atomItem.Tag = "Atom";
                    parentNode.Items.Add(atomItem);
                }

                foreach (Bond bond in modelMolecule.Bonds)
                {
                    var bondItem = new ChemItem();
                    bondItem.Header = bond.ToString();
                    bondItem.Tag = "Bond";
                    bondItem.Chemistry = bond;
                    parentNode.Items.Add(bondItem);
                }

                foreach (Ring r in modelMolecule.Rings)
                {
                    var ringNode = new ChemItem();
                    ringNode.Header = r.ToString();
                    parentNode.Items.Add(ringNode);
                    ringNode.Tag = "Ring";
                    foreach (Atom a in r.Atoms)
                    {
                        var atomItem = new ChemItem();
                        atomItem.Header = a.Id;
                        atomItem.Tag = "Atom";
                        ringNode.Items.Add(atomItem);
                    }
                }
                foreach (var childMol in modelMolecule.Molecules.Values)
                {
                    LoadTreeNode(childMol, parentNode);
                }
            }

            MoleculeTreeView.Items.Clear();

            var converter = new CMLConverter();
            using (StreamReader sr = new StreamReader(fileName))
            {
                Stopwatch sw = new Stopwatch();
                _myModel= converter.Import(sr.ReadToEnd());
                ConnectModelEvents();
                sw.Stop();
                //MessageBox.Show($"Converting took {sw.ElapsedMilliseconds}");

                foreach (var modelMolecule in _myModel.Molecules.Values)
                {
                    sw.Reset();

                    //MessageBox.Show($"Rebuilding rings took {sw.ElapsedMilliseconds}");
                    //MessageBox.Show($"Ring count= {modelMolecule.Rings.Count}");
                    LoadTreeNode(modelMolecule);
                }
            }

        }

        private void ConnectModelEvents()
        {
            _myModel.AtomsChanged += Model_AtomsChanged;
            _myModel.MoleculesChanged += Model_MoleculesChanged;
            _myModel.BondsChanged += Model_BondsChanged;
            _myModel.PropertyChanged += Model_PropertyChanged;
        }

       

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "CML Files|*.CML";
            ofd.FilterIndex = 0;
            if (ofd.ShowDialog()==true)
            {
                LoadModel(ofd.FileName);
            }
        }
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Bond)

            {
                LogBox.AppendText($"Bond {(sender as Bond).Path} property {e.PropertyName} changed.\n");
            }
        }

        private void Model_MoleculesChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {

                    LogBox.AppendText($"Molecule {(item as Molecule)} added. \n");
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    LogBox.AppendText($"Molecule {(item as Molecule)} removed. \n");
                }
            }
        }

        private void Model_BondsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (sender is Molecule m)
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        
                        LogBox.AppendText($"Bond {(item as Bond)} added. \n");
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        LogBox.AppendText($"Bond {(item as Bond)} removed. \n");
                    }
                }
            }
        }

        private void Model_AtomsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
          
            if (sender is Molecule m)
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {

                        LogBox.AppendText($"Atom {(item as Atom)} added. \n");
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        LogBox.AppendText($"Atom {(item as Atom)} removed. \n");
                    }
                }
            }
        
        }



        private void MoleculeTreeView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //var selitem = MoleculeTreeView.InputHitTest(e.GetPosition(MoleculeTreeView));

            //if (e.Source is TreeViewItem)
            //{
            //    ChemItem selectedItem = (sender as ChemItem);

            //    if (selectedItem?.Chemistry is Bond bond)
            //    {
            //        (bond.Parent)?.RemoveBond(bond);
            //    }
            //}
        }

        private void DelButton_Click(object sender, RoutedEventArgs e)
        {
            ChemItem selectedItem = MoleculeTreeView.SelectedItem as ChemItem;

            if (selectedItem?.Chemistry is Bond bond)
            {
                if (bond.Parent != null)
                {
                    bond.Parent.RemoveBond(bond);
                }
            }
        }
    }
}
