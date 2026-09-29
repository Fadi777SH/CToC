using CToC.Mouse;
using CToC.Screen;
using ImageResizer.ExtensionMethods;
using Microsoft.Graphics.Canvas;
using SharpDX.DXGI;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Printing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vortice.Win32;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Storage.Streams;
using WindowsInput;
using BitmapEncoder = Windows.Graphics.Imaging.BitmapEncoder;
using Point = System.Windows.Point;
namespace CToC.Server
{
    public class UdpServer
    {
        public Socket? Accept;
        InputSimulator? inputsime;
        public Socket? Client;
        int PORT = 22;
        private int ScreenX = SystemInformation.VirtualScreen.X;
        private int ScreenY = SystemInformation.VirtualScreen.Y;

        public static event Action? PC2DisConnect;
        private bool ServerShotDown = false;
      
        int ScreenWidth = SystemInformation.VirtualScreen.Width;
        int ScreenHeight = SystemInformation.VirtualScreen.Height;
        EndPoint PcEndPoint;
        EndPoint ClientendPoint;
        int ABSX = SystemInformation.VirtualScreen.X;
        int ABSY = SystemInformation.VirtualScreen.Y;

        public delegate void framCapture(byte[] bytes);
        public static event framCapture? SingleFram;
        public delegate void ShowPic(byte[] bytes);
        public static event ShowPic? FrameArrived;
        public delegate void SentFrameToPC2Handler(Bitmap bitmap);
        public static event SentFrameToPC2Handler? SentFrameToPC2;
        public delegate void SentFrameToPC2Handlerbit(byte[] bytes);
        public static event SentFrameToPC2Handlerbit? SentFrameToPC2bytes;
        public FrameCapture frameCapture=new();

        private bool ContinueSend = true;
        private bool ContinueRecive = true;
        public async Task Sender(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            if (Accept?.Connected == true) return;
            Accept = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
           // Accept.NoDelay = true;
            Accept.Bind(new IPEndPoint(IPAddressOfPC1, PORT));
            ContinueSend = true;
            ContinueRecive = true;
            PcEndPoint = new IPEndPoint(IPAddressOfPC1, PORT);
            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);
            new Thread(async delegate ()
            {
                try
                {
                    if (ContinueSend)
                    {
                        MainWindow.KeyPressEvent += PressThisKey;
                        MainWindow.MouseChange += MouseChangepos;
                        MainWindow.MousePressEvent += MousePressedDown;
                    }
                    
                }
                catch
                {

                    System.Windows.MessageBox.Show("Error");
                    PC2DisConnect?.Invoke();

                }

            })
            {

            }.Start();

            new Task(async () =>
            {
               
                while (ContinueRecive)
                {
                    var buf = new byte[640000];
                    if (ClientendPoint != null)
                    {
                        try { 
                        var size = await Accept.ReceiveFromAsync(buf, ClientendPoint);
                        
                        Array.Resize(ref buf,size.ReceivedBytes);
                        FrameArrived?.Invoke(buf);
                        //var MSG = FromByteArrayToUDPMessage(buf);
                        //if (MSG.type == MessageType.Fram)
                        //{
                        //  FrameArrived?.Invoke(MSG.FramByte);
                        //}
                        //else if (MSG.type == MessageType.Error)
                        //{
                        //  MessageBox.Show(MSG.ErrorMessage);
                        //break;
                        //}
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }).Start();
        }

        public async Task Reciever(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            inputsime = new();
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
           
            Client.Bind(new IPEndPoint(IPAddressOfPC1, PORT));

            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);
            var Serverendpint = new IPEndPoint(IPAddressOfPC2, PORT);
            ContinueSend = true;
            ContinueRecive = true;
            frameCapture.Stream();

            new Thread(() =>
            {
                SentFrameToPC2 += UDPServer_SentFrameToPC1;
                SentFrameToPC2bytes += UdpServer_SentFrameToPC2bytes;
                
            }).Start();
            
            while (ContinueRecive)
            {



                byte[] RecievedByte = new byte[255];
                try
                {
                    

                    var size =await Client.ReceiveFromAsync(RecievedByte, Serverendpint);
                    Array.Resize(ref RecievedByte, size.ReceivedBytes);

                    UDPMessage MSG = FromByteArrayToUDPMessage(RecievedByte);
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
                        MousePosition.SetCursorPos((int)P.X, (int)P.Y);
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
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }

            }
        }


        

        private async Task<SoftwareBitmap> GetSoftwareBitmap(IDirect3DSurface Surface)
        {
            using var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(Surface);
            return softwareBitmap;
        }
        private static byte[] MessageByteArrayFromBitmap(Bitmap bitmap)
        {
            using MemoryStream stream = new();

            using var CompressedBitmap = ConversionClass.compressbitmap(bitmap, frameWidth, frameHeight);

            CompressedBitmap.Save(stream, ImageFormat.Jpeg);
            return stream.GetBuffer();
        }
        enum MessageType
        {
            Keyboard,
            MouseChange,
            point,
            MouseWheel,
            Fram,
            Error,
        }
        struct UDPMessage
        {
            public MessageType type;
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
        public async void PressThisKey(Key key)
        {

            UDPMessage MSG = new();


            MSG.type = MessageType.Keyboard;
            MSG.key = key;
            MSG.point = new System.Windows.Point(0, 0);
            MSG.mousestate = MouseButtonState.Pressed;
            MSG.MouseSide = System.Windows.Input.MouseButton.Left;
            byte[] bytes = getBytesOfUDPMessage(MSG);
            if (Accept != null && ServerShotDown == false)
                await Accept.SendToAsync(bytes, ClientendPoint);
        }

        public void FrameMessage(ref byte[] StoreByte, byte[] bytes, int w, int h)
        {
            UDPMessage MSG = new();
            MSG.type = MessageType.Fram;

            MSG.Width = w;
            MSG.Height = h;
            MSG.FramByte = bytes;
            var send = getBytesOfUDPMessage(MSG);
            send = StoreByte;
        }
        public async void MouseChangepos(System.Windows.Point portion)
        {

            UDPMessage MSG = new();


            MSG.type = MessageType.point;
            MSG.key = Key.LWin;

            MSG.point = portion;

            MSG.MouseSide = System.Windows.Input.MouseButton.Left;
            MSG.mousestate = MouseButtonState.Pressed;

            byte[] bytes = getBytesOfUDPMessage(MSG);
            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);
            }

        }
        public async void MousePressedDown(System.Windows.Input.MouseButton mouseside, MouseButtonState state)
        {

            UDPMessage MSG = new();


            MSG.type = MessageType.MouseChange;
            MSG.key = Key.LWin;
            MSG.point = new(0, 0);
            MSG.MouseSide = mouseside;
            MSG.mousestate = state;
            byte[] bytes = getBytesOfUDPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);


            }
        }
        private Point GetPC2MousePos(Point portion)
        {
            var Xpoint = (portion.X / 100) * ScreenWidth ;
            var Ypoint = (portion.Y / 100) * ScreenHeight ;
            return new(Xpoint, Ypoint);
        }


        public static int frameWidth = 870;
        public static int frameHeight = 500;
        static Stopwatch stopwatch = new();
        private async void UDPServer_SentFrameToPC1(Bitmap bitmap)
        {


            byte[] MessageByte = MessageByteArrayFromBitmap(bitmap);
            if (MessageByte.Length >= 64000)
            {
                MessageBox.Show("you acceed the length limit of the message");
            }

            if (ClientendPoint != null && ContinueSend)
            {
                // UDPMessage MSG = new UDPMessage { type = MessageType.Fram, FramByte = stream.ToArray() };


                Client?.SendToAsync(MessageByte, ClientendPoint);
            }
        }
        private void UdpServer_SentFrameToPC2bytes(byte[] MessageByte)
        {

            if (MessageByte.Length >= 64000)
            {
                MessageBox.Show("you acceed the length limit of the message");
            }

            if (ClientendPoint != null && ContinueSend)
            {
                // UDPMessage MSG = new UDPMessage { type = MessageType.Fram, FramByte = stream.ToArray() };


                Client?.SendToAsync(MessageByte, ClientendPoint);
            }
            

        }
        public async static void _direct3D11CaptureFramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {

            stopwatch.Start();
            using var Frame = sender.TryGetNextFrame();

            if (Frame != null)
            {
                
                using var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(Frame.Surface);

                var bytes = await FromSoftwarebitmapToBytes(softwareBitmap);

                SentFrameToPC2bytes?.Invoke(bytes);
               // SentFrameToPC2?.Invoke(bitmap);

            }
            


        }
        private static async Task<byte[]> GetByteFromSoftWareBitmap(SoftwareBitmap softwareBitmap)
        {
            InMemoryRandomAccessStream stream = new();
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();
            byte[] buffer = new byte[stream.Size];

            stream.Seek(0);
            await stream.ReadAsync(buffer.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);

            return buffer;
            
        }
        private static async Task<Bitmap?> FromSoftwarebitmapTobitmap(SoftwareBitmap softwareBitmap)
        {


            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
           
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            

            encoder.SetSoftwareBitmap(softwareBitmap);


            await encoder.FlushAsync();


         
            
            //try  to send byte of the bitmap

            return  new(stream.AsStream());
            
            
        }
        private static async Task<byte[]> FromSoftwarebitmapToBytes(SoftwareBitmap softwareBitmap)
        {


            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

            encoder.SetSoftwareBitmap(softwareBitmap);
            var width = 500;
            var height = 500;
            encoder.BitmapTransform.ScaledWidth = (uint)width;
            encoder.BitmapTransform.ScaledHeight = (uint)height;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            await encoder.FlushAsync();
            var com = ConversionClass.Compress(stream.AsStream().CopyToBytes());
            

            return com;


        }
        public void Appclosed()
        {
            UDPMessage MSG = new();
            MSG.ErrorMessage = "Disconnect from the remote computer";
            MSG.type = MessageType.Error;
            var B = getBytesOfUDPMessage(MSG);
            //Client?.SendToAsync(B, ClientendPoint);
        }
        public void disconnectClient()
        {
            if (Client != null)
            {
                ContinueRecive = false;
                ContinueSend = false;
                Client.Close();
            }
            if (frameCapture.IsStreaming == true)
            {
                frameCapture.EndStream();

            }
        }
        public void disconnectAccepter()
        {
            if (Accept != null)
            {
                ContinueSend = false;
                ContinueRecive = false;
                Accept.Close();
            }
            MainWindow.KeyPressEvent -= PressThisKey;
            MainWindow.MouseChange -= MouseChangepos;
            MainWindow.MousePressEvent -= MousePressedDown;
        }
    }
}
