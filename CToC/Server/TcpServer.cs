using Microsoft.VisualBasic.Devices;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using WindowsInput;

namespace CToC.Server
{
    public  class TcpServer
    {
        Socket? Accept;
        InputSimulator inputsime;

        public static event Action? PC2DisConnect;

        public async Task Sender(IPAddress IPAddressOfPC1 , IPAddress IPAddressOfPC2)
        {
            
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddressOfPC1 ,22));

            socket.Listen(10);

            var Endpoint = new IPEndPoint(IPAddressOfPC2, 22);
            new Thread(async delegate ()
            {

                Accept = await socket.AcceptAsync();

                socket.Close();
                while (true)
                {
                    try
                    {
                       
                        byte[] buffer = new byte[255];
                        var rec = await Accept.ReceiveAsync(buffer, 0);
                        if (rec <= 0)
                        {
                            throw new SocketException();
                        }
                        Array.Resize(ref buffer, rec);
                        Debug.WriteLine(Encoding.Default.GetString(buffer));
                        MainWindow.KeyPressEvent += PressThisKey;
                        MainWindow.MouseChange += MouseChangepos;
                        MainWindow.MousePressEvent += MousePressedDown;

                    }
                    catch
                    {

                        System.Windows.MessageBox.Show("Error");
                        PC2DisConnect?.Invoke();
                        break;


                    }
                }
            })
            {
                
            }.Start();
            
            
        }
        public async Task Reciever(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            inputsime = new();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpint = new IPEndPoint(IPAddressOfPC1,22);
            socket.Connect(endpint);
            byte[] RecievedByte = new byte[255];

            int size =await socket.ReceiveAsync(RecievedByte);
            Array.Resize(ref RecievedByte, size);
            TCPMessage MSG = fromBytes(RecievedByte);
            if(MSG.type == typeof(Key))
            {
                inputsime.Keyboard.KeyPress((WindowsInput.Native.VirtualKeyCode)MSG.key);
            }
            else if (MSG.type == typeof(System.Windows.Point))
            {
                inputsime.Mouse.MoveMouseToPositionOnVirtualDesktop(MSG.point.X,MSG.point.Y);
            }
            else if(MSG.type == typeof(System.Windows.Input.MouseButton))
            {
                if(MSG.mousestate == MouseButtonState.Pressed)
                {
                    if(MSG.MouseSide == System.Windows.Input.MouseButton.Left)
                    {
                        inputsime.Mouse.LeftButtonClick();
                    }
                    if(MSG.MouseSide == System.Windows.Input.MouseButton.Right)
                    {
                        inputsime.Mouse.RightButtonClick();
                    }
                    if(MSG.MouseSide == System.Windows.Input.MouseButton.Middle)
                    {
                        //
                    }
                }
            }
        }

        struct TCPMessage
        {
            public Type type;
            public Key key;
            public System.Windows.Input.MouseButton MouseSide;
            public System.Windows.Point point;

            public MouseButtonState mousestate;

        };
        byte[] getBytes(TCPMessage str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, true);
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

            
            MSG.type = typeof(Key);
            MSG.key = key;
            MSG.point = new System.Windows.Point(0,0);
            MSG.mousestate = MouseButtonState.Pressed;
            MSG.MouseSide = System.Windows.Input.MouseButton.Left;
            byte[] bytes = getBytes(MSG);
            if (Accept !=null)
            await Accept.SendAsync(bytes);
        }
        public async void MouseChangepos(System.Windows.Point point)
        {

            TCPMessage MSG = new();
      

            MSG.type = typeof(System.Windows.Point);
            MSG.key = Key.LWin;
            MSG.point = point;
            MSG.MouseSide = System.Windows.Input.MouseButton.Left;
            MSG.mousestate = MouseButtonState.Pressed;

            byte[] bytes = getBytes(MSG);
            if (Accept != null)
                await Accept.SendAsync(bytes);
        }
        public async void MousePressedDown(System.Windows.Input.MouseButton mouseside, MouseButtonState state)
        {

            TCPMessage MSG = new();


            MSG.type = typeof(System.Windows.Input.MouseButton);
            MSG.key = Key.LWin;
            MSG.point = new(0,0);
            MSG.MouseSide = mouseside;
            MSG.mousestate = state;
            byte[] bytes = getBytes(MSG);
            if (Accept != null)
                await Accept.SendAsync(bytes);
        }
    }
}
