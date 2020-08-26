using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using HelixToolkit.Wpf;
using System.Windows.Threading;
using System.Windows.Media.Media3D;

namespace Rocketware
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Initial Setup
        // Serial related variables
        SerialPort _serialPort;
        bool _continue;
        Thread readThread;

        // Data related variables
        double heading;
        double pitch;
        double roll;
        double deltaAltitude;
        double temperature;
        double humidity;
        double pressure;
        double currentAltitude = 0;

        // Program config related variables
        bool useDeltaAltitude = true;
        string logSaveLocation;
        string[] commandArray = { "launch", "abort", "deploy-parachute", "aux1", "aux2", "aux3" };

        // 3D Related Variables
        Model3DGroup rocket;
        Point3D rocketPivot = new Point3D(0, 0, 350);
        RotateTransform3D rotateRocket;

        public MainWindow()
        {
            InitializeComponent();
            outputLog.Text = DateTime.Now.ToString("T") + ":> Logging started...\n";

            // Create a new SerialPort object with default values
            _serialPort = new SerialPort();

            if (SerialPort.GetPortNames().Length != 0)
            {
                // Get the user to specify the serial ports
                ConfigSerialPort();
            }
            else
            {
                // Display Message to user
                ErrorDialog error = new ErrorDialog("No serial ports detected. Make sure the device is plugged in.");
                error.ShowDialog();
            }

            // Create instance of ModelImporter to use when importing rocket model
            ModelImporter importer = new ModelImporter();
            // Create instance of grey material
            Material material = new DiffuseMaterial(new SolidColorBrush(Colors.Gray));
            // Set the material of the importer
            importer.DefaultMaterial = material;

            // Import rocket model
            rocket = importer.Load("Resources/rocket.obj");
            // Set content of model tag in ViewPort to rocket model
            model.Content = rocket;

            OutputToLog("Program Started. Awaiting Command.");
        }
        #endregion

        #region Button & Menu Item Click Events
        /// <summary>
        /// Call the method to exit the program [Menu Item]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        /// <summary>
        /// Call the method to open the serial connection [Menu Item]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            OpenSerial();
        }

        /// <summary>
        /// Call the method to close the serial connection [Menu Item]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            CloseSerial();
        }

        /// <summary>
        /// Call the method to clear all data fields [Menu Item]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        /// <summary>
        /// Call the method to close the program [Button]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        /// <summary>
        /// Call the method to open the serial connection [Button]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOpenSerial_Click(object sender, RoutedEventArgs e)
        {
            OpenSerial();
        }

        /// <summary>
        /// Call the method to close the serial connection [Button]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCloseSerial_Click(object sender, RoutedEventArgs e)
        {
            CloseSerial();
        }

        /// <summary>
        /// Call the method to clear all data fields [Button]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        /// <summary>
        /// Get the user to select a save location for the log [Button]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSaveLog_Click(object sender, RoutedEventArgs e)
        {
            // Create a new instance of the SaveFile dialog and set the filter
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Files|*.txt";

            // Check the the user pressed Save
            if (saveFileDialog.ShowDialog() == true)
            {
                // Set the save location to the one selected by the user
                logSaveLocation = saveFileDialog.FileName;
                OutputToLog("Log will be saved once program has been closed.");
                // Disable the [Save Log] button
                buttonSaveLog.IsEnabled = false;
            }
        }

        /// <summary>
        /// Toggle the altitude reading format when the checkbox is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            // Check the current state of the checkbox
            if (useDeltaAltitudeCheckBox.IsChecked != true)
            {
                // Update the relevant data
                labelAltitude.Content = "   Altitude:";
                useDeltaAltitude = false;
            }
            else
            {
                labelAltitude.Content = "Δ Altitude:";
                useDeltaAltitude = true;
            }
        }

        /// <summary>
        /// Call the method to configue the serial port [Menu Item]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            ConfigSerialPort();
        }

        /// <summary>
        /// Opens the dialog for the user to input updated command strings [Menu Item]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            // Create a new instance of the Command Config window
            ConfigDeviceCommands configWindow = new ConfigDeviceCommands(commandArray);

            // Show the dialog and get the result
            if (configWindow.ShowDialog() == true)
            {
                // Update the command array
                commandArray = configWindow.CommandArray;
                OutputToLog("Commands updated.");
            }
        }
        #endregion

        #region Device Commands
        /// <summary>
        /// Sends the launch command through serial
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLaunch_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Write(commandArray[0]);
            OutputToLog("\"Launch\" command sent");
        }

        /// <summary>
        /// Sends the abort command through serial
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAbort_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Write(commandArray[1]);
            OutputToLog("\"Abort\" command sent.");
        }

        /// <summary>
        /// Send the deploy parachute command through serial
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDeplyParachute_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Write(commandArray[2]);
            OutputToLog("\"Deploy Parachute\" command sent.");
        }

        /// <summary>
        /// Send the aux1 command through serial
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAux1_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Write(commandArray[3]);
            OutputToLog("\"Auxiliary (1)\" command sent.");
        }

        /// <summary>
        /// Send the aux2 command through serial
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAux2_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Write(commandArray[4]);
            OutputToLog("\"Auxiliary (2)\" command sent.");
        }

        /// <summary>
        /// Send the aux3 command through serial
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAux3_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.Write(commandArray[5]);
            OutputToLog("\"Auxiliary (3)\" command sent.");
        }
        #endregion

        #region Serial Methods
        /// <summary>
        /// Once started, reads data from the serial port
        /// </summary>
        void Read()
        {
            while (_continue)
            {
                try
                {
                    // Read the serial data
                    string inputData = _serialPort.ReadLine();
                    // Split the data into an array
                    string[] rawDataArray = inputData.Split('\t');
                    float[] inputDataArray = new float[7];

                    // Get rid of any blanks and unwanted chars in the serial data
                    int j = 0;
                    for (int i = 0; i < rawDataArray.Length; i++)
                    {
                        // Delete any blanks (and unwanted chars)
                        rawDataArray[i] = DeleteChar(' ', rawDataArray[i]);

                        // Make sure element still contains data
                        if (rawDataArray[i] != "")
                        {
                            // Append clean data to final array
                            inputDataArray[j] = float.Parse(rawDataArray[i]);
                            j++;
                        }
                    }

                    // Sort data
                    heading = inputDataArray[0];
                    pitch = inputDataArray[1];
                    roll = inputDataArray[2];
                    deltaAltitude = inputDataArray[3];
                    temperature = inputDataArray[4];
                    humidity = inputDataArray[5];
                    pressure = inputDataArray[6];

                    // Apply UI changes outside thread
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        UpdateTextBoxes();
                    }));
                }
                catch { }
            }
        }

        /// <summary>
        /// Converts the euler angles to axis-angle and thus rotates the rocket model then updates the data fields
        /// </summary>
        public void UpdateTextBoxes()
        {
            // Update position textboxes
            textBoxHeading.Text = heading + "°";
            textBoxPitch.Text = pitch + "°";
            textBoxRoll.Text = roll + "°";

            // Doing Math (ugh)
            heading = ConvertToRad(heading);
            pitch = ConvertToRad(pitch);
            roll = ConvertToRad(roll);

            // Solving for required variables
            double c1 = Math.Cos(heading / 2);
            double c2 = Math.Cos(pitch / 2);
            double c3 = Math.Cos(roll / 2);

            double s1 = Math.Sin(heading / 2);
            double s2 = Math.Sin(pitch / 2);
            double s3 = Math.Sin(roll / 2);

            // Solving for angle
            double angle = (2 * Math.Acos((c1 * c2 * c3) + (s1 * s2 * s3))) * (360 / (2 * Math.PI));
            // Solving for raw axis
            double rawX = (c1 * c2 * s3) - (s1 * s2 * c3);
            double rawY = (c1 * s2 * c3) - (s1 * c2 * s3);
            double rawZ = (s1 * c2 * c3) - (c1 * s2 * s3);
            // Normalizing axis
            double x = rawX / ((rawX * rawX) + (rawY * rawY) + (rawZ * rawZ));
            double y = rawY / ((rawX * rawX) + (rawY * rawY) + (rawZ * rawZ));
            double z = rawZ / ((rawX * rawX) + (rawY * rawY) + (rawZ * rawZ));

            // Configuring rotating tranformation
            rotateRocket = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(x, y, z), angle));
            rotateRocket.CenterX = rocketPivot.X;
            rotateRocket.CenterY = rocketPivot.Y;
            rotateRocket.CenterZ = rocketPivot.Z;

            // Applying Transformation
            rocket.Transform = rotateRocket;

            // Apply the altitude difference to the current altitude
            currentAltitude += deltaAltitude;
            // Check which altitude format to display
            if (useDeltaAltitude)
            {
                // Display change in altitude
                textBoxAltitude.Text = deltaAltitude + "m";
            }
            else
            {
                // Display total altitude displacement
                textBoxAltitude.Text = currentAltitude + "m";
            }

            textBoxTemperature.Text = temperature + "°C";
            textBoxHumidity.Text = "" + humidity;
            textBoxPressure.Text = pressure + "hPa";
        }

        /// <summary>
        /// Converts an angle in degrees to radians
        /// </summary>
        /// <param name="angle">The angle in degrees</param>
        /// <returns></returns>
        double ConvertToRad(double angle)
        {
            return ((2 * Math.PI) / 360) * angle;
        }

        /// <summary>
        /// Goes through a string and removes the specified character
        /// </summary>
        /// <param name="charToDelete">Specify the character to be removed</param>
        /// <param name="inputString">The string which the character will be removed from</param>
        /// <returns></returns>
        static string DeleteChar(char charToDelete, string inputString)
        {
            string outputString = "";

            foreach (char c in inputString)
            {
                if (c != charToDelete && c != '\r' && c != '\n')
                {
                    outputString += c;
                }
            }

            return outputString;
        }

        /// <summary>
        /// Attempts to end the serial connection and blow the thread to recoverable smithereens
        /// </summary>
        void CloseSerial()
        {
            _continue = false;
            if (readThread != null)
            {
                readThread.Join();
            }
            _serialPort.Close();

            ToggleCommands();

            OutputToLog("Serial port closed.");
        }

        /// <summary>
        /// Safetly closes the program
        /// </summary>
        void Exit()
        {
            CloseSerial();

            if (logSaveLocation != null)
            {
                File.WriteAllText(logSaveLocation, outputLog.Text);
                OutputToLog("Log saved at ." + logSaveLocation);
            }

            OutputToLog("PROGRAM TERMINATED. END OF LOG.");
            Close();
        }

        /// <summary>
        /// Clears all the data fields
        /// </summary>
        void Clear()
        {
            if (!_serialPort.IsOpen)
            {
                textBoxAltitude.Text = "-";
                textBoxHeading.Text = "-";
                textBoxHumidity.Text = "-";
                textBoxPitch.Text = "-";
                textBoxRoll.Text = "-";
                textBoxTemperature.Text = "-";
                textBoxPressure.Text = "-";

                currentAltitude = 0;

                OutputToLog("Cleared data fields.");
            }
            else
            {
                OutputToLog("Serial Port is still open, unable to clear data fields.");
            }
            outputLog.ScrollToEnd();
        }

        /// <summary>
        /// Attempts to open a serial connection and start the thread
        /// </summary>
        void OpenSerial()
        {
            try
            {
                // Open the serial port
                _serialPort.Open();
                _continue = true;
                readThread = new Thread(Read);
                readThread.Start();

                ToggleCommands();

                OutputToLog("Serial Port Opened.");
                OutputToLog("Reading Serial Data...");
            }
            catch (Exception ex)
            {
                OutputToLog("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Outputs a message to the log for the user to see
        /// </summary>
        /// <param name="message">The message to output in the log</param>
        void OutputToLog(string message)
        {
            outputLog.Text += DateTime.Now.ToString("T") + ":> " + message + "\n";
            outputLog.ScrollToEnd();
        }

        /// <summary>
        /// Enables / Disables the serial related buttons
        /// </summary>
        void ToggleCommands()
        {
            menuItemCloseSerial.IsEnabled = !menuItemCloseSerial.IsEnabled;
            menuItemOpenSerial.IsEnabled = !menuItemOpenSerial.IsEnabled;
            buttonOpenSerial.IsEnabled = !buttonOpenSerial.IsEnabled;
            buttonCloseSerial.IsEnabled = !buttonCloseSerial.IsEnabled;

            buttonAbort.IsEnabled = !buttonAbort.IsEnabled;
            buttonLaunch.IsEnabled = !buttonLaunch.IsEnabled;
            buttonAux1.IsEnabled = !buttonAux1.IsEnabled;
            buttonAux2.IsEnabled = !buttonAux2.IsEnabled;
            buttonAux3.IsEnabled = !buttonAux3.IsEnabled;
            buttonDeplyParachute.IsEnabled = !buttonDeplyParachute.IsEnabled;
        }

        /// <summary>
        /// Opens the SerialPortConfigWindow window and gets the results
        /// </summary>
        void ConfigSerialPort()
        {
            SerialPortConfig configWindow = new SerialPortConfig();

            OutputToLog("Serial Configuration Window Opened.");
            if (configWindow.ShowDialog() == true)
            {
                if(configWindow.PortName != null)
                {
                    _serialPort.PortName = configWindow.PortName;
                    OutputToLog("Serial Line updated to: " + _serialPort.PortName);
                }
                else
                {
                    OutputToLog("No Serial Port selected. Current serial port: " + _serialPort.PortName);
                }

                if(configWindow.baudRate != 0)
                {
                    _serialPort.BaudRate = configWindow.baudRate;
                    OutputToLog("Serial Baud Rate has been updated to: " + _serialPort.BaudRate);
                }
                else
                {
                    OutputToLog("No Baud Rate entered.  Current Baud Rate:" + _serialPort.BaudRate);
                }
            }
            else
            {
                OutputToLog("Serial Configuration Window Closed.");
            }
        }
        #endregion
    }
}