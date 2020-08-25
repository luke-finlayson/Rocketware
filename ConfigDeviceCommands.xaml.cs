using System.Windows;

namespace Rocketware
{
    /// <summary>
    /// Interaction logic for ConfigSerialCommands.xaml
    /// </summary>
    public partial class ConfigDeviceCommands : Window
    {
        public ConfigDeviceCommands(string[] commandArray)
        {
            InitializeComponent();

            textBoxLaunch.Text = commandArray[0];
            textBoxAbort.Text = commandArray[1];
            textBoxDeployParachute.Text = commandArray[2];

            textBoxAux1.Text = commandArray[3];
            textBoxAux2.Text = commandArray[4];
            textBoxAux3.Text = commandArray[5];
        }

        public string[] CommandArray
        {
            get
            {
                string[] commandArray = { textBoxLaunch.Text, textBoxAbort.Text, textBoxDeployParachute.Text,
                    textBoxAux1.Text, textBoxAux2.Text, textBoxAux3.Text };

                return commandArray;
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
