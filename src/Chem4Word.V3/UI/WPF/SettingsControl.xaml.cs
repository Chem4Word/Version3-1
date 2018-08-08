// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Core.UI.Wpf;
using IChem4Word.Contracts;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public event EventHandler OnButtonClick;

        public Options SystemOptions { get; set; }
        public Point TopLeft { get; set; }
        public bool Dirty { get; set; }

        private bool _loading;

        public SettingsControl()
        {
            _loading = true;

            InitializeComponent();
        }

        #region Form Load

        private void SettingsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            #region Load Images

            // Tab 1 - Plug Ins
            var imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Preferences.png");
            if (imageStream != null)
            {
                var bitmap = CreateImageFromStream(imageStream);

                EditorSettingsButtonImage.Source = bitmap;
                RendererSettingsButtonImage.Source = bitmap;
                SearcherSettingsButtonImage.Source = bitmap;
            }

            // Tab 4 - Libaray
            imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Gallery-Toggle.png");
            if (imageStream != null)
            {
                var bitmap = CreateImageFromStream(imageStream);
                ImportIntoLibraryButtonImage.Source = bitmap;
            }
            imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Gallery-Save.png");
            if (imageStream != null)
            {
                var bitmap = CreateImageFromStream(imageStream);
                ExportFromLibraryButtonImage.Source = bitmap;
            }
            imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Gallery-Delete.png");
            if (imageStream != null)
            {
                var bitmap = CreateImageFromStream(imageStream);
                EraseLibraryButtonImage.Source = bitmap;
            }

            // Tab 5 Maintenance
            imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "File-Open.png");
            if (imageStream != null)
            {
                var bitmap = CreateImageFromStream(imageStream);
                LibraryFolderButtonImage.Source = bitmap;
                SettingsFolderButtonImage.Source = bitmap;
                PlugInsFolderButtonImage.Source = bitmap;
            }

            #endregion Load Images

            #region Set Current Values

            if (SystemOptions != null)
            {
                LoadSettings();
            }

            #endregion Set Current Values

            _loading = false;
        }

        #endregion Form Load

        #region Bottom Buttons

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Ok";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Cancel";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void DefaultsButton_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            try
            {
                Forms.DialogResult dr = UserInteractions.AskUserOkCancel("Restore default settings");
                if (dr == Forms.DialogResult.OK)
                {
                    _loading = true;
                    Dirty = true;
                    SystemOptions.RestoreDefaults();
                    LoadSettings();
                    _loading = false;
                }
            }
            catch (Exception ex)
            {
                new ReportError(Globals.Chem4WordV3.Telemetry, TopLeft, module, ex).ShowDialog();
            }

        }

        #endregion Bottom Buttons

        #region Tab 1 Events

        private void SelectedEditorSettings_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            IChem4WordEditor editor = Globals.Chem4WordV3.GetEditorPlugIn(SelectEditorPlugIn.SelectedItem.ToString());
            editor.ProductAppDataPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            editor.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + Constants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + Constants.TopLeftOffset * 2));
        }

        private void SelectedRendererSettings_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            IChem4WordRenderer renderer = Globals.Chem4WordV3.GetRendererPlugIn(SelectRendererPlugIn.SelectedItem.ToString());
            renderer.ProductAppDataPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            renderer.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + Constants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + Constants.TopLeftOffset * 2));
        }

        private void SelectedSearcherSettings_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            IChem4WordSearcher searcher = Globals.Chem4WordV3.GetSearcherPlugIn(SelectSearcherPlugIn.SelectedItem.ToString());
            searcher.ProductAppDataPath = Globals.Chem4WordV3.AddInInfo.ProductAppDataPath;
            searcher.ChangeSettings(new Point(SystemOptions.WordTopLeft.X + Constants.TopLeftOffset * 2, SystemOptions.WordTopLeft.Y + Constants.TopLeftOffset * 2));
        }

        private void SelectEditorPlugIn_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                PlugInComboItem pci = SelectEditorPlugIn.SelectedItem as PlugInComboItem;
                SystemOptions.SelectedEditorPlugIn = pci?.Name;
                SelectedEditorPlugInDescription.Text = pci?.Description;
                IChem4WordEditor editor = Globals.Chem4WordV3.GetEditorPlugIn(pci.Name);
                SelectedEditorSettings.IsEnabled = editor.HasSettings;
                Dirty = true;
            }
        }

        private void SelectRenderer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                PlugInComboItem pci = SelectRendererPlugIn.SelectedItem as PlugInComboItem;
                SystemOptions.SelectedRendererPlugIn = pci?.Name;
                SelectedRendererDescription.Text = pci?.Description;
                IChem4WordRenderer renderer = Globals.Chem4WordV3.GetRendererPlugIn(pci.Name);
                SelectedRendererSettings.IsEnabled = renderer.HasSettings;
                Dirty = true;
            }
        }

        private void SelectSearcher_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

                PlugInComboItem pci = SelectSearcherPlugIn.SelectedItem as PlugInComboItem;
                SelectedSearcherDescription.Text = pci?.Description;
                IChem4WordSearcher searcher = Globals.Chem4WordV3.GetSearcherPlugIn(pci.Name);
                SelectedSearcherSettings.IsEnabled = searcher.HasSettings;
                Dirty = true;
            }
        }

        #endregion Tab 1 Events

        #region Tab 2 Events

        private void ChemSpiderWebServiceUri_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
                SystemOptions.ChemSpiderWebServiceUri = ChemSpiderWebServiceUri.Text;
                Dirty = true;
            }
        }

        private void ResolverServiceUri_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
                SystemOptions.ResolverServiceUri = ResolverServiceUri.Text;
                Dirty = true;
            }
        }

        #endregion Tab 2 Events

        #region Tab 3 Events

        private void TelemetryEnabled_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");
                SystemOptions.TelemetryEnabled = TelemetryEnabled.IsChecked.Value;
                Dirty = true;
            }
        }

        #endregion Tab 3 Events

        #region Tab 4 Events

        private void ImportIntoLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            Debugger.Break();
        }

        private void ExportFromLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            Debugger.Break();
        }

        private void EraseLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        #endregion Tab 4 Events

        #region Tab 5 Events

        private void SettingsFolder_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            Debugger.Break();
        }

        private void LibraryFolder_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            Debugger.Break();
        }

        private void PlugInsFolder_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            Globals.Chem4WordV3.Telemetry.Write(module, "Action", "Triggered");

            Debugger.Break();
        }

        #endregion Tab 5 Events

        #region Private methods

        private void LoadSettings()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            #region Tab 1

            SelectEditorPlugIn.Items.Clear();
            SelectRendererPlugIn.Items.Clear();
            SelectSearcherPlugIn.Items.Clear();
            SelectedEditorSettings.IsEnabled = false;
            SelectedRendererSettings.IsEnabled = false;
            SelectedSearcherSettings.IsEnabled = false;

            string selectedEditor = SystemOptions.SelectedEditorPlugIn;
            foreach (IChem4WordEditor editor in Globals.Chem4WordV3.Editors)
            {
                PlugInComboItem pci = new PlugInComboItem()
                {
                    Name = editor.Name,
                    Description = editor.Description
                };
                int item = SelectEditorPlugIn.Items.Add(pci);
                if (editor.Name.Equals(selectedEditor))
                {
                    SelectedEditorSettings.IsEnabled = editor.HasSettings;
                    SelectedEditorPlugInDescription.Text = editor.Description;
                    SelectEditorPlugIn.SelectedIndex = item;
                }
            }

            string selectedRenderer = SystemOptions.SelectedRendererPlugIn;
            foreach (IChem4WordRenderer renderer in Globals.Chem4WordV3.Renderers)
            {
                PlugInComboItem pci = new PlugInComboItem()
                {
                    Name = renderer.Name,
                    Description = renderer.Description
                };
                int item = SelectRendererPlugIn.Items.Add(pci);
                if (renderer.Name.Equals(selectedRenderer))
                {
                    SelectedRendererSettings.IsEnabled = renderer.HasSettings;
                    SelectedRendererDescription.Text = renderer.Description;
                    SelectRendererPlugIn.SelectedIndex = item;
                }
            }

            foreach (IChem4WordSearcher searcher in Globals.Chem4WordV3.Searchers.OrderBy(s => s.DisplayOrder))
            {
                PlugInComboItem pci = new PlugInComboItem()
                {
                    Name = searcher.Name,
                    Description = searcher.Description
                };
                int item = SelectSearcherPlugIn.Items.Add(pci);
                if (SelectSearcherPlugIn.Items.Count == 1)
                {
                    SelectedSearcherSettings.IsEnabled = searcher.HasSettings;
                    SelectedSearcherDescription.Text = searcher.Description;
                    SelectSearcherPlugIn.SelectedIndex = item;
                }
            }

            #endregion Tab 1

            #region Tab 2

            ChemSpiderWebServiceUri.Text = SystemOptions.ChemSpiderWebServiceUri;
            ResolverServiceUri.Text = SystemOptions.ResolverServiceUri;

            #endregion Tab 2

            #region Tab 3

            string betaValue = Globals.Chem4WordV3.ThisVersion.Root?.Element("IsBeta")?.Value;
            bool isBeta = betaValue != null && bool.Parse(betaValue);

            TelemetryEnabled.IsChecked = isBeta || SystemOptions.TelemetryEnabled;
            TelemetryEnabled.IsEnabled = !isBeta;
            if (!isBeta)
            {
                BetaInformation.Visibility = Visibility.Hidden;
            }

            #endregion Tab 3
        }

        private BitmapImage CreateImageFromStream(Stream stream)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        #endregion Private methods
    }
}