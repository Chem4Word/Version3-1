// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.ACME.Behaviors;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : UserControl, INotifyPropertyChanged
    {
        private EditViewModel _activeViewModel;

        public EditViewModel ActiveViewModel
        {
            get
            {
                return _activeViewModel;
            }
            set
            {
                _activeViewModel = value;
                OnPropertyChanged();
            }
        }

        public static readonly DependencyProperty SliderVisibilityProperty = DependencyProperty.Register("SliderVisibility", typeof(Visibility), typeof(Editor), new PropertyMetadata(default(Visibility)));

        public event EventHandler<WpfEventArgs> OnOkButtonClick;

        // This is used to store the cml until the editor is Loaded
        private string _cml;

        /// <summary>
        /// See http://drwpf.com/blog/2007/10/05/managing-application-resources-when-wpf-is-hosted/
        /// </summary>
        public Editor(string cml) : this()
        {
            _cml = cml;
        }

        public Editor()
        {
            EnsureApplicationResources();
            InitializeComponent();
        }

        public bool Dirty
        {
            get
            {
                if (ActiveViewModel == null)
                {
                    return false;
                }
                else
                {
                    return ActiveViewModel.Dirty;
                }
            }
        }

        public Model Data
        {
            get
            {
                if (ActiveViewModel == null)
                {
                    return null;
                }
                else
                {
                    Model model = ActiveViewModel.Model.Copy();
                    model.RescaleForCml();
                    return model;
                }
            }
        }

        public bool ShowSave
        {
            get { return (bool)GetValue(ShowSaveProperty); }
            set { SetValue(ShowSaveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowSave.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSaveProperty =
            DependencyProperty.Register("ShowSave", typeof(bool), typeof(Editor), new PropertyMetadata(true));

        private void EnsureApplicationResources()
        {
            if (Application.Current == null)
            {
                // create the Application object
                new Application();

                // Merge in your application resources
                // We need to do this for controls hosted in Winforms
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
                        new Uri("Chem4Word.ACME;component/Resources/Brushes.xaml",
                            UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/ControlStyles.xaml",
                            UriKind.Relative)) as ResourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(
                    Application.LoadComponent(
                        new Uri("Chem4Word.ACME;component/Resources/ZoomBox.xaml",
                            UriKind.Relative)) as ResourceDictionary);
            }
        }

        public AtomOption SelectedAtomOption
        {
            get { return (AtomOption)GetValue(SelectedAtomOptionProperty); }
            set { SetValue(SelectedAtomOptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedAtomOption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedAtomOptionProperty =
            DependencyProperty.Register("SelectedAtomOption", typeof(AtomOption), typeof(Editor), new PropertyMetadata(default(AtomOption)));

        public Visibility SliderVisibility
        {
            get { return (Visibility)GetValue(SliderVisibilityProperty); }
            set { SetValue(SliderVisibilityProperty, value); }
        }

        private void Popup_Click(object sender, RoutedEventArgs e)
        {
            RingButton.IsChecked = true;
        }

        private void RingDropdown_OnClick(object sender, RoutedEventArgs e)
        {
            RingPopup.IsOpen = true;
            RingPopup.Closed += (senderClosed, eClosed) =>
            {
            };
        }

        private void RingSelButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetCurrentRing(sender);
            ModeButton_OnChecked(RingButton, null);
            RingPopup.IsOpen = false;
        }

        private void SetCurrentRing(object sender)
        {
            Button selButton = sender as Button;
            var currentFace = new VisualBrush();
            currentFace.AutoLayoutContent = true;
            currentFace.Stretch = Stretch.Uniform;
            
            currentFace.Visual = selButton.Content as Visual;
            RingPanel.Background = currentFace;
            RingButton.Tag = selButton.Tag;
        }

        private void ACMEControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(_cml))
            {
                CMLConverter cc = new CMLConverter();
                Model tempModel = cc.Import(_cml);

                tempModel.RescaleForXaml(false);
                var vm = new EditViewModel(tempModel);
                ActiveViewModel = vm;

                ActiveViewModel.Model.CentreInCanvas(new Size(ChemCanvas.ActualWidth, ChemCanvas.ActualHeight));

                ChemCanvas.Chemistry = vm;

                vm.Loading = true;
                var mean = ActiveViewModel.Model.MeanBondLength / Globals.ScaleFactorForXaml;
                var average = Math.Round(mean / 5.0) * 5;
                vm.CurrentBondLength = average;
                vm.Loading = false;

                ScrollIntoView();
                BindControls(vm);
            }

            //refresh the ring button
            SetCurrentRing(BenzeneButton);
            //kludge:  need to do this to put the editor into the right mode after refreshing the ring button
            ModeButton_OnChecked(SelectionButton, new RoutedEventArgs());
        }

        public static T FindChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            // Confirm parent is valid.
            if (parent == null)
            {
                return null;
            }

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null)
                    {
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }

        /// <summary>
        /// Sets up data bindings between the dropdowns
        /// and the view model
        /// </summary>
        /// <param name="vm">EditViewModel for ACME</param>
        private void BindControls(EditViewModel vm)
        {
            vm.CurrentEditor = ChemCanvas;
        }

        /// <summary>
        /// Scrolls drawing into view
        /// </summary>
        private void ScrollIntoView()
        {
            //Debug.WriteLine($"ScrollIntoView; BoundingBox.Width: {ActiveViewModel.BoundingBox.Width}");
            //Debug.WriteLine($"ScrollIntoView; BoundingBox.Height: {ActiveViewModel.BoundingBox.Height}");
            Debug.WriteLine($"ScrollIntoView; DrawingArea.ExtentWidth: {DrawingArea.ExtentWidth}");
            Debug.WriteLine($"ScrollIntoView; DrawingArea.ExtentHeight: {DrawingArea.ExtentHeight}");
            Debug.WriteLine($"ScrollIntoView; DrawingArea.ViewportWidth: {DrawingArea.ViewportWidth}");
            Debug.WriteLine($"ScrollIntoView; DrawingArea.ViewportHeight: {DrawingArea.ViewportHeight}");

            DrawingArea.ScrollToHorizontalOffset((DrawingArea.ExtentWidth - DrawingArea.ViewportWidth) / 2);
            DrawingArea.ScrollToVerticalOffset((DrawingArea.ExtentHeight - DrawingArea.ViewportHeight) / 2);
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();

            Model result = ActiveViewModel.Model.Copy();
            result.RescaleForCml();

            CMLConverter conv = new CMLConverter();
            args.OutputValue = conv.Export(result);
            args.Button = "SAVE";

            OnOkButtonClick?.Invoke(this, args);
        }

        /// <summary>
        /// Sets the current behaviour of the editor to the
        /// behavior specified in the button's tag property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModeButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (ActiveViewModel != null)
            {
                if (ActiveViewModel.ActiveMode != null)
                {
                    ActiveViewModel.ActiveMode = null;
                }

                var radioButton = (RadioButton)sender;
               
                if (radioButton.Tag is BaseEditBehavior bh)
                {
                    ActiveViewModel.ActiveMode = bh;
                }
            }
        }

        private void RingButton_OnClick(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}