// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using Chem4Word.Core.UI.Wpf;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for CmlEditor.xaml
    /// </summary>
    public partial class CmlEditor : UserControl
    {
        public delegate void EventHandler(object sender, WpfEventArgs args);

        public event EventHandler OnButtonClick;

        public CmlEditor()
        {
            InitializeComponent();
        }

        public CmlEditor(string cml)
        {
            InitializeComponent();
            cmlText.Text = cml;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WpfEventArgs args = new WpfEventArgs();
            args.OutputValue = cmlText.Text;
            args.Button = "OK";

            OnButtonClick?.Invoke(this, args);
        }
    }
}