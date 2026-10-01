using CToC.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
           // _socketAsyncEventArgs = new();
           // _socketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_compelete);
        }
        public async Task StartUserServer()
        {
            try
            {

                UserSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                UserSocket.Bind(UserEndPoint);

                UserSocketEventArg = new() { RemoteEndPoint=RemoreEndpoint};

                UserSocketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                StartSend(UserSocketEventArg);

            }
            catch(SocketException ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }


        }
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {


                //echo the data received back to the client
                e.SetBuffer(e.Offset, e.BytesTransferred);
                Socket socket = (Socket)e.UserToken;
                bool willRaiseEvent = socket.SendAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessSend(e);
                }
            }
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                
                Socket socket = (Socket)e.UserToken;

                bool willRaiseEvent = socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                //CloseClientSocket(e);
            }
        }
        public async void SentToRemote(object sender, SocketAsyncEventArgs e)
        {
            if (e!=null)
            {

            }
        }
        public async void RecieveFromRemote(object sender, SocketAsyncEventArgs e)
        {
            var buffer = e.Buffer;

            Debug.WriteLine(e.Buffer.Length);

            StartRecive(e);

        }
        public async void StartSend(SocketAsyncEventArgs e)
        {
            MainWindow.KeyPressEvent += PressThisKey;
        }
        public async void StartRecive(SocketAsyncEventArgs e)
        {
 
                var f = RemoteSocket?.ReceiveFromAsync(e);
                if (f!=true) return;

            
        }
        public async Task StartRemoteServer()
        {

            RemoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            RemoteSocket.Bind(UserEndPoint);


            UserSocketEventArg = new SocketAsyncEventArgs() { RemoteEndPoint=RemoreEndpoint};

            UserSocketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            StartRecive(UserSocketEventArg);

            //UserSocketEventArg.RemoteEndPoint = remoteendpoint;

            //UserSocketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(UserSocketEventArg_Completed);




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


            UserSocketEventArg.SetBuffer(bytes,UserSocketEventArg.Offset,bytes.Length);

            var f= UserSocket?.SendToAsync(UserSocketEventArg);
            TransmitFileOptions e = new() { };
            
            if (f == true)
            {

            }

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
