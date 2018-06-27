// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Chem4Word.UI.WPF
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public event EventHandler OnButtonClick;

        public SettingsControl()
        {
            InitializeComponent();
        }

        #region Form Load

        private void SettingsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
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

            #endregion

            #region Set Current Values
            #endregion
        }

        #endregion Form Load

        #region Bottom Buttons

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Ok";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Cancel";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void DefaultsButton_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        #endregion Bottom Buttons

        #region Tab 1 Events

        private void SelectedEditorSettings_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void SelectedRendererSettings_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void SelectedSearcherSettings_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        #endregion Tab 1 Events

        #region Tab 2 Events

        private void ChemSpiderWebServiceUri_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Debugger.Break();
        }

        private void ChemSpiderRdfServiceUri_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Debugger.Break();
        }

        #endregion Tab 2 Events

        #region Tab 3 Events

        private void TelemetryEnabled_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        #endregion Tab 3 Events

        #region Tab 4 Events

        private void ImportIntoLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void ExportFromLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void EraseLibrary_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        #endregion Tab 4 Events

        #region Tab 5 Events

        private void SelectEditorPlugIn_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debugger.Break();
        }

        private void SelectRenderer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debugger.Break();
        }

        private void SelectSearcher_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debugger.Break();
        }

        private void SettingsFolder_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void LibraryFolder_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void PlugInsFolder_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        #endregion Tab 5 Events

        #region Private methods

        private BitmapImage CreateImageFromStream(Stream stream)
        {
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