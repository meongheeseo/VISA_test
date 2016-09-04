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
        private SerialPort adam4024;

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
            stopAll();
        }

        private void stopAll()
        {
            start = false;
            thread.Abort();
            thread.Join();

            adam4024.Write("#00C0-05.000\r\n");

            serialport.Close();
            adam4024.Close();
            msgbox.AppendText("*****     Stop Data Read     *****\n");
        }

        // Thread function that continuously reads in data values until stopped by the stop button.
        private void readData()
        {
            msgbox.AppendText("*****     Start Data Read     *****\n");

            int step = 100;
            int i = 0;
            int max = -1;

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
                    double visa_data = Convert.ToDouble(retMsg.ToString(0, 13));
                    double data = i * ((max + 5) / step) - 5;
                    String msg = "#00C0" + formatNum(data) + "\r\n";

                    adam4024.Write(msg);
                    //serialport.Write(retMsg.ToString(0, 13) + "   ");
                }

                Thread.Sleep(1000);
                i++;

                if (step == i)
                {
                    stopAll();
                }
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

            String adamSerialPort = "";
            // ADAM 4024 Connection
            try
            {
                adam4024 = new SerialPort(adamSerialPort, 9600);
                adam4024.Open();
                adam4024.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                adam4024.Write("%0000000600\r\n");
                msgbox.AppendText("%0000000600\r\n");
                wait(0.5);
                
                adam4024.Write("$007C0R32\r\n");
                msgbox.AppendText("$007C0R32\r\n");
                wait(0.5);
                
                adam4024.Write("#00SC0-05.000\r\n");
                msgbox.AppendText("#00SC0-05.000\r\n");
                wait(0.5);
                
                adam4024.Write("$004\r\n");
                msgbox.AppendText("$004\r\n");
                wait(0.5);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to connect to ADAM", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
         
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            msgbox.AppendText(sp.ReadExisting() + "\r\n");
        }

        public static void wait(double seconds)
        {
            var frame = new DispatcherFrame();

            new Thread((ThreadStart)(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(seconds));
                    frame.Continue = false;
                })).Start();

            Dispatcher.PushFrame(frame);
        }

        private static String formatNum(double a)
        {
            if (a >= 0)
            {
                return (String.Format("{0, 0:+00.000}\r\n", a));
            }
            else
            {
                return (String.Format("{0, 0:00.000}\r\n", a));
            }
        }
    }
}
