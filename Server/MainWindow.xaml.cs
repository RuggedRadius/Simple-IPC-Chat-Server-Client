using AT2_4.Login;
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
        private PipeServer pipeServer;        
        static UserRepository userRepo = new UserRepository();
        static PasswordManager pwdManager = new PasswordManager();
        public string currentServerName;

        private List<User> onlineUsers;

        public MainWindow()
        {
            // Initialise the window
            InitializeComponent();

            // Initialise the server
            InitialiseServer();
        }


        void InitialiseServer()
        {
            // Create admin accounts
            CreateAdminAccount();

            // Get server name from field
            currentServerName = txtServerName.Text;

            // Create list of online users
            onlineUsers = new List<User>();

            // Create object of server
            pipeServer = new PipeServer();

            // Modify window for server display
            grpServerInformation.Visibility = Visibility.Visible;

            // Add event handlers
            pipeServer.MessageReceived += pipeServer_MessageReceived;
            pipeServer.ClientDisconnected += pipeServer_ClientDisconnected;

            log("Server initialised");
        }

        public void CreateAdminAccount()
        {
            log("Creating accounts...");
            string[] usernames = { "admin1", "admin2", "admin3" };
            string password = "password1";

            foreach (string username in usernames)
            {
                string passwordHash = pwdManager.GeneratePasswordHash(password, out string salt);

                User adminUser = new User
                {
                    UserId = username,
                    PasswordHash = passwordHash,
                    Salt = salt
                };

                // Add account to user repository
                userRepo.AddUser(adminUser);

                // Log to user
                log(string.Format("{0} created", username));
            }
        }

        public static string SimulateUserCreation()
        {
            Console.WriteLine("Let us first test the password hash creation i.e. User creation");

            Console.WriteLine("Please enter user id");
            string userid = Console.ReadLine();

            Console.WriteLine("Please enter password");
            string password = Console.ReadLine();
            string salt = null;
            string passwordHash = pwdManager.GeneratePasswordHash(password, out salt);

            // Let us save the values in the database
            User user = new User
            {
                UserId = userid,
                PasswordHash = passwordHash,
                Salt = salt
            };
            // Lets Add the User to the database
            userRepo.AddUser(user);
            return salt;
        }

        public void Login(string username, string password)
        {
            log("User logging in...");

            User newUser = userRepo.GetUser(username);

            bool result;
            if (newUser != null)
            {
                // User exists, checking password
                result = pwdManager.IsPasswordMatch(password, newUser.Salt, newUser.PasswordHash);
            }
            else
            {
                // User not found
                result = false;
            }

            if (result)
            {
                // Password matched
                log("Password matched");

                // Send login success message to client                
                ASCIIEncoding encoder = new ASCIIEncoding();
                string successString = string.Format("-login success {0}", username);
                byte[] messageBuffer = encoder.GetBytes(successString);                                
                // Send message
                pipeServer.SendMessage(messageBuffer);

                // Add user to online users
                onlineUsers.Add(newUser);

                // Announce connection to messages
                Dispatcher.Invoke(() => {
                    txtMessages.Text += String.Format("*Client [{0}] has connected.*\r\n", username);
                });
            }
            else
            {
                // Password did not match
                log("Incorrect Username/Password");
            }
        }

        public static void SimulateLogin(string salt)
        {
            Console.WriteLine("Now let is simulate the password comparison");

            Console.WriteLine("Please enter user id");
            string userid = Console.ReadLine();

            Console.WriteLine("Please enter password");
            string password = Console.ReadLine();

            // Let us retrieve the values from the database
            User user2 = userRepo.GetUser(userid);

            bool result = pwdManager.IsPasswordMatch(password, user2.Salt, user2.PasswordHash);

            if (result == true)
            {
                Console.WriteLine("Password Matched");
            }
            else
            {
                Console.WriteLine("Password not Matched");
            }
        }


        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (txtSend.Text != string.Empty)
            {
                // Convert string to byte[]
                ASCIIEncoding encoder = new ASCIIEncoding();
                string msg = string.Empty;
                Dispatcher.Invoke(() => {
                    msg = String.Format("Server:\t{0}\r\n", txtSend.Text);
                });

                byte[] messageBuffer = encoder.GetBytes(msg);

                // Send message
                pipeServer.SendMessage(messageBuffer);

                // Add to messages
                Dispatcher.Invoke(() => {
                    txtMessages.Text += msg;
                    txtMessages.ScrollToEnd();
                });

                // Add to log
                log("Message sent");
            }

            // Clear send text box
            txtSend.Text = string.Empty;
        }
        private void addToMessages(string message)
        {
            Dispatcher.Invoke(() => {
                txtMessages.Text += String.Format("{0}\r\n", message);
                txtMessages.ScrollToEnd();
            });
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
        
        private void pipeServer_ClientDisconnected()
        {
            Dispatcher.Invoke(new PipeServer.ClientDisconnectedHandler(ClientDisconnected));
            Dispatcher.Invoke(() => {
                txtMessages.Text += "*Client has disconnected.*\r\n";
                txtMessages.ScrollToEnd();

            });
        }

        private void ClientDisconnected()
        {
            
            log("Client disconnected");            
        }

        private void pipeServer_MessageReceived(byte[] message)
        {
            log("Message received");

            // Convert message to string
            string msg = System.Text.Encoding.UTF8.GetString(message, 0, message.Length);

            // Filter login attempt
            if (msg.StartsWith("-login "))
            {
                int whiteSpaceIndex = msg.LastIndexOf(' ');
                string username = msg.Substring(7, whiteSpaceIndex - 7);
                string passsword = msg.Substring(whiteSpaceIndex, msg.Length - whiteSpaceIndex);
                passsword = passsword.Substring(1, passsword.Length - 1);
                Login(username, passsword);
                return;
            }

            // Display message
            //Dispatcher.Invoke(new PipeServer.MessageReceivedHandler(DisplayMessageReceived), new object[] { message });

            // Convert string to byte[]
            msg = String.Format("Client:\t{0}\r\n", msg);
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] messageBuffer = encoder.GetBytes(msg);

            // Send message to all
            pipeServer.SendMessage(messageBuffer);

            // Add to messages
            Dispatcher.Invoke(() => {
                txtMessages.Text += msg;
                txtMessages.ScrollToEnd();
            });


        }

        private void DisplayMessageReceived(byte[] message)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string str = encoder.GetString(message, 0, message.Length);

            addToMessages(str);
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            //start the pipe server if it's not already running
            if (!pipeServer.Running)
            {
                string serverPath = getServerPathByName(txtServerName.Text);
                pipeServer.Start(serverPath);
                btnStartServer.IsEnabled = false;
                log("Server started");
                lblStatus.Content = "Running";
                lblStatus.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                log("Server failed to start");
                MessageBox.Show("Server already running.");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // DEBUG ONLY
            btnStartServer_Click(sender, e);
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
