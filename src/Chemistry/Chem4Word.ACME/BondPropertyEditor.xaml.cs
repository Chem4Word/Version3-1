using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Interaction logic for BondPropertyEditor.xaml
    /// </summary>
    public partial class BondPropertyEditor : UserControl
    {
        private DialogWindow mDialogWindow;

        public BondPropertyEditor()
        {
            InitializeComponent();
        }

        public Task ShowDialog()
        {
            // Create a task to await the dialog closing
            var tcs = new TaskCompletionSource<bool>();

            var mode = Application.Current.ShutdownMode;

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            mDialogWindow = new DialogWindow();

            // Run on UI thread
            Application.Current.Dispatcher.Invoke(() =>
                                                  {
                                                      try
                                                      {
                                                          // Show dialog

                                                          mDialogWindow.ShowDialog();
                                                      }
                                                      finally
                                                      {
                                                          // Let caller know we finished
                                                          tcs.TrySetResult(true);
                                                      }
                                                  });

            Application.Current.ShutdownMode = mode;

            return tcs.Task;
        }
    }
}
