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
using static System.Windows.Forms.AxHost;
using BitmapEncoder =Windows.Graphics.Imaging.BitmapEncoder;
using Point = System.Windows.Point;
namespace CToC.Server
{
    public class UdpServer
    {
        #region headers
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

        public delegate void framCapture(byte[] bytes);
        public static event framCapture? SingleFram;
        public delegate void ShowPic(byte[] bytes);
        public static event ShowPic? FrameArrived;
        public delegate void SentFrameToPC2Handlerbit(byte[] bytes);
        public static event SentFrameToPC2Handlerbit? SentFrameToPC2bytes;
        public FrameCapture frameCapture=new();
        #endregion
        private bool ContinueSend = true;
        private bool ContinueRecive = true;

        public async Task Sender(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            if (Accept?.Connected == true) return;
            Accept = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            Accept.Bind(new IPEndPoint(IPAddressOfPC1, PORT));

            ContinueSend = true;
            ContinueRecive = true;
            PcEndPoint = new IPEndPoint(IPAddressOfPC1, PORT);
            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);

            //new
            await Accept.ConnectAsync(ClientendPoint);
            
            new Thread(async delegate ()
            {
                try
                {
                    if (ContinueSend)
                    {
                        MainWindow.KeyPressEvent += PressThisKey;
                        MainWindow.MouseChange += MouseChangepos;
                        MainWindow.MousePressEvent += MousePressedDown;
                        MainWindow.MouseWheelevent += MainWindow_MouseWheelevent;
                      
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



            //new
            await Client.ConnectAsync(ClientendPoint);




            ContinueSend = true;
            ContinueRecive = true;

            frameCapture.Stream();

            new Thread(() =>
            {
                if(ContinueSend)
                SentFrameToPC2bytes += UdpServer_SentFrameToPC2bytes;
                
            }).Start();
            
            while (ContinueRecive)
            {



                byte[] RecievedByte = new byte[255];
                try
                {
                    

                    var size =await Client.ReceiveFromAsync(RecievedByte, ClientendPoint);
                    Array.Resize(ref RecievedByte, size.ReceivedBytes);

                    UDPMessage MSG = FromByteArrayToUDPMessage(RecievedByte);
                    if (MSG.type == MessageType.Keyboard)
                    {
                        int vk = KeyInterop.VirtualKeyFromKey(MSG.key);
                        var key = (WindowsInput.Native.VirtualKeyCode)vk;
                        inputsime.Keyboard.KeyDown(key);
                        inputsime.Keyboard.KeyUp(key);
                    }


                    else if (MSG.type == MessageType.MouseWheelChange)
                    {
                        inputsime.Mouse.VerticalScroll(MSG.MouseWheelDelta);
                    }


                    else if (MSG.type == MessageType.Mousepoint)
                    {
                        var P = GetPC2MousePos(MSG.Mousepoint);
                        MousePosition.SetCursorPos((int)P.X, (int)P.Y);
                    }

                    else if (MSG.type == MessageType.MouseChange)
                    {
                        if (MSG.mousestate == MouseButtonState.Pressed)
                        {
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Left)
                            {
                                inputsime.Mouse.LeftButtonDown();
                            }
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Right)
                            {
                                inputsime.Mouse.RightButtonDown();
                            }
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Middle)
                            {
                                
                            }
                        }
                        else if (MSG.mousestate == MouseButtonState.Released)
                        {
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Left)
                            {
                                inputsime.Mouse.LeftButtonUp();
                            }
                            if (MSG.MouseSide == System.Windows.Input.MouseButton.Right)
                            {
                                inputsime.Mouse.LeftButtonUp();
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



        #region convertAndMessages

        
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



        #region FrameCaptureRegion
        private static int frameWidth = 870;
        private static int frameHeight = 500;
        static Stopwatch stopwatch = new();
        private async void UdpServer_SentFrameToPC2bytes(byte[] MessageByte)
        {

            if (MessageByte.Length >= 64000)
            {
                MessageBox.Show($"you acceed the length limit of the message \n the length was : {MessageByte.Length}");
            }

            if (ClientendPoint != null && ContinueSend)
            {
               Client?.SendToAsync(MessageByte, ClientendPoint);
            }
            stopwatch.Stop();
            Debug.WriteLine($"{stopwatch.ElapsedMilliseconds} means {1000/stopwatch.ElapsedMilliseconds} FPS");
            stopwatch.Reset();

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

            }
            


        }

        private static async Task<byte[]> FromSoftwarebitmapToBytes(SoftwareBitmap softwareBitmap)
        {


            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);

            
            encoder.BitmapTransform.ScaledWidth = (uint)700;
            encoder.BitmapTransform.ScaledHeight = (uint)500;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();

            var CompressedBytes = ConversionClass.Compress(stream.AsStream().CopyToBytes());
            

            return CompressedBytes;


        }
        #endregion
        #region ConnectionRagion
        UDPMessage MSG = new();
        public void Appclosed()
        {
            MSG = new();
            MSG.ErrorMessage = "Disconnect from the remote computer";
            MSG.type =MessageType.Error;
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
        #endregion
        #region SendEventsRegion
        private Point GetPC2MousePos(Point portion)
        {
            var Xpoint = (portion.X / 100) * ScreenWidth;
            var Ypoint = (portion.Y / 100) * ScreenHeight;
            return new(Xpoint, Ypoint);
        }
        public async void PressThisKey(Key key)
        {

            MSG = new();


            MSG.type = MessageType.Keyboard;
            MSG.key = key;
            byte[] bytes = getBytesOfUDPMessage(MSG);
            if (Accept != null && ServerShotDown == false)
                await Accept.SendToAsync(bytes, ClientendPoint);
        }
        public async void MouseChangepos(System.Windows.Point portion)
        {

             MSG = new();


            MSG.type = MessageType.Mousepoint;


            MSG.Mousepoint = portion;

            byte[] bytes = getBytesOfUDPMessage(MSG);
            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);
                
            }

        }
        public async void MousePressedDown(System.Windows.Input.MouseButton mouseside, MouseButtonState state)
        {

            MSG = new();


            MSG.type = MessageType.MouseChange;
            MSG.MouseSide = mouseside;
            MSG.mousestate = state;
            byte[] bytes = getBytesOfUDPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);


            }
        }
        private async void MainWindow_MouseWheelevent(int delta)
        {
            MSG = new();


            MSG.type =MessageType.MouseWheelChange;
            MSG.MouseWheelDelta = delta;

            byte[] bytes = getBytesOfUDPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);


            }
        }
        #endregion
    }

}
