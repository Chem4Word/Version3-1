// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

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

        public Editor()
        {
            InitializeComponent();
        }

        public Editor(string cml)
        {
            InitializeComponent();
        
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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //WpfEventArgs args = new WpfEventArgs();
            //args.OutputValue = cmlText.Text;
            //args.Button = "OK";

            //OnOkButtonClick?.Invoke(this, args);
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
          
        }
    }
}