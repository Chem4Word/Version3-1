// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for EditLabelsControl.xaml
    /// </summary>
    public partial class EditLabelsControl : UserControl
    {
        // Potential fix for Splitter ignoring MinWidth of column / row
        //https://stackoverflow.com/questions/3967504/gridsplitter-with-min-constraints

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public Point TopLeft { get; set; }
        public string Cml { get; set; }
        public bool Dirty { get; set; }
        public List<string> Used1D { get; set; }
        public string Message { get; set; }

        public event EventHandler OnButtonClick;

        public EditLabelsControl()
        {
            InitializeComponent();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Save";
            // ToDo: Get from form data
            args.OutputValue = Cml;

            OnButtonClick?.Invoke(this, args);
        }

        private void EditLabelsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                // Load TreeView and display first molecule with any labels
                var cc = new CMLConverter();
                var model = cc.Import(Cml);
                if (model != null)
                {
                    OverallConciseFormula.Text = model.ConciseFormula;

                    var root = new TreeViewItem
                               {
                                   Header = "Structure"
                               };
                    TreeView.Items.Add(root);
                    root.IsExpanded = true;

                    AddNodes(root, model.Molecules.Values);
                }

                // Local Function to support recursion
                void AddNodes(TreeViewItem parent, IEnumerable<Molecule> molecules)
                {
                    foreach (var molecule in molecules)
                    {
                        var tvi = new TreeViewItem
                                  {
                                      Header = molecule.Path
                                  };
                        if (molecule.Atoms.Count > 0)
                        {
                            tvi.Tag = molecule;
                            tvi.Header += $" [{molecule.ConciseFormula}]";
                        }
                        parent.Items.Add(tvi);
                        tvi.IsExpanded = true;

                        var children = molecule.Molecules.Values;
                        AddNodes(tvi, children);
                    }
                }
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Display.Clear();

            var model = new Model();

            if (sender is TreeView treeView)
            {
                if (treeView.SelectedItem is TreeViewItem item)
                {
                    if (item.Tag is Molecule thisMolecule)
                    {
                        model = new Model();
                        var copy = thisMolecule.Copy();
                        model.AddMolecule(copy);
                        copy.Parent = model;

                        // ToDo: Attach Formula, Names and Labels of this Molecule to WPF UC in RHS of grid
                    }
                }
            }

            Display.Chemistry = model;
        }
    }
}