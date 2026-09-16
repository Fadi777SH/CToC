using CToC.Server;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;


namespace CToC
{

    
    public partial class MainWindow : Window
    {
        TcpServer Server ;
        public static event Action? StopClient;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            this._IPAddress = GetIPAddress().ToString();
            TcpServer.PC2DisConnect += PC2Disconnect;

        }
        public string _IPAddress { get; set; }

        private void PC2Disconnect()
        {
            this.Close();
        }

        private IPAddress GetIPAddress()
        {
            var dns = Dns.GetHostEntry(Dns.GetHostName());
            foreach( var address in dns.AddressList) 
            {
                if(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address;
                }
            }
            return IPAddress.Any;
        }

        private void changingpassword_PasswordOfSender(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.PasswordOfSender.Text.Length > 15)
                PasswordOfSender.Text = PasswordOfSender.Text.Substring(0,4);
        }

        private void changingpassword_PasswordOfController(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.PasswordOfController.Text.Length > 15)
              PasswordOfController.Text = PasswordOfController.Text.Substring(0, 4);
        }

        private void PC2ToPC1Checked(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            Server = new();

            IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
            IPAddress ipofpc2 = IPAddress.Parse(this.PasswordOfController.Text);
            if (btn != null && btn.IsChecked == true)
            {
                Server.Reciever(ipofpc1, ipofpc2);
            }
        }

        private void PC1ToPC2Checked(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            Server = new();

            IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
            IPAddress ipofpc2 = IPAddress.Parse(this.PasswordOfController.Text);
            if (btn != null&& btn.IsChecked == true )
            {
                Server.Sender(ipofpc1, ipofpc2);
            }
           
        }
    }
}
