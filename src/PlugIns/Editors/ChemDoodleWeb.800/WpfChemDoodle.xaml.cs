// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Chem4Word.Core.UI.Wpf;
using Control = System.Windows.Forms.Control;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.Editor.ChemDoodleWeb800
{

    // https://stopbyte.com/t/free-wpf-numeric-spinner-numericupdown/499
    // https://www.codeproject.com/Articles/509824/Creating-a-NumericUpDown-control-from-scratch

    /// <summary>
    /// Interaction logic for WpfChemDoodle.xaml
    /// </summary>
    public partial class WpfChemDoodle : UserControl
    {
        private string _currentMode = "Single";
        private bool _loading;

        public string Cml;

        public delegate void EventHandler(object sender, WpfEventArgs args);

        public event EventHandler OnButtonClick;

        public WpfChemDoodle()
        {
            _loading = true;
            InitializeComponent();
        }

        private void WpfChemDoodle_OnLoaded(object sender, RoutedEventArgs e)
        {
            _loading = true;
            string file = $@"{Environment.CurrentDirectory}\ChemDoodle\{_currentMode}.html";
            if (File.Exists(file))
            {
                _loading = true;
                Browser.Navigate(new Uri("file:///" + file.Replace(@"\", "/")));
            }
        }

        private void WebBrowser_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            var version = ExecuteJavaScript("GetVersion");
            var source = (HwndSource)PresentationSource.FromDependencyObject(Browser);
            if (source != null)
            {
                var host = (System.Windows.Forms.Integration.ElementHost)Control.FromChildHandle(source.Handle);
                var form = (Form) host?.TopLevelControl;
                if (form != null)
                {
                    form.Text = $"ChemDoodle Web {version}";
                }
            }
            _loading = false;
        }

        private void AddHydrogens_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                ExecuteJavaScript("AddExplicitHydrogens");
            }
        }

        private void RemoveHydrogens_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                ExecuteJavaScript("RemoveHydrogens");
            }
        }

        private void SwitchMode_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                if (_currentMode.Equals("Single"))
                {
                    _currentMode = "Multi";
                    SwitchMode.Content = "Single";
                    SwitchMode.ToolTip = "Switch to single molecule mode";
                }
                else
                {
                    _currentMode = "Single";
                    SwitchMode.Content = "Multi";
                    SwitchMode.ToolTip = "Switch to multi molecule mode";
                }

                string file = $@"{Environment.CurrentDirectory}\ChemDoodle\{_currentMode}.html";
                if (File.Exists(file))
                {
                    _loading = true;
                    Browser.Navigate(new Uri("file:///" + file.Replace(@"\", "/")));
                }
            }
        }

        private void ShowHydrogens_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                ExecuteJavaScript("ShowHydrogens", ShowHydrogens.IsChecked.Value);
            }
        }

        private void ShowColour_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                ExecuteJavaScript("AtomsInColour", ShowColour.IsChecked.Value);
            }
        }

        private void FlipStructures_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                ExecuteJavaScript("Flip");
            }
        }

        private void MirrorStructures_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                ExecuteJavaScript("Mirror");
            }
        }

        private void ShowCarbons_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                ExecuteJavaScript("ShowCarbons", ShowCarbons.IsChecked);
            }
        }

        private void SetBondLength(int value)
        {
            ExecuteJavaScript("ReScale", value);
            BondLength.Text = $"{value}";
        }

        private void BondLength_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_loading)
            {
                int bondLength;
                if (int.TryParse(BondLength.Text, out bondLength))
                {
                    bondLength = (int)Math.Round(bondLength / 5.0) * 5;
                    if (bondLength < 5 || bondLength > 95)
                    {
                        bondLength = 20;
                    }
                    SetBondLength(bondLength);
                }
                else
                {
                    SetBondLength(20);
                }
            }
        }

        private void IncreaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                int bondLength;
                if (int.TryParse(BondLength.Text, out bondLength))
                {
                    if (bondLength < 95)
                    {
                        bondLength += 5;
                        SetBondLength(bondLength);
                    }
                }
            }
        }

        private void DecreaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_loading)
            {
                int bondLength;
                if (int.TryParse(BondLength.Text, out bondLength))
                {
                    if (bondLength > 5)
                    {
                        bondLength -= 5;
                        SetBondLength(bondLength);
                    }
                }
            }
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = "";
            args.Button = "OK";

            OnButtonClick?.Invoke(this, args);
            throw new NotImplementedException();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = "";
            args.Button = "Cancel";

            OnButtonClick?.Invoke(this, args);
            throw new NotImplementedException();
        }

        private object ExecuteJavaScript(string functionName, params object[] args)
        {
            return Browser.InvokeScript(functionName, args);
        }
    }
}
