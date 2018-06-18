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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Ok";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void CancelButton_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.Button = "Cancel";
            args.OutputValue = "";

            OnButtonClick?.Invoke(this, args);
        }

        private void SettingsControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            var imageStream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "Preferences.png");
            if (imageStream != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = imageStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                EditorSettingsButtonImage.Source = bitmap;
                RendererSettingsButtonImage.Source = bitmap;
                SearcherSettingsButtonImage.Source = bitmap;
            }
        }

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
    }
}