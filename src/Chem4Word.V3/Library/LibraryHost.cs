// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Windows.Forms;
using Chem4Word.Core.UI.Forms;
using Word = Microsoft.Office.Interop.Word;

namespace Chem4Word.Library
{
    public partial class LibraryHost : UserControl
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private LibraryViewModel _libraryViewModel;

        public LibraryHost()
        {
            InitializeComponent();
        }

        public override void Refresh()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Word.Application app = Globals.Chem4WordV3.Application;
                using (new UI.WaitCursor())
                {
                    if (_libraryViewModel == null)
                    {
                        _libraryViewModel = new LibraryViewModel();
                    }
                    libraryView1.MainGrid.DataContext = _libraryViewModel;
                }
                app.System.Cursor = Word.WdCursorType.wdCursorNormal;
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}