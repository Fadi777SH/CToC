using CToC.Keyboard;
using CToC.Mouse;
using CToC.Server;
using Microsoft.VisualBasic.Devices;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WindowsInput;
using Point = System.Windows.Point;

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
        public static event Action? EndClientConnection;

        public static event Action? EndServerConnection;
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
                Server?.Reciever(ipofpc1, ipofpc2);
            }
            if (btn != null && btn.IsChecked == false)
            {
                EndClientConnection?.Invoke();
                System.Windows.MessageBox.Show("you disconnect as Client .");
            }
        }

        private void PC1ToPC2Checked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(sender.GetType());
            var btn = sender as ToggleButton;
            Server = new();

            IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
            IPAddress ipofpc2 = IPAddress.Parse(this.PasswordOfSender.Text);

            if (btn != null&& btn.IsChecked == true )
            {
                Server?.Sender(ipofpc1, ipofpc2);
            }
            if (btn != null && btn.IsChecked == false)
            {
                EndServerConnection?.Invoke();
                System.Windows.MessageBox.Show("you disconnect your Server .");
            }

        }



        private  Point GetMousePosInDpi(Point MousePoint)
        {
 
         
            //mousepos in DPI
            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix transform = source.CompositionTarget.TransformFromDevice;
            var wpfPoint = transform.Transform(MousePoint);
            //mousepos in DPI

            return wpfPoint;
        }



        private void PC2Fram_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

            Point portionInFram = PortionOfCursorInPC2Fram();
            
            MouseChange?.Invoke(portionInFram);

        }

        private Point PortionOfCursorInPC2Fram()
        {
            var Mousepos = Mouse.MousePosition.GetCursorPosition();
            
            //the point in the fram 
            var point = this.PC2Fram.PointFromScreen(Mousepos);
            
            Point ABSpointofthefram = new(0, 0);

            var Width = this.PC2Fram.ActualWidth;
            var height = this.PC2Fram.ActualHeight;
            
            var PercentofXfarFromTheABS = ((point.X - ABSpointofthefram.X) / Width) * 100;
            var PercentofYfarFromTheABS = ((point.Y - ABSpointofthefram.Y) / height) * 100;
            
            return new(PercentofXfarFromTheABS, PercentofYfarFromTheABS);
        }

        private void PC2Fram_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void PC2Fram_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MousePressEvent?.Invoke(System.Windows.Input.MouseButton.Left, e.LeftButton);
        }

        private void PC2Fram_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            MousePressEvent?.Invoke(System.Windows.Input.MouseButton.Right, e.RightButton);
        }

        private void PC2Fram_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
              if(this.PC2Fram.IsMouseOver==true)
              KeyPressEvent?.Invoke(e.Key);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //EndClientConnection?.Invoke();
            //EndServerConnection?.Invoke();
        }
    }
}
