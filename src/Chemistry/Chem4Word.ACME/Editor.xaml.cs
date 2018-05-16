// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Converters;
using Chem4Word.ViewModel;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : UserControl
    {
        private EditViewModel _activeViewModel;

        public static readonly DependencyProperty SliderVisibilityProperty = DependencyProperty.Register("SliderVisibility", typeof(Visibility), typeof(Editor), new PropertyMetadata(default(Visibility)));

        public delegate void EventHandler(object sender, WpfEventArgs args);

        public event EventHandler OnOkButtonClick;

        // This is used to store the cml until the editor is Loaded
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
                //need to do this for controls hosted in Winforms
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

        private void EventSetter_OnHandler(object sender, RoutedEventArgs e)
        {
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
            Button selButton = sender as Button;
            RingButtonPath.Style = (selButton.Content as Path).Style;
            RingPopup.IsOpen = false;
        }

        private void ACMEControl_Loaded(object sender, RoutedEventArgs e)
        {
            CMLConverter cc = new CMLConverter();
            Model.Model tempModel = cc.Import(_cml);

            tempModel.RescaleForXaml(false);
            var vm = new EditViewModel(tempModel);
            _activeViewModel = vm;
            _activeViewModel.Model = tempModel;
            this.DataContext = vm;

            Canvas c = LocateCanvas();
            //Debug.WriteLine($"Canvas is {c.ActualWidth} x {c.ActualHeight}");
            //Debug.WriteLine($"Model WH is {_activeViewModel.Model.BoundingBox.Width} x {_activeViewModel.Model.BoundingBox.Height}");
            //Debug.WriteLine($"Model TL is {_activeViewModel.Model.BoundingBox.Top} x {_activeViewModel.Model.BoundingBox.Left}");
            double x = (c.ActualWidth - _activeViewModel.Model.BoundingBox.Width) / 2.0;
            double y = (c.ActualHeight - _activeViewModel.Model.BoundingBox.Height) / 2.0;
            _activeViewModel.Model.RepositionAll(-x, -y);

            //Debug.WriteLine($"Model WH is {_activeViewModel.Model.BoundingBox.Width} x {_activeViewModel.Model.BoundingBox.Height}");
            //Debug.WriteLine($"Model TL is {_activeViewModel.Model.BoundingBox.Top} x {_activeViewModel.Model.BoundingBox.Left}");

            ScrollIntoView();
            BindControls(vm);
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
        /// Centers any chemistry on the drawing area
        /// </summary>
        private void ScrollIntoView()
        {
            // Original From Clyde
            // -------------------
            //var boundingBox = _activeViewModel.Model.BoundingBox;
            //double hOffset = (boundingBox.Right - boundingBox.Left) / 2;
            //double vOffset = (boundingBox.Bottom - boundingBox.Top) / 2;

            // From Diagram Designer
            // ---------------------
            //double scale = 1;
            //double halfViewportHeight = DrawingArea.ViewportHeight / 2;
            //double halfViewportWidth = DrawingArea.ViewportWidth / 2;
            //double newVerticalOffset = ((DrawingArea.VerticalOffset + halfViewportHeight) * scale - halfViewportHeight);
            //double newHorizontalOffset = ((DrawingArea.HorizontalOffset + halfViewportWidth) * scale - halfViewportWidth);

            //DrawingArea.ScrollToHorizontalOffset(newHorizontalOffset);
            //DrawingArea.ScrollToVerticalOffset(newVerticalOffset);

            Canvas canvas = LocateCanvas();
            double newVerticalOffset = (canvas.ActualWidth - _activeViewModel.Model.BoundingBox.Width) / 2.0;
            double newHorizontalOffset = (canvas.ActualHeight - _activeViewModel.Model.BoundingBox.Height) / 2.0;

            DrawingArea.ScrollToHorizontalOffset(newVerticalOffset);
            DrawingArea.ScrollToVerticalOffset(newHorizontalOffset);
        }

        private Canvas LocateCanvas()
        {
            Canvas res = FindChild<Canvas>(DrawingArea);
            return res;
        }

        /// <summary>
        /// Sets up data bindings btween the dropdowns
        /// and the view model
        /// </summary>
        /// <param name="vm">EditViewModel for ACME</param>
        private void BindControls(ViewModel.EditViewModel vm)
        {
            Binding atomBinding = new Binding("SelectedAtomOption");
            atomBinding.Source = vm;
            AtomCombo.SetBinding(ComboBox.SelectedItemProperty, atomBinding);

            Binding bondBinding = new Binding("SelectedBondOption");
            bondBinding.Source = vm;

            BondCombo.SetBinding(ComboBox.SelectedItemProperty, bondBinding);

            vm.DrawingSurface = LocateCanvas();
        }

        private void AtomCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DrawingArea_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void BondLengthCombo_OnChange(object sender, RoutedEventArgs e)
        {
            BondLengthOption blo = BondLengthSelector.SelectedItem as BondLengthOption;
            if (blo != null)
            {
                if (Math.Abs(_activeViewModel.Model.XamlBondLength - blo.ChosenValue) > 2.5 * Globals.ScaleFactorForXaml)
                {
                    Debug.WriteLine($"Model WH is {_activeViewModel.Model.BoundingBox.Width} x {_activeViewModel.Model.BoundingBox.Height}");
                    Debug.WriteLine($"Model TL is {_activeViewModel.Model.BoundingBox.Top} x {_activeViewModel.Model.BoundingBox.Left}");
                    _activeViewModel.Model.ScaleToAverageBondLength(blo.ChosenValue);
                    Debug.WriteLine($"Model WH is {_activeViewModel.Model.BoundingBox.Width} x {_activeViewModel.Model.BoundingBox.Height}");
                    Debug.WriteLine($"Model TL is {_activeViewModel.Model.BoundingBox.Top} x {_activeViewModel.Model.BoundingBox.Left}");
                    ScrollIntoView();
                }
            }
        }

        private void ZoomFactorCombo_OnChange(object sender, RoutedEventArgs e)
        {
            // ToDo: Plumbing in place ready to use ...
            ComboBox cb = sender as ComboBox;
            ComboBoxItem cbi = cb?.SelectedItem as ComboBoxItem;
            string selected = cbi?.Content as string;
            switch (selected)
            {
                case "100%":
                    break;
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();

            Model.Model result = _activeViewModel.Model.Clone();
            result.RescaleForCml();

            CMLConverter conv = new CMLConverter();
            args.OutputValue = conv.Export(result);
            args.Button = "SAVE";

            OnOkButtonClick?.Invoke(this, args);
        }

        private void ModeButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_activeViewModel != null)
            {
                if (_activeViewModel.ActiveMode != null)
                {
                    _activeViewModel.ActiveMode = null;
                }

                var behavior = (Behavior)((sender as RadioButton).Tag);
                if (behavior != null)
                {
                    _activeViewModel.ActiveMode = behavior;
                }
            }
        }
    }
}