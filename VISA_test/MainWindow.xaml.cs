using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;

using System.IO.Ports;

namespace VISA_test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int defaultRM = -1;
        private int vi = -1;
        private Boolean start = false;
        private ViStatus viStatus;
        private Thread thread;
        private SerialPort serialport;

        public MainWindow()
        {
            InitializeComponent();
            readUSB();
        }

        private void readUSB()
        {
            this.viStatus = VISA.viOpenDefaultRM(ref this.defaultRM);

            if (this.viStatus < ViStatus.VI_SUCCESS)
            {
                MessageBox.Show("Failed to open Default RM", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            String usb_ID = "USB0::0x0AAD::0x0178::100958::INSTR";
            this.viStatus = VISA.viOpen(this.defaultRM, usb_ID, VISA.VI_NULL, VISA.VI_NULL, ref this.vi);

            if (this.viStatus < ViStatus.VI_SUCCESS)
            {
                MessageBox.Show("Failed to Connect", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            msgbox.AppendText("Connection Success!\n");

            // Set time out values.
            this.viStatus = VISA.viSetAttribute(this.vi, ViAttr.VI_ATTR_TMO_VALUE, 5000);
        }

        private void start_btn_Click(object sender, RoutedEventArgs e)
        {
            start = true;
            connectSerial();
            thread = new Thread(readData);
            thread.Start();
        }

        private void stop_btn_Click(object sender, RoutedEventArgs e)
        {
            start = false;
            thread.Abort();
            thread.Join();
            serialport.Close();
            msgbox.AppendText("*****     Stop Data Read     *****\n");
        }

        // Thread function that continuously reads in data values until stopped by the stop button.
        private void readData()
        {
            msgbox.AppendText("*****     Start Data Read     *****\n");

            while (start)
            {
                int writeCount = 0;
                String cmd = "FETCh?";
                this.viStatus = VISA.viWrite(this.vi, cmd, cmd.Length, ref writeCount);

                if (this.viStatus < ViStatus.VI_SUCCESS)
                {
                    MessageBox.Show(
                        "Error writing to the device!",
                        "R&S Korea Antenna Measure", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                int retCount = 0;
                StringBuilder retMsg = new StringBuilder(500);
                this.viStatus = VISA.viRead(this.vi, retMsg, 500, ref retCount);

                if (this.viStatus < ViStatus.VI_SUCCESS)
                {
                    MessageBox.Show(
                        "Error reading a response from the device!",
                        "R&S Korea Antenna Measure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    msgbox.AppendText(retMsg.ToString(0, 13));
                    serialport.Write(retMsg.ToString(0, 13) + "   ");
                }

                Thread.Sleep(1000);
            }
        }

        private void connectSerial()
        {
            try
            {
                serialport = new SerialPort("COM35", 9600);
                serialport.Open();
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to conenct to serial port", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
