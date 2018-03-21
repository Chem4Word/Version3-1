// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using Chem4Word.Model.Converters;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : UserControl
    {
        public static readonly DependencyProperty SelectedAtomOptionProperty = DependencyProperty.Register("SelectedAtomOption", typeof(AtomOption), typeof(Editor), new PropertyMetadata(default(AtomOption)));
        public static readonly DependencyProperty SliderVisibilityProperty = DependencyProperty.Register("SliderVisibility", typeof(Visibility), typeof(Editor), new PropertyMetadata(default(Visibility)));

        public delegate void EventHandler(object sender, WpfEventArgs args);

        public event EventHandler OnOkButtonClick;
        // ToDo: Remove this
        private string _cml;

        /// <summary>
        /// See http://drwpf.com/blog/2007/10/05/managing-application-resources-when-wpf-is-hosted/
        /// </summary>
        public Editor(string cml)
        {
            _cml = cml;

            EnsureApplicationResources();
            InitializeComponent();
        }

        public bool ShowSave
        {
            get { return (bool)GetValue(ShowSaveProperty); }
            set { SetValue(ShowSaveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowSave.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSaveProperty =
            DependencyProperty.Register("ShowSave", typeof(bool), typeof(Editor), new PropertyMetadata(false));


        private void EnsureApplicationResources()
        {
            if (Application.Current == null)
            {
                // create the Application object
                new Application();

                // merge in your application resources
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/ACMEResources.xaml",
                            UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/AdornerBrushes.xaml",
                            UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/BondBrushes.xaml",
                            UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/Brushes.xaml",
                            UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/ControlStyles.xaml",
                            UriKind.Relative)) as ResourceDictionary);
            }
        }

        public BondOption SelectedBondOption
        {
            get { return (BondOption)GetValue(SelectedBondOptionProperty); }
            set { SetValue(SelectedBondOptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedBondOption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedBondOptionProperty =
            DependencyProperty.Register("SelectedBondOption", typeof(BondOption), typeof(Editor), new FrameworkPropertyMetadata(new PropertyChangedCallback(BondOptionChanged)));

        private static void BondOptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
        }

        public AtomOption SelectedAtomOption
        {
            get { return (AtomOption) GetValue(SelectedAtomOptionProperty); }
            set { SetValue(SelectedAtomOptionProperty, value); }
        }

        public Visibility SliderVisibility
        {
            get { return (Visibility) GetValue(SliderVisibilityProperty); }
            set { SetValue(SliderVisibilityProperty, value); }
        }


        private void EventSetter_OnHandler(object sender, RoutedEventArgs e)
        {
        }

        private void RingDropdown_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void RingSelButton_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void ACMEControl_Loaded(object sender, RoutedEventArgs e)
        {
            // ToDo: Load into initial model
            CMLConverter cc = new CMLConverter();
            DrawingArea.Chemistry = cc.Import(_cml);
        }

        private void AtomCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DrawingArea_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = _cml;
            args.Button = "SAVE";

            OnOkButtonClick?.Invoke(this, args);
        }
    }
}