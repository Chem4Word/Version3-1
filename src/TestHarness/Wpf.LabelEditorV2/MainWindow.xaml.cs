// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Chem4Word.ACME.Controls;

namespace Wpf.LabelEditor
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            EditLabelsControl.Used1D = new List<string>
            {
                "m5.f0:<<Guid>>",
                "m5.f2:<<Guid>>",
                "m5.n2:<<Guid>>"
            };
            EditLabelsControl.PopulateTreeView(GetCmlFile("AMC Mixture.cml"));
        }

        private string GetCmlFile(string fileName)
        {
            string cml = String.Empty;

            var folder = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(folder, "Data", fileName);
            if (File.Exists(path))
            {
                cml = File.ReadAllText(path);
            }

            return cml;
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            //EditLabelsControl.Used1D = new List<string>
            //{
            //    "m5.f0:<<Guid>>",
            //    "m5.f2:<<Guid>>",
            //    "m5.n2:<<Guid>>"
            //};
            //EditLabelsControl.PopulateTreeView(GetCmlFile("AMC Mixture.cml"));
        }
    }
}
