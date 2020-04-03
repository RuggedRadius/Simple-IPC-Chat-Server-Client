using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AT2_4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PipeClient pipeClient;
        private bool connected;
        private bool loggedIn;

        private int serverReconnectTimeout = 3;

        public MainWindow()
        {
            // Initialise the window
            InitializeComponent();
            log("Client initialised");
            InitialiseClient();
            
            Task.Run(() =>
            {
                string serverPath;
                while (true)
                {
                    if (!connected)
                    {
                        log("Attempting to re-connect in");
                        for (int i = serverReconnectTimeout; i > 0; i--)
                        {
                            log(i + "...");
                            Thread.Sleep(1000);
                        }

                        Dispatcher.Invoke(() => {
                            // Connect to server
                            serverPath = getServerPathByName(txtClientServerName.Text);
                            if (pipeClient.Connect(serverPath))
                            {
                                //btnClientConnect.IsEnabled = false;
                                log("Client connected");
                                lblConnectionStatus.Content = "Connected";
                                lblConnectionStatus.Foreground = new SolidColorBrush(Colors.Green);
                                connected = true;
                            }
                            else
                            {
                                log("Client failed to connect");
                                lblConnectionStatus.Content = "Disconnected";
                                lblConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);
                                connected = false;
                            }
                        });
                    }
                }
            });
        }

        private void CreatePipeClient()
        {
            // Create the pipe Client
            if (pipeClient != null)
            {
                pipeClient.MessageReceived -= pipeClient_MessageReceived;
                pipeClient.ServerDisconnected -= pipeClient_ServerDisconnected;
            }

            pipeClient = new PipeClient();
            pipeClient.MessageReceived += pipeClient_MessageReceived;
            pipeClient.ServerDisconnected += pipeClient_ServerDisconnected;
        }

        public void InitialiseClient()
        {
            CreatePipeClient();



            // Modify window for client display
            Dispatcher.Invoke(() => {
                grpClientInformation.Visibility = Visibility.Visible;
                grpMessages.Visibility = Visibility.Hidden;

                // Connect to server
                string serverPath = getServerPathByName(txtClientServerName.Text);
                if (pipeClient.Connect(serverPath))
                {
                    //btnClientConnect.IsEnabled = false;
                    log("Client connected");
                    lblConnectionStatus.Content = "Connected";
                    lblConnectionStatus.Foreground = new SolidColorBrush(Colors.Green);
                    connected = true;
                }
                else
                {
                    log("Client failed to connect");
                    lblConnectionStatus.Content = "Disconnected";
                    lblConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);
                    connected = false;
                }
            });
        }



        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (txtSend.Text != string.Empty)
            {
                // Encode string to byte[] and send to server
                ASCIIEncoding encoder = new ASCIIEncoding();
                pipeClient.SendMessage(encoder.GetBytes(txtSend.Text));

                // Add to log
                //log("Message sent");
            }

            txtSend.Text = string.Empty;
        }
        private void addToMessages(string message)
        {
            txtMessages.Text += String.Format("{0}", message);
            txtMessages.ScrollToEnd();
        }

        public void log(string log)
        {
            Dispatcher.Invoke(() => {
                // Get current time
                string currentDateTime = DateTime.Now.TimeOfDay.ToString();

                // Truncate time
                string time = currentDateTime.Substring(0, currentDateTime.IndexOf('.'));
                
                // Add to log
                txtLog.Text += string.Format("[{0}] {1}\r\n", time, log);

                // Scroll log to most recent entry
                txtLog.ScrollToEnd();
            });
        }


        private void pipeClient_ServerDisconnected()
        {
            log("Disconnected from server");

            Dispatcher.Invoke(new PipeClient.ServerDisconnectedHandler(EnableStartButton));

            Dispatcher.Invoke(() => {
                lblConnectionStatus.Content = "Disconnected";
                lblConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);
                grpMessages.Visibility = Visibility.Hidden;
                lblAccountStatus.Content = "Not Logged In";
                lblAccountStatus.Foreground = new SolidColorBrush(Colors.Red);
            });


            connected = false;
            loggedIn = false;
        }

        private void EnableStartButton()
        {
            btnClientConnect.IsEnabled = true;
        }

        private void pipeClient_MessageReceived(byte[] message)
        {
            // Convert message to string
            string msg = System.Text.Encoding.UTF8.GetString(message, 0, message.Length);

            // Filter login attempt
            if (msg.StartsWith("-login "))
            {
                string[] responses = msg.Split(' ');

                string usernameCurVal = "";
                Dispatcher.Invoke(() => {
                    usernameCurVal = txtUsername.Text;
                });
  
                if (responses[2].Equals(usernameCurVal))
                {
                    // Response intended for this user
                    if (responses[1].Equals("success"))
                    {
                        // Login success
                        log("Login successful");
                        Dispatcher.Invoke(() =>
                        {
                            grpMessages.Visibility = Visibility.Visible;
                            txtMessages.Text = string.Empty;
                            lblAccountStatus.Content = "Logged In";
                            lblAccountStatus.Foreground = new SolidColorBrush(Colors.Green);
                            loggedIn = true;
                        });
                    }
                    else
                    {
                        // Login failed
                        log("Login failed");
                        Dispatcher.Invoke(() =>
                        {
                            grpMessages.Visibility = Visibility.Hidden;
                            lblAccountStatus.Content = "Not Logged In";
                            lblAccountStatus.Foreground = new SolidColorBrush(Colors.Red);
                            loggedIn = false;
                        });
                    }
                }
                return;
            }

            if (loggedIn)
            {
                log("Message received");
                Dispatcher.Invoke(new PipeClient.MessageReceivedHandler(DisplayReceivedMessage), new object[] { message });
            }
        }

        private void DisplayReceivedMessage(byte[] message)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string str = encoder.GetString(message, 0, message.Length);

            if (str == "close")
            {
                pipeClient.Disconnect();

                InitialiseClient();
                string serverPath = getServerPathByName(txtClientServerName.Text);
                pipeClient.Connect(serverPath);
            }

            addToMessages(str);
        }

        private void btnClientConnect_Click(object sender, RoutedEventArgs e)
        {
            if (connected)
            {
                Login(txtUsername.Text, txtPassword.Password);
            }
            else
            {
                MessageBox.Show("Check connection to server.", "No server connection", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClientDisconnect_Click(object sender, RoutedEventArgs e)
        {
            pipeClient.Disconnect();
            EnableStartButton();
            lblConnectionStatus.Content = "Disconnected";
            lblConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);
            connected = false;

            Dispatcher.Invoke(() => {
                grpMessages.Visibility = Visibility.Hidden;
                lblAccountStatus.Content = "Not Logged In";
                lblAccountStatus.Foreground = new SolidColorBrush(Colors.Red);
                loggedIn = false;
            });

            //pipeClient = null;
            
            
        }

        private void txtClientServerName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Login(string username, string password)
        {
            // Do login stuff..
            ASCIIEncoding encoder = new ASCIIEncoding();
            string loginString = String.Format("-login {0} {1}", username, password);
            pipeClient.SendMessage(encoder.GetBytes(loginString));
        }

        private void txtSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSend_Click(sender, e);
            }
        }

        private string getServerPathByName(string name)
        {
            return string.Format(@"\\.\pipe\{0}", name);
        }
    }
}
