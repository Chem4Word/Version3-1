using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    // Source https://github.com/angelsix/fasetto-word
    // Source https://www.youtube.com/watch?v=jrgT-fbV2tM

    /// <summary>
    /// Interaction logic for PropertyEditor.xaml
    /// </summary>
    public partial class AtomPropertyEditor : UserControl
    {
        public int WindowMinimumWidth { get; set; } = 250;

        public int WindowMinimumHeight { get; set; } = 100;

        private DialogWindow mDialogWindow;
        private AtomPropertiesModel _atomPropertiesModel;

        public AtomPropertyEditor()
        {
            InitializeComponent();
        }

        public Task ShowDialog(AtomPropertiesModel atomPropertiesModel)
        {
            _atomPropertiesModel = atomPropertiesModel;

            // Create a task to await the dialog closing
            var tcs = new TaskCompletionSource<bool>();

            var mode = Application.Current.ShutdownMode;

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            mDialogWindow = new DialogWindow();
            mDialogWindow.ViewModel = _atomPropertiesModel;
            DataContext = _atomPropertiesModel.Atom;

            // Run on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    mDialogWindow.ViewModel.WindowMinimumWidth = WindowMinimumWidth;
                    mDialogWindow.ViewModel.WindowMinimumHeight = WindowMinimumHeight;
                    mDialogWindow.ViewModel.Title = string.IsNullOrEmpty(atomPropertiesModel.Title) ? "Atom Properties" : atomPropertiesModel.Title;

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
            _atomPropertiesModel.Save = true;
            mDialogWindow.Close();
        }
    }
}