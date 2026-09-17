using CToC.Keyboard;
using CToC.Mouse;
using CToC.Server;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WindowsInput;


namespace CToC
{

    
    public partial class MainWindow : Window
    {
        TcpServer Server =new();
        private static IntPtr _hookID = IntPtr.Zero;
        public delegate void MouseChangeHandler(System.Windows.Point point);

        public static event MouseChangeHandler? MouseChange;

        public delegate void KeyPressHandler(System.Windows.Input.Key key);
        public static event KeyPressHandler? KeyPressEvent;
        public delegate void MousePressHandler(System.Windows.Input.MouseButton mouseButton,MouseButtonState state);
        public static event MousePressHandler? MousePressEvent;
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

            IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
            IPAddress ipofpc2 = IPAddress.Parse(this.PasswordOfController.Text);
            if (btn != null && btn.IsChecked == true)
            {
                Server.Reciever(ipofpc1, ipofpc2);
            }
        }

        private void PC1ToPC2Checked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(sender.GetType());
            var btn = sender as ToggleButton;
            Server = new();

            IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
            IPAddress ipofpc2 = IPAddress.Parse(this.PasswordOfController.Text);
            if (btn != null&& btn.IsChecked == true )
            {
                Server.Sender(ipofpc1, ipofpc2);
            }
           
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            KeyPressEvent?.Invoke(e.Key);
        }

        private void Window_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //inconsistant with the PC corrodinates
            System.Windows.Point MousePoint = GetMousePos();
            
            MouseChange?.Invoke(MousePoint);

        
            
        }


        private  System.Windows.Point GetMousePos()
        {
 
            var MousePoint = MousePosition.GetCursorPosition();

            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix transform = source.CompositionTarget.TransformFromDevice;
            var wpfPoint = transform.Transform(MousePoint);
            return MousePoint;
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MousePressEvent?.Invoke(System.Windows.Input.MouseButton.Left,e.LeftButton);
        }

        private void Window_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            MousePressEvent?.Invoke(System.Windows.Input.MouseButton.Right, e.RightButton);
        }
    }
}
