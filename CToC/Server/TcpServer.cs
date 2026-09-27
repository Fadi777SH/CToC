using CToC.Mouse;
using CToC.Screen;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Printing;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using WindowsInput;
using Point = System.Windows.Point;
namespace CToC.Server
{
    public class TcpServer
    {
        Socket? Accept;
        InputSimulator? inputsime;
        Socket? Client;
        int PORT = 22;
        private int ScreenX = SystemInformation.VirtualScreen.X;
        private int ScreenY = SystemInformation.VirtualScreen.Y;

        public static event Action? PC2DisConnect;
        private bool ServerShotDown = false;
        //private bool ClientShotDown = false;
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
        public delegate void SentFrameToPC1Handler(Bitmap bitmap);
        public static event SentFrameToPC1Handler? SentFrameToPC1;

        public FrameCapture frameCapture=new();
        public async Task Sender(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            if (Accept?.Connected == true) return;
            Accept = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Accept.Bind(new IPEndPoint(IPAddressOfPC1, PORT));

            PcEndPoint = new IPEndPoint(IPAddressOfPC1, PORT);
            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);
            new Thread(async delegate ()
            {
                try
                {

                    MainWindow.EndServerConnection += MainWindow_EndServerConnection;
                    MainWindow.KeyPressEvent += PressThisKey;
                    MainWindow.MouseChange += MouseChangepos;
                    MainWindow.MousePressEvent += MousePressedDown;
                    
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
               
                while (true)
                {
                    var buf = new byte[640000];
                    if (ClientendPoint != null)
                    {
                        var size = await Accept.ReceiveFromAsync(buf, ClientendPoint);
                        Array.Resize(ref buf,size.ReceivedBytes);
                        FrameArrived?.Invoke(buf);
                    }
                }
            }).Start();

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

            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);
            var Serverendpint = new IPEndPoint(IPAddressOfPC2, PORT);

            frameCapture.Stream();

            new Thread(() =>
            {
                SentFrameToPC1 += TcpServer_SentFrameToPC1;
            }).Start();

            while (true)
            {



                byte[] RecievedByte = new byte[255];
                try
                {
                    

                    var size =await Client.ReceiveFromAsync(RecievedByte, Serverendpint);
                    Array.Resize(ref RecievedByte, size.ReceivedBytes);

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


        private async void TcpServer_SentFrameToPC1(Bitmap bitmap)
        {
            using (MemoryStream stream = new())
            {
                bitmap.Save(stream, ImageFormat.Bmp);

                if (ClientendPoint != null)
                    Client?.SendToAsync(stream.ToArray(), ClientendPoint);
            }
        }

        enum MessageType
        {
            Keyboard,
            MouseChange,
            point,
            MouseWheel,
            Fram,
        }
        struct TCPMessage
        {
            public MessageType type;
            public Key key;
            public System.Windows.Input.MouseButton MouseSide;
            public System.Windows.Point point;
            public int Width;
            public int Height;
            public byte[] FramByte;
            public MouseButtonState mousestate;
            public IDirect3DSurface surface;

        };
        static byte[] getBytesOfTCPMessage(TCPMessage str)
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
        static byte[] getBytesOfFrame(IDirect3DSurface str)
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
            MSG.point = new System.Windows.Point(0, 0);
            MSG.mousestate = MouseButtonState.Pressed;
            MSG.MouseSide = System.Windows.Input.MouseButton.Left;
            byte[] bytes = getBytesOfTCPMessage(MSG);
            if (Accept != null && ServerShotDown == false)
                await Accept.SendToAsync(bytes, ClientendPoint);
        }

        public void FrameMessage(ref byte[] StoreByte, byte[] bytes, int w, int h)
        {
            TCPMessage MSG = new();
            MSG.type = MessageType.Fram;

            MSG.Width = w;
            MSG.Height = h;
            MSG.FramByte = bytes;
            var send = getBytesOfTCPMessage(MSG);
            send = StoreByte;
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
            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);
            }

        }
        public async void MousePressedDown(System.Windows.Input.MouseButton mouseside, MouseButtonState state)
        {

            TCPMessage MSG = new();


            MSG.type = MessageType.MouseChange;
            MSG.key = Key.LWin;
            MSG.point = new(0, 0);
            MSG.MouseSide = mouseside;
            MSG.mousestate = state;
            byte[] bytes = getBytesOfTCPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);


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

        public async static void _direct3D11CaptureFramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {

            var Frame = sender.TryGetNextFrame();
            
            if (Frame != null)
            {
                var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(Frame.Surface);
                    
                var bitmap = await FormSoftwarebitmapTobitmap(softwareBitmap);

                if (bitmap != null)
                {
                   
                    try
                    {
                        SentFrameToPC1?.Invoke(bitmap); 
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                       // stream?.Dispose();
                        bitmap?.Dispose();
                        Frame?.Dispose();
                        softwareBitmap?.Dispose();
                    }
                }
                    
            }
            

        }
    

        private static async Task<Bitmap?> FormSoftwarebitmapTobitmap(SoftwareBitmap softwareBitmap)
        {

            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            

            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);

            encoder.BitmapTransform.ScaledWidth = 320;
            encoder.BitmapTransform.ScaledHeight = 240;
            encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            encoder.IsThumbnailGenerated = true;
            await encoder.FlushAsync();
           var  bmp = new Bitmap(stream.AsStream());

            try
            {
                return bmp;
            }
            catch (Exception err)
            {
                const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                switch (err.HResult)
                {
                    case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                        // If the encoder does not support writing a thumbnail, then try again
                        // but disable thumbnail generation.
                        encoder.IsThumbnailGenerated = false;
                        break;
                    default:
                        throw;
                }
            }
            finally
            {
                bmp?.Dispose();
                stream?.Dispose();

            }
            return null;
            
        }
        
    }
}
