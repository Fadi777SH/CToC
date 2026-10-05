using CToC.Mouse;
using CToC.Screen;
using K4os.Compression.LZ4;
using Microsoft.Graphics.Canvas;
using Mono.CSharp;
using SharpDX.DXGI;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Storage.Compression;
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
        public delegate void ShowPic(byte[] bytes, int W, int H);
        public static event ShowPic? FrameArrived;
        public delegate void SentFrameToPC2Handlerbit(byte[] bytes,int W,int H);
        public static event SentFrameToPC2Handlerbit? SentFrameToPC2bytes;
        public delegate void ProcessDirectSurface(IDirect3DSurface surface);
        public static event ProcessDirectSurface? ProcessDirecSurfaceEvent;
        public FrameCapture frameCapture = new();
        #endregion
        private bool ContinueSend = true;
        private bool ContinueRecive = true;
        private const int _SingleFrameChunk = 64000;
        public async Task Sender(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            if (Accept?.Connected == true) return;
            Accept = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) ;

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
                catch(Exception ex)
                {

                    System.Windows.MessageBox.Show(ex.Message);
                    
                    PC2DisConnect?.Invoke();

                }

            })
            {
            }.Start();

            new Task(async () =>
            {
                 System.Collections.Generic.IEnumerable<byte> Concate = new byte[0];
        
                int currentfingerprint = 0;
                byte[] array = new byte[0];
                int lastframe = 0;
                bool concatefirst = false;
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
                            if (UDPMSG.type == MessageType.Error)
                            {
                                if(UDPMSG.ErrorMessageType==_UDPErrorMessageTypes.ClientExit)
                                {
                                    System.Windows.MessageBox.Show("there been an exist or a disconnect from the client side ");
                                    break;
                                }
                            }

                            if (currentfingerprint != UDPMSG.FrameFingerPrint)
                            {
                                currentfingerprint = UDPMSG.FrameFingerPrint;
                                
                                Concate = new byte[0];
                                Concate = Concate.Concat(UDPMSG.ChunkByteArray);

                                concatefirst = false;

                                lastframe = UDPMSG.CurrentChunkNumber;

                            }
                            if (currentfingerprint == UDPMSG.FrameFingerPrint)
                            {
                                if (UDPMSG.CurrentChunkNumber == 1) concatefirst = true;
                                if (concatefirst == true && lastframe == UDPMSG.CurrentChunkNumber - 1)
                                {
                                    if (UDPMSG.CurrentChunkNumber < UDPMSG.totalChunks)
                                    {
                                        Concate = Concate.Concat(UDPMSG.ChunkByteArray);
                                    }

                                    //last chunk
                                    if (UDPMSG.CurrentChunkNumber == UDPMSG.totalChunks)
                                    {
                                        Array.Resize(ref UDPMSG.ChunkByteArray, UDPMSG.ShouldResizeTo);

                                        Concate = Concate.Concat(UDPMSG.ChunkByteArray);
                                        

                                        if (UDPMSG.TotalSizeOfTheFrame == Concate.ToArray().Length)
                                        {

                                            FrameArrived?.Invoke(Concate.ToArray(),UDPMSG.FrameWidth,UDPMSG.FrameHeight);
                                        }

                                        concatefirst = false;
                                    }

                                    lastframe = UDPMSG.CurrentChunkNumber;
                                }
                            }


                        }
                        catch (Exception ex)
                        {
                           
                            System.Windows.MessageBox.Show(ex.Message);
                        }
                    }
                }
            })
            {

            }.Start();


        }


        public async Task Reciever(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            inputsime = new();
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) ;

            Client.Bind(new IPEndPoint(IPAddressOfPC1, PORT));

            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);
            
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
                    else if (MSG.type == MessageType.Error)
                    {
                        if (MSG.ErrorMessageType == _UDPErrorMessageTypes.ClientExit)
                        System.Windows.MessageBox.Show("there been an exist or a disconnect from the client side ");
                        break;
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

        Random random = new();
        private async void UdpServer_SentFrameToPC2bytes(byte[] _MessageByte,int W,int H)
        {

            var subBuffers = _MessageByte.Chunk(_SingleFrameChunk);
            var totalchunks = subBuffers.Count();
            var count = 0;
            var RandomFingerPrint=random.Next(0,1000);
            var totalsize = _MessageByte.Length;

            if (totalchunks<40)
            foreach(var buf in subBuffers)
            {
                count++;

                var MSG = new UDPframeMessage(totalchunks, count, buf, buf.Length, RandomFingerPrint, totalsize, W, H) { type=MessageType.Fram};

                var UDPMSG = getBytesOfUDPMessage(MSG);
                

                Client?.SendToAsync(UDPMSG, ClientendPoint);
                
            }

          

        }

        private static  CanvasDevice _canvasDevice => CanvasDevice.CreateFromDirect3D11Device(FrameCapture.dev);

        public static async void _direct3D11CaptureFramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
   
            try
            {
                using var frame = sender.TryGetNextFrame();


                if (frame != null)
                {
                    var W = frame.Surface.Description.Width;
                    var H = frame.Surface.Description.Height;
                    var bytes = LZ4Compression(frame.Surface);
                    SentFrameToPC2bytes?.Invoke(bytes, W, H);

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
        #endregion
        #region ConnectionRagion
        UDPMessage MSG = new();
        public async Task disconnectClient()
        {
            if (Client != null)
            {
                ContinueRecive = false;
                ContinueSend = false;
                var MSG = new UDPframeMessage() { ErrorMessageType = _UDPErrorMessageTypes.ClientExit ,type= MessageType.Error};

                var bytes = getBytesOfUDPMessage(MSG);


                await Client.SendToAsync(bytes, ClientendPoint);
                Client.Close();
            }
            if (frameCapture.IsStreaming == true)
            {
                frameCapture.EndStream();

            }
        }
        public async Task disconnectAccepter()
        {
            if (Accept != null)
            {
                ContinueSend = false;
                ContinueRecive = false;

                MSG = new UDPMessage() { ErrorMessageType = _UDPErrorMessageTypes.ClientExit, type = MessageType.Error };

                var bytes = getBytesOfUDPMessage(MSG);
                await Accept.SendToAsync(bytes, ClientendPoint);

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