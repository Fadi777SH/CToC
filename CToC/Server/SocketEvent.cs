using CToC.Screen;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Windows.Input;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CToC.Server
{
    public class UDPServerAsync
    {
        private Socket? UserSocket;
        private Socket? RemoteSocket;
        private EndPoint? UserEndPoint;
        private EndPoint? RemoreEndpoint;
        private SocketAsyncEventArgs _socketAsyncEventArgs;
        private SocketAsyncEventArgs UserSocketEventArg;
        public UDPServerAsync(EndPoint userendpoint, EndPoint remoteendpoint)
        {
            UserEndPoint = userendpoint;
            RemoreEndpoint = remoteendpoint;
            Initilize();

        }

        private void Initilize()
        {
            _socketAsyncEventArgs = new();
            _socketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_compelete);
        }
        private void IO_compelete(object? sender, SocketAsyncEventArgs e)
        {

        }
        public async Task StartUserServer(EndPoint userendpoint, EndPoint remoteendpoint)
        {
            try
            {

                UserSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                UserSocket.Bind(UserEndPoint == null ? UserEndPoint : userendpoint);

                UserSocketEventArg = new SocketAsyncEventArgs();

                UserSocketEventArg.RemoteEndPoint = remoteendpoint;

                MainWindow.KeyPressEvent += PressThisKey;
                // MainWindow.MouseChange += MouseChangepos;
                //MainWindow.MousePressEvent += MousePressedDown;
                //MainWindow.MouseWheelevent += MainWindow_MouseWheelevent;

                UserSocketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(SendFromUserAsync);
            }
            catch(SocketException ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }


        }
        public async Task StartRemoteServer(EndPoint userendpoint, EndPoint remoteendpoint)
        {

            RemoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            RemoteSocket.Bind(userendpoint);

            UserSocketEventArg = new SocketAsyncEventArgs();

            UserSocketEventArg.RemoteEndPoint = remoteendpoint;
            UserSocketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(SendFromUserAsync);
            
            while (true)
            {
                RemoteSocket.ReceiveFromAsync(UserSocketEventArg);
            }
           


        }
        private void SendFromUserAsync(object? sender, SocketAsyncEventArgs e)
        {

        }

        public async void PressThisKey(Key key)
        {
            UDPMessage MSG = new();


            MSG.type = MessageType.Keyboard;
            MSG.key = key;
            byte[] bytes = getBytesOfUDPMessage(MSG);


           UserSocketEventArg.SetBuffer(bytes,UserSocketEventArg.Offset,UserSocketEventArg.Count);

           UserSocket?.SendToAsync(UserSocketEventArg);

        }
        public async void MouseChangepos(System.Windows.Point portion)
        {



        }
        public async void MousePressedDown(System.Windows.Input.MouseButton mouseside, MouseButtonState state)
        {

        }
        private async void MainWindow_MouseWheelevent(int delta)
        {

        }

        #region convertAndMessages
        private async Task<SoftwareBitmap> GetSoftwareBitmap(IDirect3DSurface Surface)
        {
            using var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(Surface);
            return softwareBitmap;
        }

        enum MessageType
        {
            Keyboard,
            MouseChange,
            point,
            Fram,
            Error,
            MouseWheelChange
        }
        struct UDPMessage
        {

            public MessageType type;
            public int MouseWheelDelta;
            public Key key;
            public string ErrorMessage;
            public System.Windows.Input.MouseButton MouseSide;
            public System.Windows.Point point;
            public int Width;
            public int Height;
            public byte[] FramByte;
            public MouseButtonState mousestate;
            public IDirect3DSurface surface;


        };

        static byte[] getBytesOfUDPMessage(UDPMessage str)
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
        static byte[] getBytesOfFrame(Direct3DSurfaceDescription str)
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
        UDPMessage FromByteArrayToUDPMessage(byte[] arr)
        {
            UDPMessage str = new UDPMessage();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);

                str = (UDPMessage)Marshal.PtrToStructure(ptr, str.GetType());
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return str;
        }
        #endregion
    }
}
