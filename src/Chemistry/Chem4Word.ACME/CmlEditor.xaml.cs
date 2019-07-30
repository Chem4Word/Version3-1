// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.UI.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for CmlEditor.xaml
    /// </summary>
    public partial class CmlEditor : UserControl
    {
        public event EventHandler<WpfEventArgs> OnButtonClick;

        public CmlEditor()
        {
            InitializeComponent();
        }

        public CmlEditor(string cml)
        {
            InitializeComponent();
            CmlText.Text = cml;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = CmlText.Text;
            args.Button = "OK";
            OnButtonClick?.Invoke(this, args);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = "";
            args.Button = "CANCEL";
            OnButtonClick?.Invoke(this, args);
        }
    }
}