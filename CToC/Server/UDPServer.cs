using CToC.Mouse;
using CToC.Screen;
using FFMpegCore;
using FFMpegCore.Pipes;
using K4os.Compression.LZ4;
using Microsoft.Graphics.Canvas;
using SharpDX.DXGI;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using WindowsInput;
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
        public delegate void ShowPic(byte[] bytes, int raw);
        public static event ShowPic? FrameArrived;
        public delegate void SentFrameToPC2Handlerbit(byte[] bytes);
        public static event SentFrameToPC2Handlerbit? SentFrameToPC2bytes;
        public delegate void ProcessDirectSurface(IDirect3DSurface surface);
        public static event ProcessDirectSurface? ProcessDirecSurfaceEvent;
        public FrameCapture frameCapture = new();
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
                System.Collections.Generic.IEnumerable<byte> Concate = new byte[65000];

                while (ContinueRecive)
                {
                    var buf = new byte[650000];
                   
                    if (ClientendPoint != null)
                    {
                        try
                        {
                            var size = await Accept.ReceiveFromAsync(buf, ClientendPoint);
                            Array.Resize(ref buf, size.ReceivedBytes);
                          
                            var UDPMSG = FromByteArrayToUDPFrameMessage(buf);

                            if( UDPMSG.ChunkByteArray.Length!=UDPMSG.ShouldResizeTo)
                            Array.Resize(ref UDPMSG.ChunkByteArray, UDPMSG.ShouldResizeTo);

                            if( UDPMSG.CurrentChunkNumber < UDPMSG.totalChunks)
                            {
                                Concate = Concate.Concat(UDPMSG.ChunkByteArray);
                            }
                            else if (UDPMSG.totalChunks == UDPMSG.CurrentChunkNumber)
                            {
                                FrameArrived?.Invoke(Concate.ToArray(), 1920 * 1080 * 4);
                                
                                Concate =new byte[65000];
                            }
                            //Array.Resize(ref buf, size.ReceivedBytes);
                            

                           
                        }
                        catch (Exception ex)
                        {
                           // MessageBox.Show(ex.Message);
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
                if (ContinueSend)
                {
                    SentFrameToPC2bytes += UdpServer_SentFrameToPC2bytes;
                }

            }).Start();

            while (ContinueRecive)
            {



                byte[] RecievedByte = new byte[255];
                try
                {


                    var size = await Client.ReceiveFromAsync(RecievedByte, ClientendPoint);
                     Array.Resize(ref RecievedByte, size.ReceivedBytes);
 
                    UDPMessage MSG = FromByteArrayToUDPMessage(RecievedByte);
                    if (MSG.type == MessageType.Keyboard)
                    {
                        int vk = KeyInterop.VirtualKeyFromKey(MSG.key);
                        var key = (WindowsInput.Native.VirtualKeyCode)vk;
                        if (MSG.IsKeyDown)
                            inputsime.Keyboard.KeyDown(key);
                        else
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
                                inputsime.Mouse.RightButtonUp();
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


        static byte[] getBytesOfUDPMessage<T>(T str)
        {
            int size = Marshal.SizeOf(str);
            
            byte[] arr = new byte[65000];

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
        UDPframeMessage FromByteArrayToUDPFrameMessage(byte[] arr)
        {
            UDPframeMessage str = new UDPframeMessage();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);

                str = (UDPframeMessage)Marshal.PtrToStructure(ptr, str.GetType());
            }
            catch(Exception ex)
            {
              System.Windows.MessageBox.Show(ex.Message);
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
        private async void UdpServer_SentFrameToPC2bytes(byte[] _MessageByte)
        {

            var subBuffers = _MessageByte.Chunk(64000);
            var totalchunks = subBuffers.Count();
            var count = 1;
            foreach(var buf in subBuffers)
            {
                count++;
                var MSG = new UDPframeMessage( totalchunks,count,buf,buf.Length);

                var UDPMSG = getBytesOfUDPMessage(MSG);
                

                Client?.SendToAsync(UDPMSG, ClientendPoint);
                
            }
          
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds != 0)
                Debug.WriteLine($"{stopwatch.ElapsedMilliseconds} means {1000 / stopwatch.ElapsedMilliseconds} FPS");
            stopwatch.Reset();
        }

        private static  CanvasDevice _canvasDevice => CanvasDevice.CreateFromDirect3D11Device(FrameCapture.dev);

        public static async void _direct3D11CaptureFramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            stopwatch.Start();
            try
            {
                using var frame = sender.TryGetNextFrame();


                if (frame != null)
                {

                    var bytes = LZ4Compression(frame.Surface);
                    SentFrameToPC2bytes?.Invoke(bytes);

                }
            }
            catch(Exception ex)
            {
               System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private static byte[] LZ4Compression(IDirect3DSurface surface)
        {

            var _renderTargetBitmap = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, surface);
            
            
            byte[] raw =_renderTargetBitmap.GetPixelBytes();
            var compressed = new byte[LZ4Codec.MaximumOutputSize(raw.Length)];
            int size = LZ4Codec.Encode(raw, 0, raw.Length, compressed, 0, compressed.Length,LZ4Level.L00_FAST);

            Array.Resize(ref compressed, size);
 
            return compressed;
        }
        public static  byte[] ExtractBytesAsync(MediaStreamSample sample)
        {
            
            var buffer = sample.Buffer;

            byte[] bytes = new byte[buffer.Capacity];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(bytes);
            }
            return bytes;

        }
        static async Task<byte[]> CompressSurfaceAsync(IDirect3DSurface surface, uint w, uint h)
        {
            using var ms = new MemoryStream();
            using SoftwareBitmap sb = await SoftwareBitmap.CreateCopyFromSurfaceAsync(surface);

            var frame =new SoftwareBitmapFrame(sb);   // class from my earlier message
            var source = new RawVideoPipeSource(new IVideoFrame[] { frame }) { FrameRate = 30 };


            await FFMpegArguments
                .FromPipeInput(new RawVideoPipeSource(new[] { frame}) { FrameRate = 30 })
                .OutputToPipe(new StreamPipeSink(ms), o => o
                    .WithVideoCodec("libx265")
                    .WithConstantRateFactor(28)
                    .ForcePixelFormat("yuv420p")
                    .ForceFormat("hevc"))          // raw Annex B bitstream, pipe-friendly
                .ProcessAsynchronously();

            byte[] bytes = ms.ToArray();
            return bytes;
        }
        #endregion
        #region ConnectionRagion
        UDPMessage MSG = new();
        public void Appclosed()
        {
            MSG = new();
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
        #endregion
        #region SendEventsRegion
        private Point GetPC2MousePos(Point portion)
        {
            var Xpoint = (portion.X / 100) * ScreenWidth;
            var Ypoint = (portion.Y / 100) * ScreenHeight;
            return new(Xpoint, Ypoint);
        }
        public async void PressThisKey(Key key, bool e)
        {

            //skip pressing the Lwin and Rwin button
            if (Key.LWin == key || key == Key.RWin) return;
            MSG = new();
            MSG.type = MessageType.Keyboard;
            MSG.key = key;
            MSG.IsKeyDown = e;

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
            Debug.WriteLine(MSG.MouseSide + "  " + MSG.mousestate);
            byte[] bytes = getBytesOfUDPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendToAsync(bytes, ClientendPoint);


            }
        }
        private async void MainWindow_MouseWheelevent(int delta)
        {
            MSG = new();


            MSG.type = MessageType.MouseWheelChange;
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