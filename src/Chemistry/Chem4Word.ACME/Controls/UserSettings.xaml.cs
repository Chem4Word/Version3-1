// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2.Annotations;
using IChem4Word.Contracts;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    ///     Interaction logic for UserSettings.xaml
    /// </summary>
    public partial class UserSettings : UserControl, INotifyPropertyChanged
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public IChem4WordTelemetry Telemetry { get; set; }

        public Point TopLeft { get; set; }

        public event EventHandler<WpfEventArgs> OnButtonClick;

        private Options _options;

        public Options Options
        {
            get { return _options; }
            set
            {
                if (value != null)
                {
                    _options = value;

                    var model = new SettingsModel();
                    model.CurrentBondLength = (double)_options.BondLength;
                    model.ShowMoleculeGroups = _options.ShowMoleculeGroups;
                    SettingsModel = model;
                    DataContext = SettingsModel;
                }
            }
        }

        private SettingsModel _model;

        public SettingsModel SettingsModel
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        private bool _loading;

        public UserSettings()
        {
            _loading = true;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Defaults_OnClick(object sender, RoutedEventArgs e)
        {
            _options.RestoreDefaults();
            SettingsModel.CurrentBondLength = Options.BondLength;
            SettingsModel.ShowMoleculeGroups = Options.ShowMoleculeGroups;
            OnPropertyChanged(nameof(SettingsModel));
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Options.Dirty = false;
            WpfEventArgs args = new WpfEventArgs();
            args.Button = "CANCEL";
            args.OutputValue = "";
            OnButtonClick?.Invoke(this, args);
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            // Copy current model values to options before saving
            Options.BondLength = (int)SettingsModel.CurrentBondLength;
            Options.ShowMoleculeGroups = SettingsModel.ShowMoleculeGroups;
            FileUtils.SaveAcmeSettings(Options, Telemetry, TopLeft);
            Options.Dirty = false;

            WpfEventArgs args = new WpfEventArgs();
            args.Button = "SAVE";
            args.OutputValue = "";
            OnButtonClick?.Invoke(this, args);
        }

        private void DefaultBondLength_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Options.BondLength = (int)SettingsModel.CurrentBondLength;
            if (_loading)
            {
                Options.Dirty = false;
            }
        }

        private void UserSettings_OnLoaded(object sender, RoutedEventArgs e)
        {
            _loading = false;
        }
    }
}