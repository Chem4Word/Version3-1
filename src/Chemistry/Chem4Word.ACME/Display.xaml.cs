// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Drawing;
using Chem4Word.Core;
using Chem4Word.Model.Converters.CML;
using Chem4Word.Model.Converters.MDL;
using Chem4Word.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChemistryModel = Chem4Word.Model.Model;

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
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange |
                    FrameworkPropertyMetadataOptions.AffectsMeasure, ChemistryChanged));

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
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure));

        #endregion ChemistryWidth (DependencyProperty)

        #region ChemistryHeight (DependencyProperty)

        public double ChemistryHeight
        {
            get { return (double)GetValue(ChemistryHeightProperty); }
            set { SetValue(ChemistryHeightProperty, value); }
        }

        public static readonly DependencyProperty ChemistryHeightProperty = DependencyProperty.Register(
            "ChemistryHeight", typeof(double), typeof(Display), new FrameworkPropertyMetadata(default(double),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure));

        #endregion ChemistryHeight (DependencyProperty)


        /// <summary>
        /// 
        /// </summary>      


       



        #endregion Public Properties

        #region Private Methods

        private void HandleDataContextChanged()
        {
            ChemistryModel chemistryModel = null;

            if (Chemistry is string)
            {
                var data = Chemistry as string;
                if (!string.IsNullOrEmpty(data))
                {
                    if (data.StartsWith("<"))
                    {
                        var conv = new CMLConverter();
                        chemistryModel = conv.Import(data);
                    }
                    else if (data.Contains("M  END"))
                    {
                        var conv = new SdFileConverter();
                        chemistryModel = conv.Import(data);
                    }
                }
            }
            else
            {
                if (Chemistry != null && !(Chemistry is ChemistryModel))
                {
                    Debugger.Break();
                    throw new ArgumentException("Object must be of type 'Chem4Word.Model.Model'.");
                }
                chemistryModel = Chemistry as ChemistryModel;
            }
            //assuming we've got this far, we should have something we can draw
            if (chemistryModel != null)
            {
                if (chemistryModel.AllAtoms.Count > 0)
                {
                    chemistryModel.RescaleForXaml(true);

                    CurrentViewModel = new ViewModel.DisplayViewModel(chemistryModel);
                    DrawChemistry(CurrentViewModel);
                }
                else
                {
                    //Placeholder.DataContext = null;
                }
            }
            else
            {
                //Placeholder.DataContext = null;
            }
        }

        private void DrawChemistry(DisplayViewModel currentViewModel)
        {
            ChemCanvas.Chemistry = currentViewModel;
        }

        public DisplayViewModel CurrentViewModel { get; set; }

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

        #region Drawing Code

        // collection to keep track of the visuals
        private Dictionary<object, ChemicalVisual> chemicalVisuals;

        #endregion Drawing Code
    }
}