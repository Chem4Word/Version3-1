using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME
{
    // Source https://github.com/angelsix/fasetto-word
    // Source https://www.youtube.com/watch?v=jrgT-fbV2tM

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