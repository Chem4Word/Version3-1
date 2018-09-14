// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model.Converters.CML;
using Chem4Word.Model.Converters.Json;
using IChem4Word.Contracts;
using Ionic.Zip;
using Control = System.Windows.Forms.Control;
using Cursors = System.Windows.Forms.Cursors;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;

namespace Chem4Word.Editor.ChemDoodleWeb800
{
    /// <summary>
    /// Interaction logic for WpfChemDoodle.xaml
    /// </summary>
    public partial class WpfChemDoodle : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private string AppTitle = "Chem4Word Editor - Powered By ChemDoodle Web V";

        private string _currentMode = "Single";
        private bool _loading;

        private Model.Model _model;
        private Stopwatch _sw;

        public delegate void EventHandler(object sender, WpfEventArgs args);

        public event EventHandler OnButtonClick;

        private IChem4WordTelemetry _telemetry;
        private Options _userOptions;
        private string _cml;
        private string _productAppDataPath;

        public WpfChemDoodle()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _loading = true;
            InitializeComponent();
        }

        public WpfChemDoodle(IChem4WordTelemetry telemetry, string productAppDataPath, Options userOptions, string cml)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _loading = true;

            _telemetry = telemetry;
            _userOptions = userOptions;
            _cml = cml;
            _productAppDataPath = productAppDataPath;

            InitializeComponent();
        }

        private void WpfChemDoodle_OnLoaded(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            _loading = true;
            _sw.Start();

            var converter = new CMLConverter();
            _model = converter.Import(_cml);

            bool singleMolecule = _model.Molecules.Count == 1;

            DeployCdw800();
            SetupControls(singleMolecule);
            ShowCdw(singleMolecule);
        }

        private void ShowCdw(bool singleMolecule)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string htmlfile = "";
            if (singleMolecule)
            {
                htmlfile = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "ChemDoodleWeb.Single.html");
            }
            else
            {
                htmlfile = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "ChemDoodleWeb.Multi.html");
            }
            File.WriteAllText(Path.Combine(_productAppDataPath, "Editor.html"), htmlfile);

            long sofar = sw.ElapsedMilliseconds;

            _telemetry.Write(module, "Timing", $"Writing resources to disk took {sofar}ms");

            _telemetry.Write(module, "Information", "Starting browser");
            Browser.Navigate(Path.Combine(_productAppDataPath, "Editor.html"));
        }

        private void SetupControls(bool singleMolecule)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
        }

        private void DeployCdw800()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string otherVersion = Path.Combine(_productAppDataPath, "ChemDoodle-Web-702.txt");
            if (File.Exists(otherVersion))
            {
                _telemetry.Write(module, "Information", "Deleting CDW 702 resources from disk");
                File.Delete(otherVersion);
                DelTree(Path.Combine(_productAppDataPath, "ChemDoodleWeb"));
            }

            string markerFile = Path.Combine(_productAppDataPath, "ChemDoodle-Web-800.txt");
            if (!File.Exists(markerFile))
            {
                _telemetry.Write(module, "Information", "Writing resources to disk");
                File.WriteAllText(markerFile, "Delete this file to refresh ChemDoodle Web");

                Stream stream = ResourceHelper.GetBinaryResource(Assembly.GetExecutingAssembly(), "ChemDoodleWeb.ChemDoodleWeb_800.zip");

                // NB: Top level of zip file must be the folder ChemDoodleWeb
                using (ZipFile zip = ZipFile.Read(stream))
                {
                    zip.ExtractAll(_productAppDataPath, ExtractExistingFileAction.OverwriteSilently);
                }

                string cssfile = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "ChemDoodleWeb.Chem4Word.css");
                File.WriteAllText(Path.Combine(_productAppDataPath, "Chem4Word.css"), cssfile);

                string jsfile = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "ChemDoodleWeb.Chem4Word.js");
                File.WriteAllText(Path.Combine(_productAppDataPath, "Chem4Word.js"), jsfile);
            }

            sw.Stop();
            long sofar = sw.ElapsedMilliseconds;

            _telemetry.Write(module, "Timing", $"Writing resources to disk took {sofar}ms");
        }

        private void WebBrowser_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            var version = ExecuteJavaScript("GetVersion");
            var source = (HwndSource)PresentationSource.FromDependencyObject(Browser);
            if (source != null)
            {
                var host = (System.Windows.Forms.Integration.ElementHost)Control.FromChildHandle(source.Handle);
                var form = (Form) host?.TopLevelControl;
                if (form != null)
                {
                    form.Text = AppTitle + version;
                }
            }

            // Send JSON to ChemDoodle ...
            // ExecuteJavaScript("SetJSON", _tempJson, AverageBondLength);
            // ExecuteJavaScript("ReScale", nudBondLength.Value);
            // ExecuteJavaScript("ShowHydrogens", true);
            // ExecuteJavaScript("AtomsInColour", true);
            // ExecuteJavaScript("ShowCarbons", true);

            long sofar2 = _sw.ElapsedMilliseconds;
            _telemetry.Write(module, "Timing", $"ChemDoodle Web ready in {sofar2.ToString("#,##0", CultureInfo.InvariantCulture)}ms");

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            _loading = false;
        }

        private void AddHydrogens_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                ExecuteJavaScript("AddExplicitHydrogens");
            }
        }

        private void RemoveHydrogens_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                ExecuteJavaScript("RemoveHydrogens");
            }
        }

        private void SwitchMode_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                ExecuteJavaScript("ShowHydrogens", ShowHydrogens.IsChecked.Value);
            }
        }

        private void ShowColour_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                ExecuteJavaScript("AtomsInColour", ShowColour.IsChecked.Value);
            }
        }

        private void FlipStructures_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                ExecuteJavaScript("Flip");
            }
        }

        private void MirrorStructures_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                ExecuteJavaScript("Mirror");
            }
        }

        private void ShowCarbons_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            if (!_loading)
            {
                ExecuteJavaScript("ShowCarbons", ShowCarbons.IsChecked);
            }
        }

        private void SetBondLength(int value)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            ExecuteJavaScript("ReScale", value);
            BondLength.Text = $"{value}";
        }

        private void BondLength_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
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
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            WpfEventArgs args = new WpfEventArgs();
            // ToDo: Return cml
            args.OutputValue = "";
            args.Button = "OK";

            OnButtonClick?.Invoke(this, args);
            throw new NotImplementedException();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = "";
            args.Button = "Cancel";

            OnButtonClick?.Invoke(this, args);
            throw new NotImplementedException();
        }

        private object ExecuteJavaScript(string functionName, params object[] args)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            return Browser.InvokeScript(functionName, args);
        }
        private void DelTree(string sPath)
        {
            DelTree(sPath, null);
        }

        private void DelTree(string sPath, List<string> listOfFilesToIgnore)
        {
            DirectoryInfo di = new DirectoryInfo(sPath);
            DirectoryInfo[] subdirs = di.GetDirectories();
            FileInfo[] filesList = di.GetFiles();
            foreach (FileInfo f in filesList)
            {
                if (listOfFilesToIgnore == null)
                {
                    File.Delete(f.FullName);
                }
                else
                {
                    if (!listOfFilesToIgnore.Contains(f.FullName))
                    {
                        File.Delete(f.FullName);
                    }
                }
            }
            foreach (DirectoryInfo subdir in subdirs)
            {
                DelTree(Path.Combine(sPath, subdir.ToString()), listOfFilesToIgnore);
            }
        }
    }
}
