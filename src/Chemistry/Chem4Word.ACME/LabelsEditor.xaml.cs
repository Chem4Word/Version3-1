using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for LabelsEditor.xaml
    /// </summary>
    public partial class LabelsEditor : UserControl
    {
        public Point TopLeft { get; set; }
        public List<string> Used1D { get; set; }
        public string Message { get; set; }
        public bool IsDirty { get; set; }

        private Model _model;
        private string _cml;

        public event EventHandler<WpfEventArgs> OnButtonClick;

        public LabelsEditor()
        {
            InitializeComponent();
        }

        public LabelsEditor(string cml) : this()
        {
            _cml = cml;
        }

        public Model Data
        {
            get
            {
                return _model;
            }
        }

        private void LabelsEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (!string.IsNullOrEmpty(_cml))
                {
                    PopulateTreeView(_cml);
                }
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Display.Clear();

            LoadNamesEditor(NamesGrid, null);
            LoadNamesEditor(FormulaGrid, null);
            LoadNamesEditor(LabelsGrid, null);

            var model = new Model();

            if (sender is TreeView treeView)
            {
                if (treeView.SelectedItem is TreeViewItem item)
                {
                    if (item.Tag is Model m)
                    {
                        Display.Chemistry = m.Copy();
                    }

                    if (item.Tag is Molecule thisMolecule)
                    {
                        //Debug.WriteLine($"Molecule {thisMolecule.Path} [{thisMolecule.ConciseFormula}] selected");

                        model = new Model();
                        var copy = thisMolecule.Copy();
                        model.AddMolecule(copy);
                        copy.Parent = model;

                        if (thisMolecule.Molecules.Count == 0)
                        {
                            LoadNamesEditor(NamesGrid, thisMolecule.Names);
                            LoadNamesEditor(FormulaGrid, thisMolecule.Formulas);
                        }
                        LoadNamesEditor(LabelsGrid, thisMolecule.Labels);
                    }
                }
            }

            Display.Chemistry = model;
        }

        public void PopulateTreeView(string cml)
        {
            _cml = cml;
            var cc = new CMLConverter();
            _model = cc.Import(_cml, Used1D);
            TreeView.Items.Clear();

            if (_model != null)
            {
                OverallConciseFormulaPanel.Children.Add(TextBlockFromFormula(_model.ConciseFormula));

                var root = new TreeViewItem
                {
                    Header = "Structure",
                    Tag = _model,
                    IsSelected = true
                };
                TreeView.Items.Add(root);
                root.IsExpanded = true;

                AddNodes(root, _model.Molecules.Values);
            }

            SetupNamesEditor(NamesGrid, "Add new Name", OnAddNameClick, "Alternative name for molecule");
            SetupNamesEditor(FormulaGrid, "Add new Formula", OnAddFormulaClick, "Alternative formula for molecule");
            SetupNamesEditor(LabelsGrid, "Add new Label", OnAddLabelClick, "Custom metadata");

            // Local Function to support recursion
            void AddNodes(TreeViewItem parent, IEnumerable<Molecule> molecules)
            {
                foreach (var molecule in molecules)
                {
                    var tvi = new TreeViewItem();

                    if (molecule.Atoms.Count == 0)
                    {
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };
                        stackPanel.Children.Add(TextBlockFromFormula(molecule.CalculatedFormulaOfChildren(), "Group"));
                        tvi.Header = stackPanel;
                        //tvi.Header = "Group " + molecule.CalculatedFormulaOfChildren();
                        //tvi.Header = molecule.ConciseFormula;
                        //tvi.Tag = molecule.Labels;
                        tvi.Tag = molecule;
                    }
                    else
                    {
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal
                        };
                        stackPanel.Children.Add(TextBlockFromFormula(molecule.ConciseFormula));
                        tvi.Header = stackPanel;
                        tvi.Tag = molecule;
                    }

#if DEBUG
                    tvi.ToolTip = molecule.Path;
#endif

                    tvi.IsExpanded = true;
                    parent.Items.Add(tvi);

                    molecule.Labels.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Labels)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }
                    molecule.Formulas.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Formulas)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }
                    molecule.Names.CollectionChanged += OnCollectionChanged;
                    foreach (var property in molecule.Names)
                    {
                        property.PropertyChanged += OnTextualPropertyChanged;
                    }

                    AddNodes(tvi, molecule.Molecules.Values);
                }
            }
        }

        private void OnAddFormulaClick(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem)
            {
                if (treeViewItem.Tag is Molecule molecule)
                {
                    molecule.Formulas.Add(new TextualProperty
                    {
                        Id = molecule.GetNextId(molecule.Formulas, "f"),
                        Type = CMLConstants.AttributeValueChem4WordFormula,
                        Value = "?",
                        CanBeDeleted = true
                    });
                }
            }
        }

        private void OnAddNameClick(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem)
            {
                if (treeViewItem.Tag is Molecule molecule)
                {
                    molecule.Names.Add(new TextualProperty
                    {
                        Id = molecule.GetNextId(molecule.Names, "n"),
                        Type = CMLConstants.AttributeValueChem4WordSynonym,
                        Value = "?",
                        CanBeDeleted = true
                    });
                }
            }
        }

        private void OnAddLabelClick(object sender, RoutedEventArgs e)
        {
            if (TreeView.SelectedItem is TreeViewItem treeViewItem)
            {
                if (treeViewItem.Tag is Molecule molecule)
                {
                    molecule.Labels.Add(new TextualProperty
                    {
                        Id = molecule.GetNextId(molecule.Labels, "l"),
                        Type = CMLConstants.AttributeValueChem4WordLabel,
                        Value = "?",
                        CanBeDeleted = true
                    });
                }
            }
        }

        private void OnTextualPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TextualProperty item in e.NewItems)
                {
                    item.PropertyChanged += OnTextualPropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (TextualProperty item in e.OldItems)
                {
                    item.PropertyChanged -= OnTextualPropertyChanged;
                }
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.Button = "CANCEL";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            var cmlConvertor = new CMLConverter();
            args.Button = "OK";
            args.OutputValue = cmlConvertor.Export(_model);

            //Clipboard.SetText(cmlConvertor.Export(_model));

            OnButtonClick?.Invoke(this, args);
        }

        private void SetupNamesEditor(NamesEditor namesEditor, string buttonCaption, RoutedEventHandler routedEventHandler, string toolTip)
        {
            namesEditor.AddButtonCaption.Text = buttonCaption;
            namesEditor.AddButton.ToolTip = toolTip;
            // Remove existing handler if present (NB: -= should never crash)
            namesEditor.AddButton.Click -= routedEventHandler;
            namesEditor.AddButton.Click += routedEventHandler;
            namesEditor.AddButton.IsEnabled = false;
        }

        private void LoadNamesEditor(NamesEditor namesEditor, ObservableCollection<TextualProperty> data)
        {
            namesEditor.AddButton.IsEnabled = data != null;
            namesEditor.NamesModel.ListOfNames = data;
        }

        // Copied from $\src\Chem4Word.V3\Navigator\FormulaBlock.cs
        // Ought to be made into common routine
        // ToDo: Refactor into common code ...

        private TextBlock TextBlockFromFormula(string formula, string prefix = null)
        {
            var textBlock = new TextBlock();

            if (!string.IsNullOrEmpty(prefix))
            {
                // Add in the new element
                Run run = new Run($"{prefix} ");
                textBlock.Inlines.Add(run);
            }

            var parts = FormulaHelper.Parts(formula);
            foreach (FormulaPart formulaPart in parts)
            {
                // Add in the new element
                Run atom = new Run(formulaPart.Atom);
                textBlock.Inlines.Add(atom);

                if (formulaPart.Count > 1)
                {
                    var subs = new Run(formulaPart.Count.ToString())
                    {
                        BaselineAlignment = BaselineAlignment.Subscript
                    };

                    subs.FontSize = subs.FontSize - 2;
                    textBlock.Inlines.Add(subs);
                }
            }

            return textBlock;
        }
    }
}