using CToC.Keyboard;
using CToC.Mouse;
using CToC.Screen;
using CToC.Server;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Graphics.Capture;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using BitmapEncoder = Windows.Graphics.Imaging.BitmapEncoder;
using Point = System.Windows.Point;

namespace CToC
{


    public  partial class MainWindow :System.Windows.Window
    {
        UdpServer Server = new();
        private static IntPtr _hookID = IntPtr.Zero;
        public delegate void MouseChangeHandler(System.Windows.Point point);

        public static event MouseChangeHandler? MouseChange;

        public  DispatcherQueue _dispatcherQueue;

        public delegate void KeyPressHandler(System.Windows.Input.Key key);
        public static event KeyPressHandler? KeyPressEvent;
        public delegate void MousePressHandler(System.Windows.Input.MouseButton mouseButton, MouseButtonState state);
        public static event MousePressHandler? MousePressEvent;
        public delegate void MouseWheelHandler(int delta);
        public static event MouseWheelHandler? MouseWheelevent;
        public static event Action? EndClientConnection;
        private int ScreenWidth = SystemInformation.VirtualScreen.Width;
        private int ScreenHeight = SystemInformation.VirtualScreen.Height;
        private int ScreenX = SystemInformation.VirtualScreen.X;
        private int ScreenY = SystemInformation.VirtualScreen.Y;
        TakeScreenSnippit screenDisplay;
        private IntPtr Handle;

   
        public static event Action? EndServerConnection;
        public MainWindow()
        {

            InitializeComponent();
            DataContext = this;
            

            this.ThisPCIPAddress = GetIPAddress().ToString();


            Handle = new WindowInteropHelper(this).Handle;


            UdpServer.FrameArrived += TcpServer_DisplayFrame;
            
        }

        private async void TcpServer_DisplayFrame(byte[] bytes)
        {
            try
            {
                var decompressbyte = ConversionClass.Decompress(bytes);
                var ImageSource = ConvertToImageSource(decompressbyte);
                this.Dispatcher.Invoke(() => this.Frames.Source = ImageSource);
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }


        }

        public static BitmapImage ConvertToImageSource(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {

                ms.Position = 0;
               
                var bitmapImage = new BitmapImage();
                
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); 

                return bitmapImage;
            }
        }

        public string _IPAddress
        {
            get
            {
                return ThisPCIPAddress;
            }
        } 


        private string ThisPCIPAddress { get; set; }

        private IPAddress GetIPAddress()
        {
            var dns = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var address in dns.AddressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address;
                }
            }
            return IPAddress.Any;
        }

        private void changingpassword_PasswordOfSender(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.PasswordOfSender.Text.Length > 15)
                PasswordOfSender.Text = PasswordOfSender.Text.Substring(0, 4);

        }

        private void changingpassword_PasswordOfController(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.PasswordOfController.Text.Length > 15)
                PasswordOfController.Text = PasswordOfController.Text.Substring(0, 4);
        }

        private void PC2ToPC1Checked(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;

            try
            {
                IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
                IPAddress ipofpc2 = IPAddress.Parse(this.PasswordOfController.Text);

     

                if (btn != null && btn.IsChecked == true)
                {
                    if (PC1ToPC2.IsChecked == false && (ipofpc1.ToString() != ipofpc2.ToString()))
                    {
                       Server?.Reciever(ipofpc1, ipofpc2);
                      //uDPServerAsync?.StartRemoteServer();
                      
                    }
                    else
                    {
                        if (ipofpc1.ToString() == ipofpc2.ToString())
                        {
                            System.Windows.MessageBox.Show("you can't connect to the same IP of your computer");
                        }
                        if (PC1ToPC2.IsChecked == true)
                        {
                            System.Windows.MessageBox.Show("you can't be a reciver and a remote at the same time");
                        }


                        
                        btn.IsChecked = false;
                    }
                }
                else if(btn != null && btn.IsChecked == false)
                {
                    Server.disconnectClient();
                    System.Windows.MessageBox.Show("you disconnect as a remote");
                }
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid IP type . \n {ex.Message}");
                btn?.IsChecked = false;
            }
        }

        private void PC1ToPC2Checked(object sender, RoutedEventArgs e)
        {
            
            var btn = sender as ToggleButton;
            try
            {
                IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
                IPAddress ipofpc2 = IPAddress.Parse(this.PasswordOfSender.Text);
                
                if (btn?.IsChecked == true)
                {
                    if (PC2ToPC1?.IsChecked == false&&(ipofpc1.ToString() != ipofpc2.ToString()))
                    {
                        Server?.Sender(ipofpc1, ipofpc2);

                    }
                    else
                    {
                        if(ipofpc1.ToString() == ipofpc2.ToString())
                        {
                            System.Windows.MessageBox.Show("you can't connect to the same IP of your computer");
                        }
                        if (PC2ToPC1?.IsChecked == true)
                        {
                            System.Windows.MessageBox.Show("you can't be a reciver and a remote at the same time");
                        }
                        
                        btn.IsChecked = false;
                    }
  
                }
                else if (btn != null && btn.IsChecked == false)
                {
                    Server.disconnectAccepter();
                    System.Windows.MessageBox.Show("you disconnect as a remote");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid IP type .\n {ex.Message}");
                btn?.IsChecked = false;
            }
        }



        private Point GetMousePosInDpi(Point MousePoint)
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
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                MousePressEvent?.Invoke(MouseButton.Left, MouseButtonState.Pressed);
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                MousePressEvent?.Invoke(MouseButton.Right, MouseButtonState.Pressed);
            }
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

        private void PC2Fram_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (this.PC2Fram.IsMouseOver == true)
                KeyPressEvent?.Invoke(e.Key);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Server.Appclosed();

        }

        private void PC2Fram_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MouseWheelevent?.Invoke(e.Delta);
        }

        private void PC2Fram_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MousePressEvent?.Invoke(e.ChangedButton, e.ButtonState);
        }

        private void PC2Fram_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MousePressEvent?.Invoke(e.ChangedButton, e.ButtonState);
        }
    }
}