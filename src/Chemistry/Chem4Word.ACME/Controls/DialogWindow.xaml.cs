using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME.Controls
{
    /// <summary>
    /// Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window
    {
        public DialogWindow()
        {
            InitializeComponent();
        }

        private BaseDialogModel _viewModel;

        public BaseDialogModel ViewModel
        {
            get => _viewModel;
            set
            {
                // Set new value
                _viewModel = value;

                // Update data context
                DataContext = _viewModel;
            }
        }

        private void DialogWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = ViewModel.Centre.X - ActualWidth / 2;
            Top = ViewModel.Centre.Y - ActualHeight / 2;
        }
    }
}
