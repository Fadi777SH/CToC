using CToC.Mouse;
using CToC.Screen;
using Microsoft.VisualBasic.Devices;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using WindowsInput;
using Point = System.Windows.Point;
namespace CToC.Server
{
    public  class TcpServer
    {
        Socket? Accept ;
        InputSimulator? inputsime;
        Socket? Client;
        int PORT = 22;
        private int ScreenX = SystemInformation.VirtualScreen.X;
        private int ScreenY = SystemInformation.VirtualScreen.Y;

        public static event Action? PC2DisConnect;
        private bool ServerShotDown = false;
        private bool ClientShotDown = false;
        int ScreenWidth = SystemInformation.VirtualScreen.Width;
        int ScreenHeight = SystemInformation.VirtualScreen.Height;
        EndPoint endPoint;
        int ABSX = SystemInformation.VirtualScreen.X;
        int ABSY = SystemInformation.VirtualScreen.Y;

        public delegate void framCapture(byte[] bytes);
        public static event framCapture? SingleFram;
        public async Task Sender(IPAddress IPAddressOfPC1 , IPAddress IPAddressOfPC2)
        {
            if (Accept?.Connected == true) return;
            Accept = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Accept.Bind(new IPEndPoint(IPAddressOfPC1,PORT));
            endPoint = new IPEndPoint(IPAddressOfPC2, PORT);



            new Thread(async delegate ()
            {
                try
                {
                    MainWindow.EndServerConnection += MainWindow_EndServerConnection;
                    MainWindow.KeyPressEvent += PressThisKey;
                    MainWindow.MouseChange += MouseChangepos;
                    MainWindow.MousePressEvent += MousePressedDown;

                    CaptureScreenFromClient(endPoint);
                }
                catch
                {

                    System.Windows.MessageBox.Show("Error");
                    PC2DisConnect?.Invoke();

                }
                
            })
            {
                
            }.Start();
            MainWindow.KeyPressEvent -= PressThisKey;
            MainWindow.MouseChange -= MouseChangepos;
            MainWindow.MousePressEvent -= MousePressedDown;
            MainWindow.EndServerConnection -= MainWindow_EndServerConnection;
        }

        private void MainWindow_EndServerConnection()
        {
            if (Accept != null && ServerShotDown == false)
            {
                Accept.Close();
                ServerShotDown = true;
            }
        }

        public async Task Reciever(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            inputsime = new();
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            Client.Bind(new IPEndPoint(IPAddressOfPC1, PORT));

            var Serverendpint = new IPEndPoint(IPAddressOfPC2,PORT);



            SendScreenFromClient(Serverendpint);


            while (true)
            {

                byte[] RecievedByte = new byte[255];
                try
                {
                    MainWindow.EndClientConnection += disconnectClient;
                    await Client.ReceiveFromAsync(RecievedByte,Serverendpint);
                    
                    TCPMessage MSG = fromBytes(RecievedByte);
                    if (MSG.type == MessageType.Keyboard)
                    {
                        int vk = KeyInterop.VirtualKeyFromKey(MSG.key);
                        var key = (WindowsInput.Native.VirtualKeyCode)vk;
                        inputsime.Keyboard.KeyDown(key);
                        inputsime.Keyboard.KeyUp(key);
                    }
                    else if (MSG.type == MessageType.point)
                    {

                        int vWidth = SystemInformation.VirtualScreen.Width;
                        int vHeight = SystemInformation.VirtualScreen.Height;

                        double absX = (MSG.point.X) * (65535.0 / vWidth);
                        double absY = (MSG.point.Y) * (65535.0 / vHeight);
                        var P = GetPC2MousePos(MSG.point);
                        //inputsime.Mouse.MoveMouseTo(absX, absY);
                        MousePosition.SetCursorPos((int)P.X,(int)P.Y);
                    }
                    else if (MSG.type == MessageType.MouseChange)
                    {
                        if (MSG.mousestate == MouseButtonState.Pressed)
                        {
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Left)
                            {
                                inputsime.Mouse.LeftButtonClick();
                            }
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Right)
                            {
                                inputsime.Mouse.RightButtonClick();
                            }
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Middle)
                            {

                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
        }


        
        enum MessageType
        {
            Keyboard,
            MouseChange,
            point ,
            MouseWheel
        }
        struct TCPMessage
        {
            public MessageType type;
            public Key key;
            public System.Windows.Input.MouseButton MouseSide;
            public System.Windows.Point point;

            
            public MouseButtonState mousestate;

        };
        byte[] getBytesOfTCPMessage(TCPMessage str)
        {
            int size = Marshal.SizeOf(str);

            byte[] arr = new byte[255];
            Array.Resize(ref arr, size);

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, false);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }
        TCPMessage fromBytes(byte[] arr)
        {
            TCPMessage str = new TCPMessage();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);

                str = (TCPMessage)Marshal.PtrToStructure(ptr, str.GetType());
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return str;
        }
        public async void PressThisKey(Key key)
        {

            TCPMessage MSG = new();

            
            MSG.type = MessageType.Keyboard;
            MSG.key = key;
            MSG.point = new System.Windows.Point(0,0);
            MSG.mousestate = MouseButtonState.Pressed;
            MSG.MouseSide = System.Windows.Input.MouseButton.Left;
            byte[] bytes = getBytesOfTCPMessage(MSG);
            if (Accept !=null&& ServerShotDown==false)
            await Accept.SendToAsync(bytes,endPoint);
        }
        public async void MouseChangepos(System.Windows.Point portion)
        {

            TCPMessage MSG = new();
      

            MSG.type = MessageType.point;
            MSG.key = Key.LWin;
     
            MSG.point = portion;
       
            MSG.MouseSide = System.Windows.Input.MouseButton.Left;
            MSG.mousestate = MouseButtonState.Pressed;

            byte[] bytes = getBytesOfTCPMessage(MSG);
            if (Accept != null && ServerShotDown==false)
            {
                await Accept.SendToAsync(bytes, endPoint);
            }

        }
        public async void MousePressedDown(System.Windows.Input.MouseButton mouseside, MouseButtonState state)
        {

            TCPMessage MSG = new();


            MSG.type = MessageType.MouseChange;
            MSG.key = Key.LWin;
            MSG.point = new(0,0);
            MSG.MouseSide = mouseside;
            MSG.mousestate = state;
            byte[] bytes = getBytesOfTCPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
               await Accept.SendToAsync(bytes, endPoint);
               

            }
        }
        private Point GetPC2MousePos(Point portion)
        {
            var Xpoint = (portion.X / 100) * ScreenWidth + ABSX;
            var Ypoint = (portion.Y / 100) * ScreenHeight + ABSY;
            return new(Xpoint, Ypoint);
        }

        public void disconnectClient()
        {
           // if (Client != null)
               // Client.Close();
        }
        private void SendScreenFromClient(EndPoint Serverendpint)
        {
            Rectangle screenrectangle = new(ScreenX, ScreenY, ScreenWidth, ScreenHeight);
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var Bitmap = RecordScreen.Recordscreen(screenrectangle);
                        var bytes = RecordScreen.BitmapTobyteConverter(Bitmap); 
                        Client?.SendToAsync(bytes,Serverendpint);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex); // don't MessageBox inside a loop
                    }

                }
            });
        }
        private void CaptureScreenFromClient(EndPoint Serverendpint)
        {
            Rectangle screenrectangle = new(ScreenX, ScreenY, ScreenWidth, ScreenHeight);
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {

                        var bytes = new byte[255];
                        var Size = await Accept.ReceiveFromAsync(bytes, Serverendpint);
                        Array.Resize(ref bytes, Size.ReceivedBytes);
                        SingleFram?.Invoke(bytes);

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex); // don't MessageBox inside a loop
                    }

                }
            });
        }
    }
}
