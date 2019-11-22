using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.ACME;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.JSON;
using Chem4Word.Model2.Helpers;
using Newtonsoft.Json;

namespace Wpf.FunctionalGroupEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _lastFunctionalGroup;
        private List<FunctionalGroup> _functionalGroups = Globals.FunctionalGroupsList;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Groups.Items.Clear();
            foreach (var functionalGroup in Globals.FunctionalGroupsList)
            {
                Groups.Items.Add(functionalGroup);
            }
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            InvalidateArrange();
        }

        private void Groups_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var jsonConvertor = new JSONConverter();

            if (Groups.SelectedItem is FunctionalGroup fg)
            {
                if (!string.IsNullOrEmpty(_lastFunctionalGroup))
                {
                    if (Editor.IsDirty)
                    {
                        var temp = Editor.ActiveViewModel.Model.Copy();
                        temp.RescaleForCml();
                        string expansion = jsonConvertor.Export(temp, true);
                        var fge = _functionalGroups.FirstOrDefault(f => f.Name.Equals(_lastFunctionalGroup));
                        fge.Expansion = expansion;
                    }
                }

                _lastFunctionalGroup = fg.ToString();

                if (fg.Expansion == null)
                {
                    var model = jsonConvertor.Import("{'a':[{'l':'" + StripBraces(fg.Symbol) + "','x':0,'y':0}]}");
                    Editor.SetModel(model);
                }
                else
                {
                    string groupJson = Newtonsoft.Json.JsonConvert.SerializeObject(fg.Expansion);
                    var model = jsonConvertor.Import(groupJson);
                    Editor.SetModel(model);
                }
            }
        }

        private string StripBraces(string value)
        {
            return value.Replace("{", "").Replace("}", "");
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (Editor.IsDirty)
            {
                var jsonConvertor = new JSONConverter();
                var temp = Editor.ActiveViewModel.Model.Copy();
                temp.RescaleForCml();
                string jsonString = jsonConvertor.Export(temp);
                var jc = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                var fg = _functionalGroups.FirstOrDefault(f => f.Name.Equals(_lastFunctionalGroup));
                if (fg != null)
                {
                    fg.Expansion = jc;
                }
            }

            List<FunctionalGroup> gg = new List<FunctionalGroup>();
            foreach (var group in _functionalGroups)
            {
                gg.Add(group);
            }

            string json2 = JsonConvert.SerializeObject(gg, Formatting.Indented,
                                                       new JsonSerializerSettings
                                                       {
                                                           DefaultValueHandling = DefaultValueHandling.Ignore
                                                       });
            Clipboard.SetText(json2);
            MessageBox.Show("Results on Clipboard !");
        }
    }
}