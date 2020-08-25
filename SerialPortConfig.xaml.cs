using System.IO.Ports;
using System.Windows;

namespace Rocketware
{
    /// <summary>
    /// Interaction logic for ConfigSerialPort.xaml
    /// </summary>
    public partial class SerialPortConfig : Window
    {
        public SerialPortConfig()
        {
            InitializeComponent();

            foreach (string port in SerialPort.GetPortNames())
            {
                comboBoxSerialLines.Items.Add(port);
            }
        }

        /// <summary>
        /// Return the baud rate set by the user
        /// </summary>
        public int baudRate
        {
            get
            {
                return int.Parse(textBoxBaudRate.Text);
            }
        }

        /// <summary>
        /// Return the serial port set by the user
        /// </summary>
        public string PortName
        {
            get 
            {
                if (comboBoxSerialLines.SelectedItem != null)
                {
                    return comboBoxSerialLines.SelectedItem.ToString();
                }
                else
                {
                    return "COM1";
                }
            }
        }

        /// <summary>
        /// Check that the baud rate is a valid integer before closing dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int.Parse(textBoxBaudRate.Text);

                this.DialogResult = true;
            }
            catch
            {
                ErrorDialog error = new ErrorDialog("Baud rate must be a valid interger.");
                error.ShowDialog();
            }
        }
    }
}
