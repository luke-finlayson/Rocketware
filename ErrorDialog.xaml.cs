using System.Windows;

namespace Rocketware
{
    /// <summary>
    /// Interaction logic for ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : Window
    {
        public ErrorDialog(string message)
        {
            InitializeComponent();

            labelMessage.Content = message;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
