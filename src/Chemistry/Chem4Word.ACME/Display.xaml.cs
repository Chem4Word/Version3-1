// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.Core;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for Display.xaml
    /// </summary>
    public partial class Display : UserControl
    {
        public Display()
        {
            InitializeComponent();
        }

        #region Public Properties

        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(Display),
                                        new FrameworkPropertyMetadata(SystemColors.WindowBrush,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender));

        #region Chemistry (DependencyProperty)

        public object Chemistry
        {
            get { return (object)GetValue(ChemistryProperty); }
            set { SetValue(ChemistryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Chemistry.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChemistryProperty =
            DependencyProperty.Register("Chemistry", typeof(object), typeof(Display),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender |
                                                                      FrameworkPropertyMetadataOptions.AffectsArrange |
                                                                      FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                      ChemistryChanged));

        private static void ChemistryChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var view = source as Display;
            if (view == null)
            {
                return;
            }

            view.HandleDataContextChanged();
        }

        #endregion Chemistry (DependencyProperty)

        #region ChemistryWidth (DependencyProperty)

        public double ChemistryWidth
        {
            get { return (double)GetValue(ChemistryWidthProperty); }
            set { SetValue(ChemistryWidthProperty, value); }
        }

        public static readonly DependencyProperty ChemistryWidthProperty = DependencyProperty.Register(
            "ChemistryWidth", typeof(double), typeof(Display), new FrameworkPropertyMetadata(default(double),
                                                                                             FrameworkPropertyMetadataOptions
                                                                                                 .AffectsRender |
                                                                                             FrameworkPropertyMetadataOptions
                                                                                                 .AffectsArrange |
                                                                                             FrameworkPropertyMetadataOptions
                                                                                                 .AffectsMeasure));

        #endregion ChemistryWidth (DependencyProperty)

        #region ChemistryHeight (DependencyProperty)

        public double ChemistryHeight
        {
            get { return (double)GetValue(ChemistryHeightProperty); }
            set { SetValue(ChemistryHeightProperty, value); }
        }

        public static readonly DependencyProperty ChemistryHeightProperty = DependencyProperty.Register(
            "ChemistryHeight", typeof(double), typeof(Display), new FrameworkPropertyMetadata(default(double),
                                                                                              FrameworkPropertyMetadataOptions
                                                                                                  .AffectsRender |
                                                                                              FrameworkPropertyMetadataOptions
                                                                                                  .AffectsArrange |
                                                                                              FrameworkPropertyMetadataOptions
                                                                                                  .AffectsMeasure));

        #endregion ChemistryHeight (DependencyProperty)

        public bool HighlightActive
        {
            get { return (bool)GetValue(HighlightActiveProperty); }
            set { SetValue(HighlightActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighlightActive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightActiveProperty =
            DependencyProperty.Register("HighlightActive", typeof(bool), typeof(Display), new PropertyMetadata(true));

        public bool ShowGroups
        {
            get { return (bool)GetValue(ShowGroupsProperty); }
            set { SetValue(ShowGroupsProperty, value); }
        }

        public static readonly DependencyProperty ShowGroupsProperty =
            DependencyProperty.Register("ShowGroups", typeof(bool), typeof(Display), new PropertyMetadata(true));

        #endregion Public Properties

        #region Public Methods

        public void Clear()
        {
            var model = new Model();
            CurrentViewModel = new ViewModel(model);
            DrawChemistry(CurrentViewModel);
        }

        #endregion Public Methods

        #region Private Methods

        private void HandleDataContextChanged()
        {
            Model chemistryModel = null;

            if (Chemistry is string)
            {
                var data = Chemistry as string;
                if (!string.IsNullOrEmpty(data))
                {
                    if (data.StartsWith("<"))
                    {
                        var conv = new CMLConverter();
                        chemistryModel = conv.Import(data);
                        chemistryModel.EnsureBondLength(20, false);
                    }
                    if (data.Contains("M  END"))
                    {
                        var conv = new SdFileConverter();
                        chemistryModel = conv.Import(data);
                        chemistryModel.EnsureBondLength(20, false);
                    }
                }
            }
            else
            {
                if (Chemistry != null && !(Chemistry is Model))
                {
                    Debugger.Break();
                    throw new ArgumentException($"Object must be of type {nameof(Model)}.");
                }
                chemistryModel = Chemistry as Model;
                if (chemistryModel != null)
                {
                    chemistryModel.EnsureBondLength(20, false);
                }
            }

            //assuming we've got this far, we should have something we can draw
            if (chemistryModel != null)
            {
                if (chemistryModel.TotalAtomsCount > 0)
                {
                    // ToDo: second parameter to come from DisplayOptions when implemented
                    chemistryModel.RescaleForXaml(true, Constants.StandardBondLength);

                    CurrentViewModel = new ViewModel(chemistryModel);
                    DrawChemistry(CurrentViewModel);
                }
            }
        }

        private void DrawChemistry(ViewModel currentViewModel)
        {
            ChemCanvas.Chemistry = currentViewModel;
        }

        public ViewModel CurrentViewModel { get; set; }

        #endregion Private Methods

        #region Private EventHandlers

        /// <summary>
        /// Add this to the OnMouseLeftButtonDown attribute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElementOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dynamic clobberedElement = sender;
            UserInteractions.InformUser(clobberedElement.ID);
        }

        #endregion Private EventHandlers
    }
}