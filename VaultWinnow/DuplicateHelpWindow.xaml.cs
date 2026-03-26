using System.Windows;

namespace VaultWinnow
{
    public partial class DuplicateHelpWindow : Window
    {
        public DuplicateHelpWindow()
        {
            InitializeComponent();
        }

        private void BtnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
