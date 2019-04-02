using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    // Source https://github.com/angelsix/fasetto-word
    // Source https://www.youtube.com/watch?v=jrgT-fbV2tM

    /// <summary>
    /// Interaction logic for BondPropertyEditor.xaml
    /// </summary>
    public partial class BondPropertyEditor : UserControl
    {
        public int WindowMinimumWidth { get; set; } = 250;

        public int WindowMinimumHeight { get; set; } = 100;

        private DialogWindow mDialogWindow;
        private BondPropertiesModel _bondPropertiesModel;

        public BondPropertyEditor()
        {
            InitializeComponent();
        }

        public Task ShowDialog(BondPropertiesModel bondPropertiesModel)
        {
            _bondPropertiesModel = bondPropertiesModel;

            // Create a task to await the dialog closing
            var tcs = new TaskCompletionSource<bool>();

            var mode = Application.Current.ShutdownMode;

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            mDialogWindow = new DialogWindow();
            mDialogWindow.ViewModel = _bondPropertiesModel;
            DataContext = _bondPropertiesModel;

            // Run on UI thread
            Application.Current.Dispatcher.Invoke(() =>
                                                  {
                                                      try
                                                      {
                                                          mDialogWindow.ViewModel = bondPropertiesModel;

                                                          mDialogWindow.ViewModel.WindowMinimumWidth = WindowMinimumWidth;
                                                          mDialogWindow.ViewModel.WindowMinimumHeight = WindowMinimumHeight;
                                                          mDialogWindow.ViewModel.Title = string.IsNullOrEmpty(bondPropertiesModel.Title) ? "Bond Properties" : bondPropertiesModel.Title;

                                                          mDialogWindow.Content = this;

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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _bondPropertiesModel.Save = true;
            mDialogWindow.Close();
        }
    }
}